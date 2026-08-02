@echo off
chcp 65001 >nul
echo ========================================
echo   【模组名】汉化部署脚本
echo ========================================
echo.

set MODS_DIR=D:\app\game\steam\steamapps\common\RimWorld\Mods\【模组目录名】

echo ========== 部署到游戏 Mods ==========
if not exist "%MODS_DIR%" mkdir "%MODS_DIR%"
xcopy /Y /E /Q About "%MODS_DIR%\About\"
xcopy /Y /E /Q Languages "%MODS_DIR%\Languages\"
if exist Assemblies\*.dll (
    if not exist "%MODS_DIR%\Assemblies" mkdir "%MODS_DIR%\Assemblies"
    xcopy /Y /Q Assemblies\*.dll "%MODS_DIR%\Assemblies\"
)
if exist Source\*.csproj (
    if not exist "%MODS_DIR%\Source" mkdir "%MODS_DIR%\Source"
    xcopy /Y /E /Q Source "%MODS_DIR%\Source\"
)
echo ✓ 部署完成: %MODS_DIR%

echo.
echo ========== 全部完成 ==========
pause
