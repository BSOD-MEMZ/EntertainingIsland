@echo off
setlocal enabledelayedexpansion
echo =============================================
echo   EntertainingIsland 开发环境一键部署
echo =============================================
echo.

REM ==========================================
REM 步骤 1: 安装 .NET 8.0 SDK
REM ==========================================
echo [1/3] 检查 .NET SDK...

WHERE dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 未找到 dotnet，请先安装 .NET 8.0 SDK
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM 检查 SDK 版本
dotnet --list-sdks 2>nul | findstr "8." >nul
if %ERRORLEVEL% NEQ 0 (
    echo .NET 8.0 SDK 未安装，正在下载安装...
    echo.
    
    REM 下载 dotnet-install 脚本
    powershell -ExecutionPolicy Bypass -Command "Write-Host '下载安装脚本...'; Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '%TEMP%\dotnet-install.ps1' -UseBasicParsing"
    if %ERRORLEVEL% NEQ 0 (
        echo [错误] 下载失败，请手动安装 .NET 8.0 SDK
        echo 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
        pause
        exit /b 1
    )
    
    REM 运行安装脚本
    powershell -ExecutionPolicy Bypass -File "%TEMP%\dotnet-install.ps1" -Channel 8.0 -InstallDir "C:\Program Files\dotnet"
    if %ERRORLEVEL% NEQ 0 (
        echo [错误] 安装失败，请手动安装 .NET 8.0 SDK
        pause
        exit /b 1
    )
    
    echo .NET 8.0 SDK 安装完成！
) else (
    echo .NET 8.0 SDK 已安装。
)

REM 确保 dotnet 在 PATH 中
set "PATH=C:\Program Files\dotnet;%PATH%"

echo.
echo [2/3] 构建插件...
cd /d "d:\dev\EntertainingIsland"

dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 包还原失败！
    pause
    exit /b 1
)

dotnet build -c Debug
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 构建失败！
    pause
    exit /b 1
)

echo.
echo [3/3] 部署插件到 ClassIsland...
powershell -ExecutionPolicy Bypass -File "d:\dev\EntertainingIsland\deploy.ps1"

echo.
echo =============================================
echo   全部完成！
echo.
echo   启动 ClassIsland:
echo   d:\EntertainingIsland\ClassIsland_App\ClassIsland.Desktop.exe
echo =============================================
pause
pause
echo.
echo   接下来运行部署脚本：
echo   powershell -ExecutionPolicy Bypass -File "d:\dev\EntertainingIsland\deploy.ps1"
echo ========================================
pause
