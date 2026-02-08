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

# Pattern to match: Copyright (c) 2024-YYYY Marc Roca Musach
# We want to update the end year to current year
$pattern = '(<Copyright>Copyright \(c\) \d{4}-)(\d{4})( Marc Roca Musach</Copyright>)'

if ($content -match $pattern) {
    $oldYear = $Matches[2]
    
    if ($oldYear -eq $currentYear) {
        Write-Host "Copyright year is already up to date: $currentYear" -ForegroundColor Green
        
        if ($Check) {
            exit 0
        }
    }
    else {
        Write-Host "Updating copyright year from $oldYear to $currentYear"
        
        if ($Check) {
            Write-Host "Copyright year update required." -ForegroundColor Yellow
            exit 1
        }
        
        # Update the year
        $newContent = $content -replace $pattern, "`$1$currentYear`$3"
        
        # Write back to file
        Set-Content $propsPath $newContent -NoNewline
        
        Write-Host "Successfully updated copyright year to $currentYear" -ForegroundColor Green
    }
}
else {
    Write-Host "Error: Could not find copyright pattern in Directory.Build.props" -ForegroundColor Red
    exit 1
}
