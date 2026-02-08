# Update copyright year in Directory.Build.props

[CmdletBinding()]
param(
    [string]$Root = (Split-Path -Parent $MyInvocation.MyCommand.Path),
    [switch]$Check
)

Set-Location (Resolve-Path $Root)

$propsPath = "..\Directory.Build.props"
$currentYear = (Get-Date).Year

if (-not (Test-Path $propsPath)) {
    Write-Host "Error: Directory.Build.props not found at $propsPath" -ForegroundColor Red
    exit 1
}

# Read the file
$content = Get-Content $propsPath -Raw

$initialYear = 2026

# Pattern to match: Copyright (c) YYYY or Copyright (c) YYYY-YYYY
$pattern = '(<Copyright>Copyright \(c\) )(\d{4})(?:-(\d{4}))?( Marc Roca Musach</Copyright>)'

if ($content -match $pattern) {
    $existingInitialYear = $Matches[2]
    $existingEndYear = if ($Matches[3]) { $Matches[3] } else { $existingInitialYear }
    
    # Determine the new format
    if ($currentYear -eq $initialYear) {
        $newYearText = "$initialYear"
    }
    else {
        $newYearText = "$initialYear-$currentYear"
    }
    
    if ($existingEndYear -eq $currentYear -and $existingInitialYear -eq $initialYear) {
        Write-Host "Copyright year is already up to date: $newYearText" -ForegroundColor Green
        
        if ($Check) {
            exit 0
        }
    }
    else {
        Write-Host "Updating copyright year from $existingInitialYear-$existingEndYear to $newYearText"
        
        if ($Check) {
            Write-Host "Copyright year update required." -ForegroundColor Yellow
            exit 1
        }
        
        # Update the year
        $newContent = $content -replace $pattern, "`$1$newYearText`$4"
        
        # Write back to file
        Set-Content $propsPath $newContent -NoNewline
        
        Write-Host "Successfully updated copyright year to $newYearText" -ForegroundColor Green
    }
}
else {
    Write-Host "Error: Could not find copyright pattern in Directory.Build.props" -ForegroundColor Red
    exit 1
}
