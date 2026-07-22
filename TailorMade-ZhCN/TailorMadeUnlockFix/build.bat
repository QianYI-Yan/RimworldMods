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
    xcopy /E /Y /Q "Assemblies\TailorMadeUnlockFix.dll" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Assemblies\"
    xcopy /E /Y /Q "Patches\RestoreOnlyUseRaceRestricted.xml" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\Patches\"
    xcopy /E /Y /Q "About\About.xml" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\About\"
    xcopy /E /Y /Q "About\Preview.png" "D:\app\game\steam\steamapps\common\RimWorld\Mods\TailorMadeUnlockFix\About\"
    echo ✓ Deployed!
) else (
    echo.
    echo ✗ Build failed. Check errors above.
)

pause
