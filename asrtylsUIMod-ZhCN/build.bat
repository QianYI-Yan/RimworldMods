@echo off
chcp 65001 >nul
echo ========================================
echo   asrtylsUIMod-ZhCN 汉化构建部署脚本
echo ========================================
echo.

set MODS_DIR=D:\app\game\steam\steamapps\common\RimWorld\Mods\asrtylsUIMod-ZhCN

echo ========== [1/6] 编译主框架 ==========
dotnet build Source\AstrylsUIZhCN\AstrylsUIZhCN.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 主框架编译失败
    pause
    exit /b 1
)
echo ✓ 主框架编译成功

echo ========== [2/6] 编译 Learning Menu ==========
dotnet build Source\AstrylsUIZhCN.LearningMenu\AstrylsUIZhCN.LearningMenu.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ Learning Menu 编译失败
    pause
    exit /b 1
)
echo ✓ Learning Menu 编译成功

echo ========== [3/6] 编译 Colonist Bar ==========
dotnet build Source\AstrylsUIZhCN.ColonistBar\AstrylsUIZhCN.ColonistBar.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ Colonist Bar 编译失败
    pause
    exit /b 1
)
echo ✓ Colonist Bar 编译成功

echo ========== [4/6] 编译 Circinus ==========
dotnet build Source\AstrylsUIZhCN.Circinus\AstrylsUIZhCN.Circinus.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ Circinus 编译失败
    pause
    exit /b 1
)
echo ✓ Circinus 编译成功

echo ========== [5/6] 编译 Faction Menu ==========
dotnet build Source\AstrylsUIZhCN.FactionMenu\AstrylsUIZhCN.FactionMenu.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ Faction Menu 编译失败
    pause
    exit /b 1
)
echo ✓ Faction Menu 编译成功

echo ========== [6/6] 编译 Modern CC ==========
dotnet build Source\AstrylsUIZhCN.ModernCC\AstrylsUIZhCN.ModernCC.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ Modern CC 编译失败
    pause
    exit /b 1
)
echo ✓ Modern CC 编译成功

echo ========== [7/7] 校验项目根 Assemblies（编译产物回流） ==========
if not exist Assemblies mkdir Assemblies
copy /Y "Source\AstrylsUIZhCN\bin\Release\net472\AstrylsUIZhCN.dll" "Assemblies\AstrylsUIZhCN.dll" >nul
copy /Y "Source\AstrylsUIZhCN.LearningMenu\bin\Release\net472\AstrylsUIZhCN.LearningMenu.dll" "Assemblies\AstrylsUIZhCN.LearningMenu.dll" >nul
copy /Y "Source\AstrylsUIZhCN.ColonistBar\bin\Release\net472\AstrylsUIZhCN.ColonistBar.dll" "Assemblies\AstrylsUIZhCN.ColonistBar.dll" >nul
copy /Y "Source\AstrylsUIZhCN.Circinus\bin\Release\net472\AstrylsUIZhCN.Circinus.dll" "Assemblies\AstrylsUIZhCN.Circinus.dll" >nul
copy /Y "Source\AstrylsUIZhCN.FactionMenu\bin\Release\net472\AstrylsUIZhCN.FactionMenu.dll" "Assemblies\AstrylsUIZhCN.FactionMenu.dll" >nul
copy /Y "Source\AstrylsUIZhCN.ModernCC\bin\Release\net472\AstrylsUIZhCN.ModernCC.dll" "Assemblies\AstrylsUIZhCN.ModernCC.dll" >nul
echo ✓ 项目根 Assemblies 已更新（6 个 DLL）

echo ========== [8/8] 部署到游戏 Mods ==========
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
