Write-Host "=== .NET 8.0 SDK 安装脚本 ==="
Write-Host "正在下载 dotnet-install 脚本..."
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$env:TEMP\dotnet-install.ps1" -UseBasicParsing

Write-Host "正在安装 .NET 8.0 SDK 到 C:\Program Files\dotnet ..."
& "$env:TEMP\dotnet-install.ps1" -Channel 8.0 -InstallDir "C:\Program Files\dotnet"

Write-Host "安装完成！正在验证..."
dotnet --version
dotnet --list-sdks
