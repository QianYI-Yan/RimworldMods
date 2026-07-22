@echo off
chcp 65001 >nul
echo ========================================
echo   TailorMadeZhCN + UnlockFix 编译脚本
echo ========================================
echo.

echo ========== [1/2] 汉化模组 ==========
dotnet build Source\TailorMadeZhCN\TailorMadeZhCN.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 汉化编译失败
    pause
    exit /b 1
)
echo ✓ 汉化编译成功

echo Deploying Chinese translation to Mods...
xcopy /Y /Q Assemblies\TailorMadeZhCN.dll "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Assemblies\"
if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source"
xcopy /Y /Q Source\TailorMadeZhCN\*.cs "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source\"
xcopy /Y /Q Source\TailorMadeZhCN\*.csproj "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source\"
xcopy /Y /Q Source\Directory.Build.props "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source\"
if exist "Source\TailorMadeZhCN\Properties" (
    if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source\Properties" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source\Properties"
    xcopy /Y /Q "Source\TailorMadeZhCN\Properties\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMade-ZhCN\Source\Properties\"
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

echo Deploying TailorMadeUnlockFix to Mods...
xcopy /Y /Q TailorMadeUnlockFix\Assemblies\TailorMadeUnlockFix.dll "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Assemblies\"
if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source"
xcopy /Y /Q TailorMadeUnlockFix\Source\TailorMadeUnlockFix\*.cs "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
xcopy /Y /Q TailorMadeUnlockFix\Source\TailorMadeUnlockFix\*.csproj "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
xcopy /Y /Q TailorMadeUnlockFix\Source\TailorMadeUnlockFix\Directory.Build.props "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
if exist "TailorMadeUnlockFix\Source\TailorMadeUnlockFix\Properties" (
    if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties"
    xcopy /Y /Q "TailorMadeUnlockFix\Source\TailorMadeUnlockFix\Properties\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties\"
)
echo ✓ 修复模组部署完成

echo.
echo ========== 全部完成 ==========
echo   汉化: Assemblies\TailorMadeZhCN.dll
echo   修复: TailorMadeUnlockFix\Assemblies\TailorMadeUnlockFix.dll

pause
