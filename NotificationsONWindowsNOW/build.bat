@echo off
chcp 65001 >nul
echo ============================================
echo   NotificationsONWindowsNOW - Build Script
echo ============================================
echo.

REM 1. 构建桥梁进程
echo [1/2] Building ToastBridge...
dotnet build Bridge\ToastBridge\ToastBridge.csproj -c Release
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
xcopy /Y /Q About\About.xml "%TARGET%\About\"
xcopy /Y /Q Bridge\ToastBridge.exe "%TARGET%\Bridge\"
xcopy /E /Y /Q /I "Languages\*" "%TARGET%\Languages\"
xcopy /Y /Q Source\NotificationsONWindowsNOW\*.cs "%TARGET%\Source\"
xcopy /Y /Q Source\NotificationsONWindowsNOW\*.csproj "%TARGET%\Source\"
xcopy /Y /Q Source\NotificationsONWindowsNOW\Directory.Build.props "%TARGET%\Source\"
if exist "Source\NotificationsONWindowsNOW\Properties" (
    if not exist "%TARGET%\Source\Properties" mkdir "%TARGET%\Source\Properties"
    xcopy /Y /Q "Source\NotificationsONWindowsNOW\Properties\*.cs" "%TARGET%\Source\Properties\"
)

echo ✓ Deployed!
pause
