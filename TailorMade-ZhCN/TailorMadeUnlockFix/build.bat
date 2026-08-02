@echo off
chcp 65001 >nul
echo ============================================
echo   TailorMadeUnlockFix - 构建部署脚本
echo ============================================
echo.

dotnet build Source\TailorMadeUnlockFix\TailorMadeUnlockFix.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ 编译成功！
    echo   输出: Assemblies\TailorMadeUnlockFix.dll
    echo.
    echo 部署到 RimWorld Mods...
    if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source"
    xcopy /E /Y /Q /I Assemblies\TailorMadeUnlockFix.dll "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Assemblies\"
    xcopy /E /Y /Q /I About "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\About\"
    if exist "Patches" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Patches" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Patches"
        xcopy /E /Y /Q "Patches" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Patches\"
    )
    xcopy /E /Y /Q /I Source\TailorMadeUnlockFix\*.cs "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
    xcopy /E /Y /Q /I Source\TailorMadeUnlockFix\*.csproj "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
    xcopy /E /Y /Q /I Source\TailorMadeUnlockFix\Directory.Build.props "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
    if exist "Source\TailorMadeUnlockFix\Properties" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties"
        xcopy /Y /Q "Source\TailorMadeUnlockFix\Properties\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties\"
    )
    echo ✓ 部署完成！
) else (
    echo.
    echo ✗ 编译失败，请查看上方错误。
    pause
    exit /b 1
)

pause
