@echo off
chcp 65001 >nul
echo ============================================
echo   ModernExpandMenu - 构建部署脚本
echo ============================================
echo.

set MODS_DIR=D:\app\game\steam\steamapps\common\RimWorld\Mods\ModernExpandMenu

dotnet build Source\ModernExpandMenu\ModernExpandMenu.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ 编译成功！
    echo     输出: Assemblies\ModernExpandMenu.dll
    echo.
    echo 部署到 RimWorld Mods...
    if not exist "%MODS_DIR%\Source" mkdir "%MODS_DIR%\Source"
    :: 清理可能残留的构建中间产物（bin/obj 不应部署到游戏目录）
    if exist "%MODS_DIR%\Source\bin" rd /s /q "%MODS_DIR%\Source\bin"
    if exist "%MODS_DIR%\Source\obj" rd /s /q "%MODS_DIR%\Source\obj"
    :: 部署多语言翻译
    if exist "Languages" (
        if not exist "%MODS_DIR%\Languages" mkdir "%MODS_DIR%\Languages"
        xcopy /E /Y /Q /I Languages "%MODS_DIR%\Languages\"
    )
    xcopy /E /Y /Q /I Assemblies\ModernExpandMenu.dll "%MODS_DIR%\Assemblies\"
    xcopy /E /Y /Q /I About "%MODS_DIR%\About\"
    xcopy /Y /Q /I Source\ModernExpandMenu\*.cs "%MODS_DIR%\Source\"
    xcopy /E /Y /Q /I Source\ModernExpandMenu\*.csproj "%MODS_DIR%\Source\"
    xcopy /E /Y /Q /I Source\Directory.Build.props "%MODS_DIR%\Source\"
    if exist "Source\ModernExpandMenu\Theme" (
        if not exist "%MODS_DIR%\Source\Theme" mkdir "%MODS_DIR%\Source\Theme"
        xcopy /Y /Q "Source\ModernExpandMenu\Theme\*.cs" "%MODS_DIR%\Source\Theme\"
    )
    if exist "Source\ModernExpandMenu\UI" (
        if not exist "%MODS_DIR%\Source\UI" mkdir "%MODS_DIR%\Source\UI"
        xcopy /Y /Q "Source\ModernExpandMenu\UI\*.cs" "%MODS_DIR%\Source\UI\"
    )
    if exist "Source\ModernExpandMenu\Properties" (
        if not exist "%MODS_DIR%\Source\Properties" mkdir "%MODS_DIR%\Source\Properties"
        xcopy /Y /Q "Source\ModernExpandMenu\Properties\*.cs" "%MODS_DIR%\Source\Properties\"
    )
    echo ✓ 部署完成！
) else (
    echo.
    echo ✗ 编译失败，请查看上方错误。
    pause
    exit /b 1
)

pause
