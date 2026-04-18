# Sync GhJSON schemas from the published ghjson-spec into the local embedded snapshot.
#
# Downloads the main schema and every referenced extension schema from
# https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/ and overwrites the
# committed copies under src/GhJSON.Core/Validation/Schemas/v1.0/.
#
# Usage (from repo root):
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1 -Check        # exit code 1 if drift
#   pwsh -ExecutionPolicy Bypass -File .\tools\Sync-Schemas.ps1 -BaseUrl ...  # override source

#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$BaseUrl = 'https://architects-toolkit.github.io/ghjson-spec/schema/v1.0/',
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }

$destRoot = Join-Path $Root 'src\GhJSON.Core\Validation\Schemas\v1.0'
if (-not (Test-Path $destRoot)) {
    New-Item -ItemType Directory -Force -Path $destRoot | Out-Null
}

# Files to sync:
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
        $registryText = (Invoke-WebRequest -Uri $registryUrl -UseBasicParsing -ErrorAction Stop).Content
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

    return ($list | Select-Object -Unique)
}

$files = Get-SchemaFileList -Base $BaseUrl

$changed = @()
$unchanged = @()
$drift = $false

foreach ($rel in $files) {
    $url = $BaseUrl + $rel
    $dest = Join-Path $destRoot ($rel -replace '/', '\')
    $destDir = Split-Path -Parent $dest
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    }

    Write-Host "Fetching $url"
    try {
        $remote = Invoke-WebRequest -Uri $url -UseBasicParsing -ErrorAction Stop
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
Write-Host ("Summary: {0} changed, {1} unchanged, {2} total." -f $changed.Count, $unchanged.Count, $files.Count)

if ($Check -and $drift) {
    Write-Host 'Embedded schema snapshot is OUT OF DATE.' -ForegroundColor Red
    exit 1
}

exit 0
