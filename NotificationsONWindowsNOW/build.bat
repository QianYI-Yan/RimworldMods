@echo off
chcp 65001 >nul
echo ============================================
echo   NotificationsONWindowsNOW - Build Script
echo ============================================
echo.

REM 1. 构建桥梁进程
echo [1/2] Building ToastBridge...
dotnet build Source\ToastBridge\ToastBridge.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ✗ ToastBridge build failed.
    pause
    exit /b 1
)

REM 2. 构建模组 DLL
echo [2/2] Building NotificationsONWindowsNOW...
dotnet build Source\NotificationsONWindowsNOW\NotificationsONWindowsNOW.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ✗ Mod build failed.
    pause
    exit /b 1
)

echo.
echo ✓ Build succeeded!
echo.
echo Deploying to RimWorld Mods...
set TARGET=D:\app\game\steam\steamapps\common\RimWorld\Mods\NotificationsONWindowsNOW

if not exist "%TARGET%\Assemblies" mkdir "%TARGET%\Assemblies"
if not exist "%TARGET%\Bridge" mkdir "%TARGET%\Bridge"
if not exist "%TARGET%\Source" mkdir "%TARGET%\Source"
if not exist "%TARGET%\Languages" mkdir "%TARGET%\Languages"

xcopy /Y /Q Assemblies\NotificationsONWindowsNOW.dll "%TARGET%\Assemblies\"
:: 部署整个 About 目录（黑名单思路：About.xml / Preview.png / PublishedFileId.txt 等全部复制）
if not exist "%TARGET%\About" mkdir "%TARGET%\About"
xcopy /E /Y /Q "About\*" "%TARGET%\About\"
xcopy /Y /Q Bridge\ToastBridge.exe "%TARGET%\Bridge\"
xcopy /E /Y /Q /I "Languages\*" "%TARGET%\Languages\"
:: 部署源码（先清空旧源码树避免残留，robocopy 黑名单整体复制，仅排除 bin/obj）
if exist "%TARGET%\Source" rd /s /q "%TARGET%\Source"
robocopy "Source" "%TARGET%\Source" /E /XD bin obj /NFL /NDL /NJH /NJS >nul

echo ✓ Deployed!
pause
