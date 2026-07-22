@echo off
chcp 65001 >nul
echo ========================================
echo   TailorMadeZhCN 汉化补丁 - 编译脚本
echo ========================================
echo.

echo [1/2] Building Chinese translation...
dotnet build Source\TailorMadeZhCN\TailorMadeZhCN.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ✗ 汉化编译失败
    pause
    exit /b 1
)
echo ✓ 汉化编译成功

echo.
echo [2/2] Building TailorMadeUnlockFix...
dotnet build TailorMadeUnlockFix\Source\TailorMadeUnlockFix\TailorMadeUnlockFix.csproj -c Release
if %ERRORLEVEL% EQU 0 (
    echo ✓ 修复模组编译成功
    echo.
    echo 输出: Assemblies\TailorMadeZhCN.dll
    echo      TailorMadeUnlockFix\Assemblies\TailorMadeUnlockFix.dll
) else (
    echo ✗ 修复模组编译失败
    pause
    exit /b 1
)

pause
