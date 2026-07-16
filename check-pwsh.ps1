# Check if PowerShell 7 is installed
$paths = @(
    "C:\Program Files\PowerShell\7\pwsh.exe",
    "$env:LOCALAPPDATA\Microsoft\WindowsApps\pwsh.exe"
)

$found = $false
foreach ($p in $paths) {
    if (Test-Path $p) {
        Write-Host "Found: $p"
        & $p -Command '$PSVersionTable.PSVersion'
        $found = $true
        break
    }
}

if (-not $found) {
    Write-Host "PowerShell 7 not found."
    Write-Host "Current pwsh version:"
    pwsh -Command '$PSVersionTable.PSVersion'
}

# Also check PATH
Write-Host "`nPATH entries for pwsh:"
$env:PATH -split ";" | Where-Object { $_ -like "*owerShell*" }
