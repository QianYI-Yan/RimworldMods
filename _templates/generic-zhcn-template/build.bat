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
:: Source 部署（csproj 可能在顶层或子目录，用递归查找判断）
dir /b /s Source\*.csproj >nul 2>&1 && (
    if not exist "%MODS_DIR%\Source" mkdir "%MODS_DIR%\Source"
    xcopy /Y /E /Q Source "%MODS_DIR%\Source\"
    :: 清理编译中间产物（bin/obj 不应部署到游戏目录）
    for /d /r "%MODS_DIR%\Source" %%D in (bin obj) do if exist "%%D" rd /s /q "%%D"
)
echo ✓ 部署完成: %MODS_DIR%

echo.
echo ========== 全部完成 ==========
pause
