@echo off
chcp 65001 >nul
echo ========================================
echo   TailorMadeZhCN + UnlockFix 编译脚本
echo ========================================
echo.

set TARGET_ZhCN=D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN
set TARGET_Fix=D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix

echo ========== [1/2] 汉化模组 ==========
dotnet build Source\TailorMadeZhCN\TailorMadeZhCN.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 汉化编译失败
    pause
    exit /b 1
)
echo ✓ 汉化编译成功

echo 部署汉化到游戏 Mods...
if not exist "%TARGET_ZhCN%\About" mkdir "%TARGET_ZhCN%\About"
xcopy /Y /E /Q About "%TARGET_ZhCN%\About\"
if not exist "%TARGET_ZhCN%\Languages" mkdir "%TARGET_ZhCN%\Languages"
xcopy /Y /E /Q Languages "%TARGET_ZhCN%\Languages\"
xcopy /Y /Q Assemblies\TailorMadeZhCN.dll "%TARGET_ZhCN%\Assemblies\"
if not exist "%TARGET_ZhCN%\Source" mkdir "%TARGET_ZhCN%\Source"
xcopy /Y /Q Source\TailorMadeZhCN\*.cs "%TARGET_ZhCN%\Source\"
xcopy /Y /Q Source\TailorMadeZhCN\*.csproj "%TARGET_ZhCN%\Source\"
xcopy /Y /Q Source\Directory.Build.props "%TARGET_ZhCN%\Source\"
if exist "Source\TailorMadeZhCN\Properties" (
    if not exist "%TARGET_ZhCN%\Source\Properties" mkdir "%TARGET_ZhCN%\Source\Properties"
    xcopy /Y /Q "Source\TailorMadeZhCN\Properties\*.cs" "%TARGET_ZhCN%\Source\Properties\"
)
echo ✓ 汉化部署完成

echo.
echo ========== [2/2] TailorMadeUnlockFix ==========
dotnet build TailorMadeUnlockFix\Source\TailorMadeUnlockFix\TailorMadeUnlockFix.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 修复模组编译失败
    pause
    exit /b 1
)
echo ✓ 修复模组编译成功

echo 部署 TailorMadeUnlockFix 到游戏 Mods...
if not exist "%TARGET_Fix%\About" mkdir "%TARGET_Fix%\About"
xcopy /Y /E /Q TailorMadeUnlockFix\About "%TARGET_Fix%\About\"
if exist TailorMadeUnlockFix\Patches (
    if not exist "%TARGET_Fix%\Patches" mkdir "%TARGET_Fix%\Patches"
    xcopy /Y /E /Q TailorMadeUnlockFix\Patches "%TARGET_Fix%\Patches\"
)
xcopy /Y /Q TailorMadeUnlockFix\Assemblies\TailorMadeUnlockFix.dll "%TARGET_Fix%\Assemblies\"
if not exist "%TARGET_Fix%\Source" mkdir "%TARGET_Fix%\Source"
xcopy /Y /Q TailorMadeUnlockFix\Source\TailorMadeUnlockFix\*.cs "%TARGET_Fix%\Source\"
xcopy /Y /Q TailorMadeUnlockFix\Source\TailorMadeUnlockFix\*.csproj "%TARGET_Fix%\Source\"
xcopy /Y /Q TailorMadeUnlockFix\Source\TailorMadeUnlockFix\Directory.Build.props "%TARGET_Fix%\Source\"
if exist "TailorMadeUnlockFix\Source\TailorMadeUnlockFix\Properties" (
    if not exist "%TARGET_Fix%\Source\Properties" mkdir "%TARGET_Fix%\Source\Properties"
    xcopy /Y /Q "TailorMadeUnlockFix\Source\TailorMadeUnlockFix\Properties\*.cs" "%TARGET_Fix%\Source\Properties\"
)
echo ✓ 修复模组部署完成

echo.
echo ========== 全部完成 ==========
echo   汉化: Assemblies\TailorMadeZhCN.dll
echo   修复: TailorMadeUnlockFix\Assemblies\TailorMadeUnlockFix.dll

pause
