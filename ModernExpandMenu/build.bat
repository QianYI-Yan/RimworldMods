@echo off
chcp 65001 >nul
echo ============================================
echo   ModernExpandMenu - Build Script
echo ============================================
echo.

dotnet build Source\ModernExpandMenu\ModernExpandMenu.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] Build succeeded!
    echo      Output: Assemblies\ModernExpandMenu.dll
    echo.
    echo Deploying to RimWorld Mods...
    if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source"
    :: 清理可能残留的构建中间产物（bin/obj 不应部署到游戏目录）
    if exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\bin" rd /s /q "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\bin"
    if exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\obj" rd /s /q "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\obj"
    :: 部署多语言翻译
    if exist "Languages" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Languages" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Languages"
        xcopy /E /Y /Q /I Languages "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Languages\"
    )
    xcopy /E /Y /Q /I Assemblies\ModernExpandMenu.dll "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Assemblies\"
    xcopy /E /Y /Q /I About\About.xml "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\About\"
    if exist "About\Preview.png" xcopy /E /Y /Q /I About\Preview.png "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\About\"
    xcopy /Y /Q /I Source\ModernExpandMenu\*.cs "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\"
    xcopy /E /Y /Q /I Source\ModernExpandMenu\*.csproj "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\"
    xcopy /E /Y /Q /I Source\Directory.Build.props "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\"
    if exist "Source\ModernExpandMenu\Theme" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\Theme" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\Theme"
        xcopy /Y /Q "Source\ModernExpandMenu\Theme\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\Theme\"
    )
    if exist "Source\ModernExpandMenu\UI" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\UI" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\UI"
        xcopy /Y /Q "Source\ModernExpandMenu\UI\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\UI\"
    )
    if exist "Source\ModernExpandMenu\Properties" (
        if not exist "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\Properties" mkdir "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\Properties"
        xcopy /Y /Q "Source\ModernExpandMenu\Properties\*.cs" "D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu\Source\Properties\"
    )
    echo [OK] Deployed!
) else (
    echo.
    echo [FAIL] Build failed. Check errors above.
)

pause
