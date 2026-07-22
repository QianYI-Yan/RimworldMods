@echo off
chcp 65001 >nul
echo ========================================
echo   TailorMadeZhCN 汉化补丁 - 编译脚本
echo ========================================
echo.

dotnet build Source\TailorMadeZhCN\TailorMadeZhCN.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ 编译成功！
    echo   输出: Assemblies\TailorMadeZhCN.dll
    echo.
    echo 将 TailorMade-ZhCN 文件夹复制到 RimWorld/Mods/ 目录即可使用。
) else (
    echo.
    echo ✗ 编译失败，请检查错误信息。
)

pause
