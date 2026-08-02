# build.bat 构建部署脚本规范

工作区各模组的构建部署脚本统一约定。核心原则：**源码部署用黑名单机制，不用白名单**。

## 核心规范

| 项 | 要求 |
|----|------|
| 编码 | 开头 `@echo off` + `chcp 65001 >nul`（中文输出不乱码） |
| 部署目标 | 用 `set TARGET=...Mods\模组目录` 变量，避免重复长路径 |
| 编译 | 有源码的模组：`dotnet build xxx.csproj -c Release` + `if %ERRORLEVEL% NEQ 0 ( ... exit /b 1 )` |
| **源码部署** | **黑名单机制**：`xcopy /E /Y /Q "Source\项目目录\*" "%TARGET%\Source\"` 整体复制，随后仅清理 `bin`、`obj` |
| 排除项 | 只需维护黑名单：`bin`、`obj`（其它一律自动包含） |
| 多产物 | 桥梁进程等独立 exe 先构建再复制到对应目录（如 `Bridge\`） |
| 其他部署 | `About`、`Assemblies`、`Languages`、`Bridge` 等按需复制 |
| 结尾 | `pause`（交互运行停住；自动化调用用 `cmd /c "build.bat < NUL"` 跳过） |

## 为什么用黑名单（不用白名单）

- 白名单（逐条 `xcopy *.cs / *.csproj / Properties / Theme / UI ...`）：**新增文件或子目录时容易漏**，部署后游戏目录缺文件
- 黑名单：整体复制整个源码目录，只删 `bin`/`obj`，**新增文件/子目录自动跟上**，零维护

## 参考实现

- `NotificationsONWindowsNOW/build.bat`：双项目（桥梁 + 模组）+ 黑名单源码部署
- `ModernExpandMenu/build.bat`：单项目 + 清理 bin/obj（黑名单思路的早期版本，可对照）
