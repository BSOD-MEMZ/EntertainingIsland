# ClassIsland 插件部署和调试脚本
# 用法: .\deploy.ps1 [-Debug]

param([switch]$Debug)

$ErrorActionPreference = 'Stop'

# 路径配置
$pluginProject = $PSScriptRoot
$pluginOutput = "$pluginProject\bin\Debug"
$classIslandApp = "d:\EntertainingIsland\ClassIsland_App"
$pluginInstallDir = "$classIslandApp\data\Plugins\entertainingisland.app"  # 对应 manifest.yml 中的 id

Write-Host "=== ClassIsland 插件部署脚本 ===" -ForegroundColor Cyan

# 1. 构建插件
Write-Host "`n🔨 正在构建插件..." -ForegroundColor Yellow
Set-Location $pluginProject
dotnet build -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 构建失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 构建成功" -ForegroundColor Green

# 2. 复制插件到 ClassIsland Plugins 目录
Write-Host "`n📦 正在部署插件..." -ForegroundColor Yellow
if (Test-Path $pluginInstallDir) {
    Remove-Item $pluginInstallDir -Recurse -Force
}
New-Item -ItemType Directory -Path $pluginInstallDir -Force | Out-Null

# 复制所有构建输出
Copy-Item "$pluginOutput\*" $pluginInstallDir -Recurse -Force

Write-Host "✅ 插件已部署到: $pluginInstallDir" -ForegroundColor Green
Get-ChildItem $pluginInstallDir -File | Select-Object Name, @{N='Size';E={$_.Length}} | Format-Table -AutoSize

# 3. 启动 ClassIsland
if ($Debug) {
    Write-Host "`n🚀 正在启动 ClassIsland（调试模式）..." -ForegroundColor Yellow
    Write-Host "   请在 IDE 中附加调试器或使用热重载。" -ForegroundColor DarkGray
}

$classIslandExe = "$classIslandApp\ClassIsland.exe"
if (Test-Path $classIslandExe) {
    Write-Host "`n🚀 启动 ClassIsland..." -ForegroundColor Green
    Start-Process $classIslandExe
} else {
    Write-Host "`n⚠️  ClassIsland.exe 未找到: $classIslandExe" -ForegroundColor Red
    Write-Host "   请先下载 ClassIsland 发行版。" -ForegroundColor Red
}

Write-Host "`n=== 部署完成 ===" -ForegroundColor Cyan
Write-Host "插件 ID: entertainingisland.app"
Write-Host "安装路径: $pluginInstallDir"
