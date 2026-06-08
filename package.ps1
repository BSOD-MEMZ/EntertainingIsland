# ClassIsland 插件打包脚本
# 用法: .\package.ps1
# 输出: .\out\entertainingisland.app.zip

$ErrorActionPreference = 'Stop'

$pluginProject = $PSScriptRoot
$pluginId = "entertainingisland.app"
$outDir = "$pluginProject\out"
$publishDir = "$outDir\publish"
$zipFile = "$outDir\$pluginId.zip"

Write-Host "=== ClassIsland 插件打包脚本 ===" -ForegroundColor Cyan

# 1. 清理旧的输出
Write-Host "`n🧹 清理旧输出..." -ForegroundColor Yellow
if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# 2. 构建 Release 版本
Write-Host "`n🔨 构建 Release 版本..." -ForegroundColor Yellow
Set-Location $pluginProject
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 构建失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 构建成功" -ForegroundColor Green

# 3. 复制必要文件到发布目录
Write-Host "`n📦 收集打包文件..." -ForegroundColor Yellow

$buildOutput = "$pluginProject\bin\Release"
$allFiles = Get-ChildItem $buildOutput -Recurse -File

$skipExtensions = @('.pdb')
$skipNames = @()

foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Substring($buildOutput.Length + 1)
    $ext = $file.Extension.ToLower()

    # 跳过调试符号和不需要的文件
    if ($skipExtensions -contains $ext) {
        Write-Host "  ⏭ 跳过: $relativePath" -ForegroundColor DarkGray
        continue
    }

    $dest = Join-Path $publishDir $relativePath
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item $file.FullName $dest -Force
    Write-Host "  ✅ $relativePath" -ForegroundColor Gray
}

# 4. 确保必要文件都在（如果 manifest 或 icon 不在输出目录，从项目根复制）
if (-not (Test-Path "$publishDir\manifest.yml")) {
    Copy-Item "$pluginProject\manifest.yml" $publishDir -Force
}
if (-not (Test-Path "$publishDir\icon.png")) {
    Copy-Item "$pluginProject\icon.png" $publishDir -Force
}

# 4b. 复制 docs 文件夹
$docsSource = "$pluginProject\docs"
if (Test-Path $docsSource) {
    Write-Host "`n📁 复制 docs 文件夹..." -ForegroundColor Yellow
    $docsDest = "$publishDir\docs"
    Copy-Item $docsSource $docsDest -Recurse -Force
    Write-Host "  ✅ docs\" -ForegroundColor Gray
}

# 5. 打包为 zip
Write-Host "`n📦 创建插件包..." -ForegroundColor Yellow
if (Test-Path $zipFile) {
    Remove-Item $zipFile -Force
}

# 进入 publish 目录打包，使 zip 内结构为根目录即为插件文件
Push-Location $publishDir
try {
    Compress-Archive -Path * -DestinationPath $zipFile -Force
} finally {
    Pop-Location
}

# 6. 输出信息
Write-Host "`n✅ 打包完成！" -ForegroundColor Green
Write-Host ""
Write-Host "📦 输出文件: $zipFile" -ForegroundColor Cyan
$zipSize = (Get-Item $zipFile).Length
Write-Host "📏 文件大小: $([math]::Round($zipSize / 1KB, 1)) KB" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 包含文件:" -ForegroundColor Yellow
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipFile)
foreach ($entry in $zip.Entries | Sort-Object Name) {
    $sizeText = if ($entry.Length -gt 1KB) { "$([math]::Round($entry.Length / 1KB, 1)) KB" } else { "$($entry.Length) B" }
    Write-Host "  $($entry.FullName)  ($sizeText)" -ForegroundColor Gray
}
$zip.Dispose()
Write-Host ""
Write-Host "将此 .zip 文件上传到 ClassIsland 插件市场即可。" -ForegroundColor Green
