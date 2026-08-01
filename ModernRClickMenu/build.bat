@echo off
chcp 65001 >nul
echo ============================================
echo   ModernRClickMenu - Build Script
echo ============================================
echo.

dotnet build Source\ModernRClickMenu\ModernRClickMenu.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] Build succeeded!
    echo      Output: Assemblies\ModernRClickMenu.dll
    echo.
    echo Deploying to RimWorld Mods...
    if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source"
    xcopy /E /Y /Q /I Assemblies\ModernRClickMenu.dll "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Assemblies\"
    xcopy /E /Y /Q /I About\About.xml "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\About\"
    if exist "About\Preview.png" xcopy /E /Y /Q /I About\Preview.png "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\About\"
    xcopy /Y /Q /I Source\ModernRClickMenu\*.cs "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\"
    xcopy /E /Y /Q /I Source\ModernRClickMenu\*.csproj "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\"
    xcopy /E /Y /Q /I Source\Directory.Build.props "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\"
    if exist "Source\ModernRClickMenu\Theme" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\Theme" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\Theme"
        xcopy /Y /Q "Source\ModernRClickMenu\Theme\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\Theme\"
    )
    if exist "Source\ModernRClickMenu\UI" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\UI" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\UI"
        xcopy /Y /Q "Source\ModernRClickMenu\UI\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\UI\"
    )
    if exist "Source\ModernRClickMenu\Properties" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\Properties" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\Properties"
        xcopy /Y /Q "Source\ModernRClickMenu\Properties\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernRClickMenu\Source\Properties\"
    )
    echo [OK] Deployed!
) else (
    echo.
    echo [FAIL] Build failed. Check errors above.
)

pause
