$ErrorActionPreference = 'Stop'

$pluginOutput = "d:\dev\EntertainingIsland\bin\Debug"
$pluginInstallDir = "d:\EntertainingIsland\data\Plugins\entertainingisland.app"
$classIslandExe = "d:\EntertainingIsland\ClassIsland_App\ClassIsland.Desktop.exe"

Write-Host "=== ClassIsland Plugin Deploy ==="

# Kill existing ClassIsland
Write-Host "Killing existing ClassIsland..."
Get-Process -Name "ClassIsland*" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Stopping: $($_.ProcessName) (PID: $($_.Id))"
    $_.Kill()
}
Start-Sleep -Seconds 2

# Clean and create install dir
if (Test-Path $pluginInstallDir) {
    Remove-Item $pluginInstallDir -Recurse -Force
}
New-Item -ItemType Directory -Path $pluginInstallDir -Force | Out-Null

# Copy build output
Write-Host "Copying files..."
Copy-Item -Path (Join-Path $pluginOutput "*") -Destination $pluginInstallDir -Recurse -Force

# List deployed files
Write-Host "Deployed to: $pluginInstallDir"
Write-Host ""
Write-Host "Files:"
Get-ChildItem $pluginInstallDir -File | ForEach-Object { Write-Host "  $($_.Name)" }

Write-Host ""
Write-Host "=== Deploy Complete ==="
Write-Host "Plugin ID: entertainingisland.app"

# Launch ClassIsland
if (Test-Path $classIslandExe) {
    Write-Host "Launching ClassIsland..."
    Start-Process $classIslandExe
}
