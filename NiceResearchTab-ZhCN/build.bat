@echo off
chcp 65001 >nul
echo ========================================
echo   NiceResearchTab-ZhCN 汉化构建部署脚本
echo ========================================
echo.

set MODS_DIR=D:\app\game\steam\steamapps\common\RimWorld\Mods\NiceResearchTab-ZhCN

echo ========== [1/3] 编译硬编码补丁 ==========
dotnet build Source\NiceResearchTabZhCN\NiceResearchTabZhCN.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 补丁编译失败
    pause
    exit /b 1
)
echo ✓ 补丁编译成功

echo ========== [2/3] 更新项目根 Assemblies（编译产物回流） ==========
if not exist Assemblies mkdir Assemblies
copy /Y "Source\NiceResearchTabZhCN\bin\Release\net472\NiceResearchTabZhCN.dll" "Assemblies\NiceResearchTabZhCN.dll"
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 产物复制失败
    pause
    exit /b 1
)
echo ✓ 项目根 Assemblies 已更新

echo ========== [3/3] 部署到游戏 Mods ==========
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
