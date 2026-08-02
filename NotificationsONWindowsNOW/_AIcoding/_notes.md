# NotificationsONWindowsNOW — 项目笔记

## 项目定位
- 将 RimWorld 游戏内通知（信件 Letter + 消息 Message）推送到 Windows 通知中心。
- mod ID：`yintx.deepseek.NotificationsONWindowsNOW`
- 类型：独立功能模组（非汉化/修复），目录在工作区根目录。
- 状态：**端到端实测通过**（2026-08-02）；已加入设置页与中英双语本地化。

## 关键设计决策
- **桥梁进程架构**：RimWorld 跑在 Unity Mono 上，无法直接调 WinRT（`Windows.UI.Notifications`），
  因此用 `Bridge/ToastBridge.exe`（.NET Framework CLR）通过命名管道接收内容并发送 Toast。
- **命名管道**：`\\.\pipe\RimWorldToastBridge`，协议为 UTF-8 文本行。
  - 普通通知行：`标题\u0001正文\u0001类型`（类型：`L`=信件事件锚点，`M`=消息）
  - 控制行：`\u0002SET\u0001merge=<毫秒>`（模组设置变化时下发，更新桥梁的合并窗口）
- **ToastBridge 用反射调 WinRT**：不引用 Windows.winmd，避免构建依赖 Windows SDK 路径，跨机器可编译。
- **补丁点**：
  - `LetterStack.ReceiveLetter(Letter, string, int, bool)` — 所有信件重载的最终入口。
  - `Messages.Message(Message, bool)` — 所有消息重载的最终入口；用 `Messages.IsLive` 过滤重复消息。
- **设置系统**（`NotificationsOnWindowsNowSettings`，设置页中英双语）：
  - `enableShortTimeMessageDedup` — 短时间消息去重开关，**默认关闭**（模组侧 3 秒/180 tick 去重）。
  - `mergeWindowSeconds` — 合并窗口秒数（0=不合并），默认 2 秒；经控制行下发到桥梁。
- **通知合并（锚点模式）**：桥梁侧以**信件（L）为事件锚点**——信件到达后，窗口内的伴随消息（M）合并进它；
  **无锚点的独立消息（M）立即单独推送，不合并**。这样：一次袭击（信件+伴随消息）合成一条，
  而存档「已保存为」这类独立系统消息单独弹出，互不干扰。用户明确不要过滤这类消息，只要求不合并。
  合并正文换行拼接（最多 8 行）；新信件到达时若缓冲未发，先 flush 旧锚点再开新窗口。
- **富文本剥离**：游戏内文本常含 `<color>`、`<b>` 等富文本标签（实测友军信件带 `<color=#00BFFFFF>`），
  转发时用官方 `ColoredText.StripTags()` 剥成纯文本再推送。

## AUMID 注册（重要踩坑记录）
无打包桌面应用发 Toast 会被 Win10 静默丢弃，必须注册 AUMID：
1. **必须用 IShellLink COM 接口创建快捷方式**，不能用 WScript.Shell——
   微软文档明确：WScript.Shell 创建的 .lnk 其 AppUserModelID 属性存储不可写。
2. **SHGetPropertyStoreFromParsingName 的 flags 必须为 GPS_READWRITE (2)**！
   - flags=0（GPS_DEFAULT）→ 只读存储，SetValue 抛 STG_E_ACCESSDENIED
   - flags=1（GPS_HANDLERPROPERTIESONLY）→ 也不对
   - flags=2（GPS_READWRITE）→ 正确，SetValue/Commit 成功
3. AUMID 用 `RimWorld.NotificationsONWindowsNOW`，快捷方式在用户级开始菜单
   `%APPDATA%\Microsoft\Windows\Start Menu\Programs\RimWorld Notifications Bridge.lnk`。
4. 注册成功验证：Shell.Application 的 `ExtendedProperty("System.AppUserModel.ID")`。

## 已验证结论
- RimWorld 游戏目录有 `MonoBleedingEdge`，且 `Managed\` 下**无 `System.Runtime.WindowsRuntime.dll`**
  → 游戏进程内（Mono）无法调用 WinRT，桥梁进程是唯一可靠方案。
- ToastBridge 进程权限与游戏相同（当前用户），无管理员权限、无注册表、无自启动。
- **RimWorld 1.6 不需要 About.xml 的 `modClass` 字段**：`LoadedModManager.CreateModClasses()`
  通过反射自动扫描所有加载程序集里的 `Mod` 派生类并实例化（按 type.Assembly 归属 modpack）。
  About.xml 里写 `modClass` 会报 XML error（`doesn't correspond to any field in ModMetaDataInternal`），
  但**不影响功能**（Mod 类仍会被自动发现）；已从 About.xml 删除该字段。

## 对外说明
- About.xml 描述与 README 中**明确警告**：模组包含并运行额外的 Windows 可执行程序
  `Bridge/ToastBridge.exe`（用于调 WinRT Toast）。该程序以当前用户权限运行、不访问网络、
  不写注册表、不设自启，仅在本机范围接收通知。这是模组的透明性/安全告知。

## 已知问题 / 注意
- 管道协议无认证：本机任意进程可连管道伪造 toast（仅能弹通知，影响低）。
  可选加固：随机管道名 + 进程路径/哈希校验。
- 每次发送若管道不可用会尝试拉起桥梁进程并重试一次（首条可能因桥未就绪偶发丢失）。
- 桥梁进程空闲 5 分钟无连接自动退出（省资源）；游戏退出后进程仍可能存活（可接受）。

## 待办
- [x] 端到端测试：AUMID 注册 + 管道 + Toast 弹出（2026-08-02 通过）
- [x] 游戏内实测：游戏内真实事件已推送（友军降临、存档保存、剪贴板等，2026-08-02 通过）
- [x] 富文本处理：`StripTags()` 剥离 `<color>` 等标签（2026-08-02 修复）
- [x] 通知合并：合并窗口合并一次事件的碎片通知（2026-08-02）
- [x] About.xml 清理：移除 1.6 不支持的 `modClass` 字段（2026-08-02）
- [x] 设置页：去重开关（默认关）/ 合并窗口（2026-08-02）；SilentInput 过滤已撤销（用户不要过滤，只要不合并）
- [x] 锚点合并：信件为锚点合并伴随消息，独立消息单独推送（2026-08-02）
- [x] 英文本地化：`Languages/English/Keyed/NotificationsOnWindowsNow.xml`（2026-08-02）
- [x] 描述警告：About.xml 与 README 注明额外 exe（2026-08-02）
- [ ] 游戏内验证设置页与合并/去重行为
- [ ] 可选项：注册 AUMID 后点击通知回跳游戏并选中目标（需 app activation）
- [ ] 可选项：Toast 附带信件类型图标（LetterDef 图标 → toast 图像）
- [ ] 可选项：管道安全加固（随机管道名 + 进程校验）

## 构建
- `build.bat`：先构建 `Bridge/ToastBridge`，再构建模组 DLL，部署（含 Languages/About/Bridge/Source）到游戏 `Mods/NotificationsONWindowsNOW/`。
- 框架 net472；NuGet：`Krafs.Rimworld.Ref 1.6.4871`、`Lib.Harmony 2.3.6`。
- 诊断脚本：`_AIcoding/_toast_diag*.ps1`、`_AIcoding/AumidDiag/`（可复用排障）。
