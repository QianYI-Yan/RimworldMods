# ═══════════════════════════════════════════════════════════
# ModernExpandMenu 构建部署脚本（PowerShell 版，解决终端中文乱码）
#
# 用法: 在项目目录下执行  ./build.ps1
#
# 乱码根源: build.bat 以 UTF-8 编码保存并 `chcp 65001` 输出中文，
#           而 PowerShell 默认用系统代码页（GBK/gb2312）解码外部程序输出
#           → 中文显示成「閮ㄧ讲鍒」乱码。
# 修复: 执行前把控制台输出解码编码设为 UTF-8，再调用 build.bat。
# ═══════════════════════════════════════════════════════════
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 切到脚本所在目录（无论从哪调用）
Set-Location -Path $PSScriptRoot

# 调用 build.bat（< NUL 跳过其 pause 等待，避免挂起）
cmd /c "build.bat < NUL"

# 透传构建退出码（编译失败时非 0，方便 CI/脚本判断）
exit $LASTEXITCODE
