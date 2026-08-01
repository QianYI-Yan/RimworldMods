@echo off
chcp 65001 >nul
echo ========================================
echo   asrtylsUIMod-ZhCN 汉化构建部署脚本
echo ========================================
echo.

set MODS_DIR=D:\app\game\steam\steamapps\common\RimWorld\Mods\asrtylsUIMod-ZhCN

echo ========== [1/3] 编译硬编码补丁 ==========
dotnet build Source\AstrylsUIZhCN\AstrylsUIZhCN.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 补丁编译失败
    pause
    exit /b 1
)
echo ✓ 补丁编译成功

echo ========== [2/3] 更新项目根 Assemblies（编译产物回流） ==========
if not exist Assemblies mkdir Assemblies
copy /Y "Source\AstrylsUIZhCN\bin\Release\net472\AstrylsUIZhCN.dll" "Assemblies\AstrylsUIZhCN.dll"
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
if exist Source\*.csproj (
    if not exist "%MODS_DIR%\Source" mkdir "%MODS_DIR%\Source"
    xcopy /Y /E /Q Source "%MODS_DIR%\Source\"
)
echo ✓ 部署完成: %MODS_DIR%

echo.
echo ========== 全部完成 ==========
pause
