# Sync GhJSON schemas from the published ghjson-spec into the local embedded snapshot.
#
# Discovers all published schema versions under the ghjson-spec schema directory,
# downloads each version's main schema and every referenced extension schema,
# and overwrites the committed copies under src/GhJSON.Core/Validation/Schemas/.
#
# Usage (from repo root):
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1 -Check        # exit code 1 if drift
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1 -BaseUrl ...  # override source
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1 -Version 1.0  # skip auto-discovery
#
# Rate-limit warning: Auto-discovery uses the GitHub API (unauthenticated) which is
# limited to 60 requests per hour per IP. If you hit this limit, use -Version to
# bypass auto-discovery.

#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$BaseUrl = 'https://architects-toolkit.github.io/ghjson-spec/schema/',
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string[]]$Version = @(),
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }

# Discover all published schema versions by listing the schema directory.
function Get-SchemaVersions {
    param([string]$Base)

    # GitHub Pages doesn't support directory listing, so we use the ghjson-spec
    # GitHub API to list contents of the schema directory.
    $apiUrl = 'https://api.github.com/repos/architects-toolkit/ghjson-spec/contents/schema'
    Write-Host "Discovering schema versions from $apiUrl"

    try {
        $items = @(Invoke-RestMethod -Uri $apiUrl -UseBasicParsing -Headers @{ 'User-Agent' = 'ghjson-dotnet-sync' } -ErrorAction Stop)
    }
    catch {
        # Fallback: assume at least v1.0 exists if API fails
        Write-Warning "Failed to discover versions via API: $($_.Exception.Message). Assuming v1.0."
        return @('1.0')
    }

    $versions = @($items | Where-Object { $_.type -eq 'dir' -and $_.name -match '^v\d+\.\d+' } | ForEach-Object {
        $_.name -replace '^v', ''
    })

    if ($versions.Count -eq 0) {
        Write-Warning "No version directories found. Assuming v1.0."
        return @('1.0')
    }

    return $versions | Sort-Object
}

# Files to sync for a specific version:
#   1. The main schema.
#   2. The extension registry.
#   3. Every extension schema referenced by the registry (discovered dynamically by
#      walking the registry's $ref entries, so adding/removing an extension in
#      ghjson-spec requires no change here).

function Get-SchemaFileList {
    param([string]$Base)

    $list = [System.Collections.Generic.List[string]]::new()
    $list.Add('ghjson.schema.json')
    $list.Add('extensions/extensions.schema.json')

    $registryUrl = $Base + 'extensions/extensions.schema.json'
    Write-Host "Discovering extensions from $registryUrl"

    try {
        $registryText = (Invoke-WebRequest -Uri $registryUrl -UseBasicParsing -Headers @{ 'User-Agent' = 'ghjson-dotnet-sync' } -ErrorAction Stop).Content
    }
    catch {
        throw "Failed to fetch extension registry $registryUrl : $($_.Exception.Message)"
    }

    $registry = $registryText | ConvertFrom-Json

    # Walk `properties.*.$ref` and any `$ref` inside `additionalProperties`.
    $refs = [System.Collections.Generic.List[string]]::new()
    if ($registry.PSObject.Properties.Name -contains 'properties' -and $registry.properties) {
        foreach ($prop in $registry.properties.PSObject.Properties) {
            $value = $prop.Value
            if ($value -and ($value.PSObject.Properties.Name -contains '$ref')) {
                $refs.Add([string]$value.'$ref')
            }
        }
    }

    foreach ($ref in $refs) {
        # Registry $refs are relative to extensions/ (e.g. './gh.panel.schema.json').
        $clean = $ref -replace '^\./', ''
        if ($clean -like 'http*') { continue }  # absolute refs: skip (already handled elsewhere)
        $list.Add("extensions/$clean")
    }

    return @($list | Select-Object -Unique)
}

function Sync-SchemaVersion {
    param(
        [string]$Version,
        [string]$Base,
        [string]$DestRoot,
        [switch]$Check
    )

    $versionUrl = $Base + "v$Version/"
    $destRoot = Join-Path $DestRoot "v$Version"
    if (-not (Test-Path $destRoot)) {
        New-Item -ItemType Directory -Force -Path $destRoot | Out-Null
    }

    $files = Get-SchemaFileList -Base $versionUrl

    $changed = @()
    $unchanged = @()
    $drift = $false

    foreach ($rel in $files) {
        $url = $versionUrl + $rel
        $dest = Join-Path $destRoot $rel
        $destDir = Split-Path -Parent $dest
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }

        Write-Host "Fetching $url"
        try {
            $remote = Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ 'User-Agent' = 'ghjson-dotnet-sync' } -ErrorAction Stop
        }
        catch {
            throw "Failed to download $url : $($_.Exception.Message)"
        }

        # Normalize line endings so comparisons are stable regardless of source host.
        $remoteText = ($remote.Content -replace "`r`n", "`n").TrimEnd() + "`n"

        $localText = $null
        if (Test-Path $dest) {
            $localText = ([System.IO.File]::ReadAllText($dest)) -replace "`r`n", "`n"
            $localText = $localText.TrimEnd() + "`n"
        }

        if ($localText -eq $remoteText) {
            $unchanged += $rel
            continue
        }

        $changed += $rel
        $drift = $true

        if ($Check) {
            Write-Host "  DRIFT: $rel" -ForegroundColor Yellow
        }
        else {
            # Preserve remote byte content (no BOM), but use LF to match spec repo convention.
            [System.IO.File]::WriteAllText($dest, $remoteText, (New-Object System.Text.UTF8Encoding($false)))
            Write-Host "  Updated: $dest" -ForegroundColor Green
        }
    }

    Write-Host ''
    Write-Host ("Version {0}: {1} changed, {2} unchanged, {3} total." -f $Version, $changed.Count, $unchanged.Count, $files.Count)

    return $drift
}

if ($Version.Count -gt 0) {
    $versions = $Version
} else {
    $versions = Get-SchemaVersions -Base $BaseUrl
}
$totalDrift = $false

foreach ($version in $versions) {
    Write-Host ''
    Write-Host "=== Syncing schema version $version ===" -ForegroundColor Cyan
    $versionDrift = Sync-SchemaVersion -Version $version -Base $BaseUrl -DestRoot (Join-Path $Root 'src/GhJSON.Core/Validation/Schemas') -Check:$Check
    if ($versionDrift) {
        $totalDrift = $true
    }
}

if ($Check -and $totalDrift) {
    Write-Host ''
    Write-Host 'Embedded schema snapshot is OUT OF DATE.' -ForegroundColor Red
    exit 1
}

exit 0
