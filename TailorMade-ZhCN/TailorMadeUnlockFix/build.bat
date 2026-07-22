@echo off
chcp 65001 >nul
echo ============================================
echo   TailorMadeUnlockFix - Build Script
echo ============================================
echo.

dotnet build Source\TailorMadeUnlockFix\TailorMadeUnlockFix.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Build succeeded!
    echo   Output: Assemblies\TailorMadeUnlockFix.dll
    echo.
    echo Deploying to RimWorld Mods...
    if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source"
    xcopy /E /Y /Q /I Assemblies\TailorMadeUnlockFix.dll "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Assemblies\"
    xcopy /E /Y /Q /I About\About.xml "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\About\"
    xcopy /E /Y /Q /I About\Preview.png "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\About\"
    xcopy /E /Y /Q /I Source\TailorMadeUnlockFix\*.cs "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
    xcopy /E /Y /Q /I Source\TailorMadeUnlockFix\*.csproj "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
    xcopy /E /Y /Q /I Source\TailorMadeUnlockFix\Directory.Build.props "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\"
    if exist "Source\TailorMadeUnlockFix\Properties" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties"
        xcopy /Y /Q "Source\TailorMadeUnlockFix\Properties\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Source\Properties\"
    )
    echo ✓ Deployed!
    echo ✓ Deployed!
) else (
    echo.
    echo ✗ Build failed. Check errors above.
)

pause
