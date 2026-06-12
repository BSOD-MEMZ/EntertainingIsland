# EntertainingIsland 交互式发布脚本
# 用法: .\release.ps1
# 流程: 打开 Typora 写 Release Notes → 构建打包 → 自动追加 MD5 → 生成最终 Release 说明

param(
    [switch]$SkipEditor  # 跳过编辑器，仅构建+追加 MD5
)

$ErrorActionPreference = 'Stop'

$pluginProject = $PSScriptRoot
$outDir = "$pluginProject\out"
$notesFile = "$outDir\release-notes.md"
$bodyFile = "$outDir\release-body.md"
$manifestPath = "$pluginProject\manifest.yml"
$manifest = if (Test-Path $manifestPath) { Get-Content $manifestPath -Raw } else { '' }

Write-Host "=== EntertainingIsland 发布向导 ===" -ForegroundColor Cyan

# 1. 确保 out 目录存在
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

# 2. 打开编辑器写 Release Notes
if (-not $SkipEditor) {
    # 查找 Typora 安装路径
    $typoraPaths = @(
        "E:\Program Files\Typora\Typora.exe"
        "$env:LOCALAPPDATA\Programs\Typora\Typora.exe",
        "$env:ProgramFiles\Typora\Typora.exe",
        "${env:ProgramFiles(x86)}\Typora\Typora.exe",
        (Get-Command typora -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source)
    )
    $typora = $typoraPaths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

    # 准备初始内容
    $initialContent = @"
# {version}

## 新增

- 

## 修复

- 

## 其他

- 
"@
    # 尝试从 manifest.yml 读取版本号
    if ($manifest -match '(?m)^version:\s*([0-9.]+)') {
        $initialContent = $initialContent -replace '\{version\}', $Matches[1]
    }
    else {
        $initialContent = $initialContent -replace '\{version\}', '?.?.?.?'
    }

    if (-not (Test-Path $notesFile)) {
        $initialContent | Set-Content $notesFile -Encoding UTF8
    }
    else {
        # 文件已存在（上次发布遗留），检查标题版本号是否匹配当前版本
        $existing = Get-Content $notesFile -Raw -Encoding UTF8
        $currentVer = if ($manifest -match '(?m)^version:\s*([0-9.]+)') { $Matches[1] } else { '' }
        if ($currentVer -and $existing -notmatch [regex]::Escape("# $currentVer")) {
            Write-Host "⚠️  检测到 release-notes.md 版本号不匹配，已刷新模板。" -ForegroundColor Yellow
            $initialContent | Set-Content $notesFile -Encoding UTF8
        }
    }

    if ($typora) {
        Write-Host "`n📝 正在打开 Typora 编写 Release Notes..." -ForegroundColor Yellow
        Write-Host "   写完保存后关闭 Typora 窗口即可继续。" -ForegroundColor DarkGray
        $proc = Start-Process $typora -ArgumentList $notesFile -PassThru
        $proc.WaitForExit()
        Write-Host "✅ Typora 已关闭" -ForegroundColor Green
    }
    else {
        Write-Host "`n⚠️  未找到 Typora，使用记事本打开。" -ForegroundColor Yellow
        Write-Host "   写完保存后关闭记事本窗口即可继续。" -ForegroundColor DarkGray
        Start-Process notepad -ArgumentList $notesFile -Wait
    }
}

# 3. 确认继续
Write-Host ""
$notes = Get-Content $notesFile -Raw -Encoding UTF8
Write-Host "📋 Release Notes 预览:" -ForegroundColor Yellow
Write-Host ("─" * 40)
Write-Host $notes
Write-Host ("─" * 40)

if (-not $SkipEditor) {
    $confirm = Read-Host "`n确认构建并发布? [Y/n]"
    if ($confirm -ne '' -and $confirm -ne 'y' -and $confirm -ne 'Y') {
        Write-Host "❌ 已取消。" -ForegroundColor Red
        exit 0
    }
}

# 4. 构建打包
Write-Host "`n🔨 正在构建打包..." -ForegroundColor Yellow
& "$pluginProject\package.ps1" -Ci
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 打包失败！" -ForegroundColor Red
    exit 1
}

# 5. 读取当前 release-body.md 中的 MD5，追加到用户 Release Notes 末尾
$autoBody = Get-Content $bodyFile -Raw -Encoding UTF8
# 提取 MD5 注释行
$md5Line = ($autoBody -split "`n" | Where-Object { $_ -match 'CLASSISLAND_PKG_MD5' }) -join "`n"

if (-not $md5Line) {
    Write-Host "❌ 未在 release-body.md 中找到 MD5 标记！" -ForegroundColor Red
    exit 1
}

# 6. 拼接最终 Release 说明（用户笔记 + 空行 + MD5）
$finalBody = @"
$($notes.TrimEnd())

$md5Line
"@
$finalBody | Set-Content $bodyFile -Encoding UTF8

# 也写入仓库根目录供 CI 读取（out/ 已被 gitignore）
$ciBodyFile = "$pluginProject\RELEASE_BODY.md"
$finalBody | Set-Content $ciBodyFile -Encoding UTF8

Write-Host "`n✅ 发布准备完成！" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Release 说明: $bodyFile" -ForegroundColor Cyan
Write-Host ""

# 7. 仅提交 RELEASE_BODY.md（产物由 CI 构建）
$tagVersion = if ($manifest -match '(?m)^version:\s*([0-9.]+)') { $Matches[1] } else { '?.?.?.?' }

Write-Host "📤 提交 Release 说明..." -ForegroundColor Yellow
Set-Location $pluginProject
git add RELEASE_BODY.md 2>$null
$hasChanges = git status --porcelain RELEASE_BODY.md
if ($hasChanges) {
    git commit -m "发布 v$tagVersion 说明"
    Write-Host "✅ 已提交 RELEASE_BODY.md" -ForegroundColor Green
}
else {
    Write-Host "  (无变更)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "🚀 推送 tag 即可自动构建发布:" -ForegroundColor Yellow
Write-Host "   git push origin master"
Write-Host "   git tag $tagVersion"
Write-Host "   git push origin $tagVersion"
