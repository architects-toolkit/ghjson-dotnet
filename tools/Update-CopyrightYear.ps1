# Update copyright year in Directory.Build.props

[CmdletBinding()]
param(
    [string]$Root = (Split-Path -Parent $MyInvocation.MyCommand.Path),
    [switch]$Check
)

$repoRoot = Split-Path -Parent $Root
Set-Location (Resolve-Path $Root)

$currentYear = (Get-Date).Year
$initialYear = 2026
$changedCount = 0

# Determine the new year format
if ($currentYear -eq $initialYear) {
    $newYears = "$initialYear"
}
else {
    $newYears = "$initialYear-$currentYear"
}

# Update Directory.Build.props
$propsPath = Join-Path $repoRoot "Directory.Build.props"
if (Test-Path $propsPath) {
    try {
        $content = [System.IO.File]::ReadAllText($propsPath, [System.Text.Encoding]::UTF8)
        $originalContent = $content

        # <Copyright> tag
        $tagPattern = '(<Copyright>Copyright \(c\) )(\d{4})(?:-(\d{4}))?( Marc Roca Musach</Copyright>)'
        if ($content -match $tagPattern) {
            $startYear = [int]$Matches[2]
            $endYear = if ($Matches[3]) { [int]$Matches[3] } else { $startYear }
            
            if (-not ($endYear -eq $currentYear -and $startYear -eq $initialYear)) {
                $replacement = '$1' + $newYears + '$4'
                $content = [System.Text.RegularExpressions.Regex]::Replace($content, $tagPattern, $replacement)
            }
        }

        if ($content -ne $originalContent) {
            $changedCount++
            if (-not $Check) {
                [System.IO.File]::WriteAllText($propsPath, $content, [System.Text.Encoding]::UTF8)
                Write-Host "Updated copyright year to $newYears : $propsPath"
            }
        }
    }
    catch {
        Write-Host "Error updating ${propsPath}: $($_.Exception.Message)" -ForegroundColor Red
        if ($Check) {
            exit 2
        }
    }
}

if ($Check) {
    if ($changedCount -gt 0) {
        Write-Host "Copyright year update required in $changedCount file(s)." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "All copyright years already up to date." -ForegroundColor Green
    exit 0
}

Write-Host "Copyright year update complete. Updated $changedCount file(s)."
