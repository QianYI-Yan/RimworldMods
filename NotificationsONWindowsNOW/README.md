# Notifications ON Windows NOW

把 RimWorld 游戏内的**所有通知**实时推送到 **Windows 通知中心**的模组。

> 挂机、切窗、游戏被遮挡时，也能第一时间看到袭击、贸易、任务等重要事件。

## 功能

- **信件（Letter）**：袭击预警、贸易请求、任务进展等所有进入信件栏的重要事件，推送标题 + 正文。
- **消息（Message）**：研究完成、建造完成、出现灵感等所有屏幕角落的小提示，同样推送。
- **通知合并**：同一事件（信件 + 伴随消息）的碎片通知自动合并为一条；独立的系统消息（如存档提示）单独推送，互不干扰。
- **可配置**：模组设置页可自定义合并窗口、短时间去重开关。
- **零配置**：装上即用；中英双语界面。
- **静默降级**：仅在 Windows 10/11 上生效，其它环境自动跳过，不影响游戏。

> ⚠️ **注意**：本模组包含并会运行一个额外的 Windows 可执行程序（`Bridge/ToastBridge.exe`），
> 用于调用 Windows 原生通知（Toast）接口。该程序以当前用户权限运行，不访问网络、不写注册表、
> 不设开机自启，仅在本机范围内接收游戏通知并发送到通知中心。若您对可执行程序有顾虑，请勿安装本模组。

## 工作原理

RimWorld 运行在 Unity 的 **Mono** 运行时上，无法直接调用 Windows 的 WinRT（`Windows.UI.Notifications`）接口。因此本模组采用「桥梁进程」架构：

```
RimWorld (Mono)
   └─ 模组 DLL  (Harmony 补丁捕获 Letter / Message)
        └─ 命名管道 (\\.\pipe\RimWorldToastBridge)
             └─ ToastBridge.exe (.NET Framework CLR)
                  └─ 反射调用 WinRT Toast API
                       └─ Windows 通知中心
```

- 模组侧：Harmony 补丁 `LetterStack.ReceiveLetter` 与 `Messages.Message`，捕获全部通知。
- 桥梁侧：`Bridge/ToastBridge.exe` 常驻后台，收到内容后调用 Windows 原生 Toast 通知。

## 目录结构

```
NotificationsONWindowsNOW/
├── About/About.xml           模组元数据
├── Assemblies/               构建产物（模组 DLL）
├── Bridge/
│   ├── ToastBridge/          桥梁进程源码（.NET Framework 控制台）
│   └── ToastBridge.exe       构建产物（部署到模组 Bridge/ 目录）
├── Source/NotificationsONWindowsNOW/   模组 DLL 源码（net472 + Harmony）
├── build.bat                 一键构建 + 部署脚本
└── _AIcoding/_notes.md       项目开发笔记
```

## 构建

需要 .NET SDK（8.0+，构建 net472）。运行：

```bat
build.bat
```

脚本会依次构建桥梁进程与模组 DLL，并部署到游戏 `Mods/NotificationsONWindowsNOW/`。

## 兼容性

- 游戏版本：RimWorld 1.6
- 系统要求：Windows 10 / 11（Toast 通知依赖 WinRT）
- 依赖：Harmony（随模组内嵌引用，无需单独安装）
