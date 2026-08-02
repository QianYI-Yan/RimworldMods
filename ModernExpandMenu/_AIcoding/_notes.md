# ModernExpandMenu 开发笔记

## 模组信息
- mod ID: `yintx.deepseek.modernexpandmenu`
- 位置: `D:\Github\RimworldMods\ModernExpandMenu`
- 目标: 单格储物容器右键 → MD3 风格分组悬浮窗

## 核心设计决策

### Hook 点选择
- 拦截 `FloatMenuMakerMap.GetOptions`（Postfix），而非 `AddHumanlikeOrders`
- 原因: 1.6 已重构为 Provider 架构，`GetOptions` 是统一出口，且 `Selector.HandleMapClicks` 只有列表非空才创建原版 FloatMenuMap —— **清空 `__result` 即可彻底抑制原版菜单**
- 多选（IsMultiselect）场景不接管，保持原版

### 操作项复用原版 Provider
- `FloatMenuOptionProvider_Wear`（衣物穿戴/强制穿戴）和 `FloatMenuOptionProvider_PickUpItem`（拾取）直接实例化调用 `GetOptionsFor(thing, context)`
- 复用原版选项的 label + action + disabled，避免重复实现穿戴判定、负重判定等复杂逻辑
- 已验证 `FloatMenuUtility.DecoratePrioritizedTask` 不依赖 `FloatMenuMakerMap.currentProvider`，直接调用安全
- 搬运操作自建: `HaulAIUtility.HaulToStorageJob`

### 归组映射
- 关键: `FloatMenuOption.iconThing` 字段关联物品（GetProviderOptions 自动设置）
- 本模组直接对容器 `slotGroup.HeldThings` 按 `thing.def` 分组，无需 label 匹配

### MD3 主题
- 色板为 MD3 Baseline Light，Token 集中在 `Theme/MD3Theme.cs`
- 圆角用预生成纹理（64x64，四角圆角距离场）+ 九宫格拉伸绘制
- **未来可扩展**: 接入 CSS 子集解析器，把解析结果填入 Token 模型即可实现 CSS 自定义外观，UI 层零改动

## 已知限制
- 仅 `Building_Storage` 且 `def.Size.Area == 1`（单格）
- 容器场景接管后会丢弃原版 WorkGivers 等对容器的其它选项（第一版取舍）
- 操作项 label 有硬编码中文（"搬运到储物区"），后续应改 Keyed 翻译

## ⚠️ 定位 Bug（已修复，重要经验）
- 症状: MD3 窗口出现在鼠标的垂直镜像位置，不在鼠标附近
- 根因: `UI.MousePositionOnUI = Input.mousePosition / UIScale` 是 **左下原点**（y 向上）；`windowRect` 是 GUI 空间 **左上原点**（y 向下）
- 修复: 必须用 `UI.MousePositionOnUIInverted`（原版 FloatMenu 同款），不能用 `MousePositionOnUI`

## ClickGUI 风格展开/折叠（2026-08 增加）
- 每个物品分组标题可点击，切换展开/折叠该组操作（仿 Minecraft hack client ClickGUI）
- 标题右侧显示 ▾（展开）/ ▸（折叠）箭头；hover 高亮
- 折叠状态用 `HashSet<ThingDef>`（"其他"组 key 为 null）
- 折叠切换后调 `RefreshWindowHeight()` 动态调整 windowRect 高度 + 滚动位置，防止超出屏幕
- 用户曾提议下载"水影 nextgen" ClickGUI 源码参考 —— 未采用（避免他人项目许可问题，交互自行实现）

## 接管范围变更
- 原: 仅 `Building_Storage` 单格容器（用户实测发现右键的是地上物品堆，不接管）
- 现: 右键命中 ≥2 种物品（ThingCategory.Item）即接管；选项用 `option.iconThing` 分组，原版功能零丢失

## 加载动画体系（2026-08 增加）
- 分帧生成操作（`pendingItems` + `ProcessPendingActions`，每帧 `MaxProcessedPerFrame` 个物品），避免大量物品右键卡死
- 加载期间: 顶部缓冲加载条（脉冲呼吸 + 前端光标亮点）+ 半透明覆盖层 + 中央环形进度 + 百分比 + 操作项左侧渐入逐条载入（`appearTime` 排定，30ms 间隔 / 250ms 渐入）
- 加载完成: `FinalizeGroups` + 自动滚动到顶端 + 解锁点击
- 环形进度为**平滑圆环纹理**（64x64 逐像素生成、双线性过滤、顶部顺时针），非离散点

## 悬停高亮 + 发光箭头（2026-08 增加）
- 窗口持续高亮右键命中的**物品**（`GenDraw.DrawTargetHighlight`）+ 容器格（`DrawTargetHighlightWithLayer`），修复"右键物品堆无高亮"
- 悬停操作项: hover 蓝层 + 目标物品白框高亮 + 从鼠标指向物品的黄色发光箭头（`DrawHoverArrow`: 粗淡黄光晕 + 细亮黄线 + V 形箭头尖，`GenDraw.DrawLineBetween` + `UI.MouseMapPosition()`）

## 设置系统（2026-08 增加）
- `ModernExpandMenuSettings : ModSettings`，游戏内"选项 → Mod 设置"
- 可调: 显示加载动画、悬停高亮箭头、分组标题物品总数、悬浮窗最大高度（300~900）、每帧处理物品数（2~30）
- 窗口读取 `ModernExpandMenuMod.Settings`

## 日志清理（2026-08）
- `FloatMenuOptionProvider_CarryingPawn`（InvalidCast）与 `FloatMenuOptionProvider_LoadCaravan`（NullRef）对容器内物品调用抛异常 → 加入 `ExcludedProviderTypes` 排除（面向 Pawn/车队，对物品无意义）
- 其余 Provider 保留 try/catch 兜底，单个失败只警告不崩溃

## 类控制台从底端插入动效（2026-08 增加）
- 加载期间滚动**自动跟随底部**（`WindowUpdate` 中 MoveTowards maxScroll，速度 40），新条目逐条从底端插入，如终端控制台输出
- 加载视觉结束（含额外时间）后**平滑滚回顶端**（`returnToTopPending` 标志 + MoveTowards 0，速度 20）
- 操作项复合动效: `drawRect.x -= (1-appearProgress)*20f`（左侧滑入）+ `drawRect.y += (1-appearProgress)*height`（底端插入）→ 从左下角滑入到位
- `RedistributeAppearTimes`: 用**额外时间（extraSeconds）作为所有项目滑入总时长**均匀排布，最后一条在接近额外时间末尾开始
- 展开子项目: `ToggleGroupCollapsed` 展开时重新排定 `appearTime = insertStart + i*AppearInterval`，逐条从底端插入（**不跟踪滚动**，与加载时的控制台跟随区分）
- `ProcessPendingActions` 完成时**不再重置 scrollPosition=0**（回顶交给 WindowUpdate 平滑处理）

## 日志验证（2026-08-02）
- 修复 TargetMethod 后**模组加载完全正常**: 无 "Error while instantiating"、无 "Undefined target method"
- 正常运行日志: `[ModernExpandMenu] 接管：15 组 / 15 类`
- 已解决: **Missing preview file**（`About/Preview.png`）→ 用 PowerShell + System.Drawing 程序化生成 1024x1024 MD3 水影蓝风格预览图（深色底、圆角面板、环形进度+进度条、分组行、滚动条，纯图形无文字），`build.bat` 已有 `if exist About\Preview.png` 部署逻辑，已手动复制到游戏目录
- 其余日志噪声与模组无关（不改）: 其他汉化包 "Translation data 15 errors"、MVCF "ManagedVerb" 警告、其他模组缺 downloadUrl 依赖警告、Unity 内存统计 "Failed Allocations"

## 动画体系重构：串行出现动画 + 动画设置 tab（2026-08-02）
- **严格串行**：上一项就位（动画播完）后下一项才开始（`ScheduleAppear()`，`nextEntryAppearEndTime` 全局推进，步长 = 滑入时长 + 间隔），取代原"排时间表重叠滑入"
- **可见性触发**：分组标题与操作项仅在"展开裁剪内 + 滚动视口内"且 `appearTime < 0` 时排定动画（`IsVisibleInViewport`）。加载时滚动跟随底部逐条出现；**加载后滚动/展开使新项目进入可视范围时同样播放滑入动画**（需求 11）
- 大类（分组标题）动画与子项目一致（左滑 + 底端插入），绘制顺序 header→actions 天然保证先标题后子项串行
- `ProcessPendingActions` 不再预排 appearTime（删除 `RedistributeAppearTimes`）
- `ToggleGroupCollapsed` 展开时重置该组子项 appearTime=-1，展开裁剪逐步露出时逐条串行插入
- **动画 tab**（设置界面第 2 个 tab，settingsTab=1）：单条滑入时长 / 相邻间隔 / 弹出时长 / 展开速度 / 滚动跟随速度 / 回顶速度 6 个滑块 + **实时预览区域**（循环播放"标题+3 子项从底端插入"模拟菜单，实时反映设置值）
- **加载条常驻**：加载完成后不清除（显示满条）；悬停加载条（仅加载跑完后）显示 tooltip：物品数 / 操作数 / 耗时 / 动画总时长
- **内容区始终让出加载条空间**（`LoadingBarHeight=5` + `LoadingBarGap=4`，DrawGroups 与 ComputeContentHeight 一致），滚动时不再遮挡加载条
- **上下边框描边**：`MD3Widgets.DrawRoundedRectOutline`（外框 Outline + 内缩 Surface）
- **滚动淡出遮罩**：`MD3Widgets.DrawVerticalFade`（1x16 渐变纹理），内容超出一屏且未到边缘时顶部/底部 14px 淡出，避免硬截断
- **加载覆盖层拒绝交互**：滚轮在滚动视口前被 `Event.current.Use()`，点击用覆盖层上 `Widgets.ButtonInvisible` 拦截
- 新设置项（6 个，含 ExposeData）：`itemAppearDuration`(0.25) / `itemAppearInterval`(0.03) / `popAnimationDuration`(0.18) / `expandAnimationSpeed`(10) / `scrollFollowSpeed`(40) / `scrollReturnSpeed`(20)
- **恢复默认对话框改树状结构**：常规 / 动画 / 颜色 三个分组（`ResetSection` 标题行 + 缩进子项），`ResetItem.selected` 内聚，开关 id 1000+ 段
- 修复：对话框确定按钮原引用不存在的 `ResetApply` 键 → 改回已有的 `ResetConfirm`（此前翻译缺失一直显示 key 原文）
- 新增 19 个翻译键 × 9 语言（动画 tab / 速度设置 / 预览 / 加载统计 tooltip / 重置分组与动画项）
- **已知权衡**：严格串行下全量项动画总时长 = 项数 ×（滑入时长+间隔），菜单项多时整体出现较慢，可在动画 tab 调小滑入时长

## 动画体系重构 v2：按组独立 + 滚动进出动画 + 总开关（2026-08-02）
- **每组子项动画独立**：`groupNextAppearEndTime`（Dictionary 按组），组内串行、组间并行互不等待；`ScheduleAppear(group)` 按组推进
- **动画方向改两阶段**：前 55% 水平从左向右滑入（底部位置），后 45% 垂直向上归位——不再是"左下→右上"对角线
- **动画未到时全元素隐藏**：`ComputeBlockAnim` 返回统一 `BlockAnim`（alpha + offset），背景/图标/文本/竖条全部乘 alpha，图标用 `GUI.color` 包住 `ThingIcon`
- **滚动进出动画**：`BlockAnim` 状态机（appearTime / disappearTime / hasAppeared）——进入视口过半播放出现，滚出过半播放消失（淡出+下滑），消失完成重置 appearTime 下次滚回重新播放
- **加载条置顶**：绘制顺序移到覆盖层之后（最顶层），不再被淡出遮罩/覆盖层遮挡
- **删发光**：删除环形进度圈（`DrawProgressRing` 整个方法 + ringTexture/ringPixels 字段）与加载条缓冲段"流动高光"；覆盖层只保留黑色 + 中央百分比
- **加载完成底部流程**：阶段1 加载/额外显示跟随底部 → 阶段2 `bottomFlowActive` 滚动到所有组项底部 → 阶段3 到底后等待 200ms（`BottomWaitSeconds`）→ 阶段4 加载视觉结束回顶；修复了额外时间=0 时跳过底部流程的缺口（`bottomFlowActive` 标记）
- **窗口高度动态动画**：`windowHeightAnimationSpeed`(200) 设置，WindowUpdate 平滑过渡；`RefreshWindowHeight` 只限滚动位置不再跳变高度
- **动画总开关** `enableAnimations`（默认 true）：动画 tab 顶部"总开关"卡片；关闭后停用弹出/出现消失/展开/滚动跟随回顶/高度/加载视觉（blockInteraction 与加载条条件也叠加开关）
- 新增设置：`windowHeightAnimationSpeed`、`enableAnimations`；新增翻译键 5 个 × 9 语言（动画总开关区 + 启用动画 + 2 个重置项）

## 源码整理与 Git 推送（2026-08-02）
- **发现并修复 csproj 显式 Compile 列表问题**：`EnableDefaultItems=false` 下 csproj 曾引用根目录旧版 `Dialog_ResetDefaults.cs`（ResetEntry 平铺版），导致**树状对话框从未编译生效**；且 `Patch_Md3CloseButton.cs` 是废弃旧文件（功能已被 `Patch_ModSettingsUI.cs` 覆盖）但一直未删
- 已删除：根目录旧版 `Dialog_ResetDefaults.cs`、`Patch_Md3CloseButton.cs`（含游戏部署目录 Source 残留）；csproj 改为 `<Compile Include="UI\Dialog_ResetDefaults.cs" />`，重新构建验证通过
- **Git 推送**：
  - `main`：`33c5eb7`（完整模组含 DLL/翻译/About/Preview/README/笔记），仅含 ModernExpandMenu 相关
  - `source-only`：`1496366`（纯源码 + build.bat + 翻译 + About，不含 DLL/README/笔记/Preview），同步最新动画重构源码；**source-only 分支定位为"所有模组的源码"（TailorMade 等保留不动），本次只同步菜单模组**
- 本次未推送（其他项目，留待各自处理）：`asrtylsUIMod-ZhCN/`、`NotificationsONWindowsNOW/`、`_templates/`

## 动画重构 v3：逐组展开 + 纯水平滑入 + 细节修正（2026-08-02）
- **逐组展开**：加载时组从上到下依次出现——第一组就位（标题+子项动画完成）→ 窗口高度扩大 → 再显示下一组（`revealedGroupCount` + `nextGroupRevealTime`，组间串行）；加载时不再"滚动跟随底部"，`scrollPosition` 保持顶部；`ComputeContentHeight` / `DrawGroups` 只统计/绘制已展开的组；加载时组**默认展开**（标题+子项一起出现，用户仍可点击折叠）
- **动画改纯水平从左向右滑入**：`ComputeBlockAnim` 出现动画只做 `offsetX`（-20px → 0），不做垂直偏移——不再是"左下→右上"对角线；消失动画仍为淡出+下滑
- **修复图标提前出现**：`ComputeBlockAnim` 中 `!hasAppeared && appearTime < 0`（尚未排定动画）时 alpha=0 完全隐藏（此前错误显示为 alpha=1，导致图标/组项提前出现）
- **删物品高亮圆圈**：移除 `highlightedItems` 的 `GenDraw.DrawTargetHighlight` 循环（用户要求）；容器高亮 `highlightStorage`（`DrawTargetHighlightWithLayer`）保留
- **恢复加载环形**：按用户要求恢复 `DrawProgressRing`（环形本身保留，无发光/呼吸/亮点），覆盖层恢复"环形 + 百分比"；覆盖层透明度 0.4 → 0.25（让逐组展开动画透出可见）
- **滚动速度默认调快**：`scrollFollowSpeed` 40 → 80、`scrollReturnSpeed` 20 → 40（含重置对话框默认值；**注意：用户已有 ModSettings XML 会沿用旧值，需在动画 tab 手动调或删配置**）

## 动画重构 v4：首次加载只播组标题 + 控制台滚动 + 回顶时间设定（2026-08-02）
- **首次加载只有组标题动画**：加载时只逐条排定组标题出现（`group.appearTime = reveal 时刻`），子项目不参与首次加载；组**默认折叠**（`expandedTargets` 空），子项目折叠时不占布局空间（`progress=0`），不会"提前申请区域把下面的组往下推"
- **子项目到出现时才推下面的组**：点击展开时 `expandProgress` 0→1，`ComputeContentHeight` 按 progress 计算，窗口高度动态增长（往下推）+ 子项逐条左滑入
- **加载条跑完时首次动画完成**：reveal 节奏**均匀分布到加载视觉时长内**（`lastStart = visualEnd - itemAppearDuration`），加载条跑完时所有组标题已排定
- **首次加载像控制台滚动到底部**：阶段1 恢复滚动跟随底部（`MoveTowards maxScroll`，速度 80），新组标题在底部出现并跟随可见
- **回顶改为时间设定**：`scrollReturnSpeed` → `scrollReturnDuration`（默认 0.6 秒），ease-out-cubic 固定时长内滚回顶端（`scrollReturnStartTime`/`scrollReturnStartPos`）；翻译键同步改 `ScrollReturnSpeed` → `ScrollReturnDuration`（9 语言）
- 图标跟随子项目渐变（alpha 乘 appearProgress），未排定动画时 alpha=0 不提前出现

## 配置分享功能（2026-08-02）
- 新增 `SettingsShare.cs`：把 Mod 设置导出/导入为独立 XML（反射遍历 `ModernExpandMenuSettings` 的 DeclaredOnly public 实例字段；float 用 InvariantCulture）
- **RimWorld 配置接口说明**：标准 ModSettings 由游戏自动保存（`ExposeData()` + `Write()` → `存档数据目录/Config/ModSettings/<PackageId>.xml`）；无专门的"额外文件"API，但可用 `GenFilePaths.SaveDataFolderPath` + `System.IO.File` 自由读写自定义文件（本功能即用此）
- 功能：常规 tab 底部新增「分享配置」卡片，4 个按钮：
  - 导出到文件 → `存档数据目录/ModernExpandMenuShare/ModernExpandMenu_<时间戳>.xml`
  - 复制配置 → 剪贴板（XML 文本，可直接发给他人）
  - 从剪贴板导入 / 从文件导入（读分享文件夹最新文件）
- 反馈：有地图用 `Messages.Message`，无地图退化 `Log.Message`；新增 9 个翻译键 × 9 语言
- 导入需 XML 根元素 `<ModernExpandMenuSettings>`，字段按名称匹配（缺字段/多余字段容错）

## 两个 Bug 修复（2026-08-02）
1. **加载条加载完成后额外跑很久**：配置 `extraLoadingBarSeconds=0` 时，加载完成瞬间 `lastStart = visualEnd - 动画时长` 变为负数，reveal 间隔被 clamp 到 0.05s/组，剩余组被拖很久（覆盖层长时间显示）。修复：加载完成（加载条跑完）时**剩余组立即全部排定**，覆盖层尽快进入底部流程（滚到底 + 200ms）
2. **输入框点击后看不到内容**：`MD3NumberField` 在 `TextFieldNumeric` 画完文字后**又填充了一次边框**（`DrawRoundedRect` 全填充）覆盖了文字。修复：改为"背景 + `DrawRoundedRectOutline` 描边环"（不填充内部），先画边框再输入文字
- 配置文件实际路径：`存档数据目录/Config/Mod_ModernExpandMenu_ModernExpandMenuMod.xml`（`Mod_<Name>_<SettingsClass>.xml` 命名）；用户实际 `maxProcessedPerFrame=55`、`extraLoadingBarSeconds=0`、颜色为粉彩玫瑰调色板

## 配置管理器（类 Windows 资源管理器，2026-08-02）
- 新增 `UI/Dialog_ConfigManager.cs`：列出分享文件夹（`存档数据目录/ModernExpandMenuShare/`）中的配置文件，每行显示名称 / 大小 / 修改时间，点击选中（主色高亮层）
- 操作：导入所选（应用配置）/ 删除所选 / 刷新 / 关闭；空状态提示先导出
- 入口：分享卡片改为 5 按钮一行，新增「管理配置」按钮打开对话框
- 新增 7 个翻译键 × 9 语言

## 配置管理器增强（2026-08-02）
- **重命名所选**：新增 `UI/Dialog_RenameConfig.cs`（MD3 输入对话框：深色背景 + 主色描边环 + 原版 TextField 输入，自动聚焦，回车确认）；校验非法字符/重名/名称未变，成功回调刷新上级列表
- 配置管理器底部按钮改为 5 个一行：刷新 / 重命名所选 / 删除所选 / 导入所选 / 关闭
- 新增 5 个翻译键 × 9 语言
- **修复重命名"删文件"误报**：`RefreshFiles` 原用 `ModernExpandMenu_*.xml` 模式扫描，改名成非前缀名后列表匹配不到 → 文件从列表消失（看似被删）。修复：扫描分享文件夹**所有 `*.xml`**（`Dialog_ConfigManager.RefreshFiles` 与 `SettingsShare.LoadLatestFileContent` 同步改）

## MD3 UI 风格规范化（工作区，2026-08-02）
- 工作区 `rimworld-modding.instructions.md` 新增「UI 风格规范：MD3 首选」章节（色板 Token / 自绘控件 / 滚动条 / 输入框 / 对话框约定），并挂接详细规范文档
- 新建 `_templates/md3-ui-style/STYLE_GUIDE.md`：完整 MD3 规范（色板 Token 表 / 尺寸常量表 / 控件 API 清单 / 绘制约定 / 设置界面规范 / 参考实现路径）；已加入工作区「参考模板」索引
- **设置界面 MD3 化**：`MD3Widgets` 新增公共 `MD3BeginScrollView` / `MD3EndScrollView` / `MD3Scrollbar`（MD3 细滚动条，scrollbarId 区分多个，拖动支持）；`Dialog_ResetDefaults` 与 `Dialog_ConfigManager` 的原版 `showScrollbars:true` 滚动条改为 MD3 细条

## 最后一批 UI / 动画细节（2026-08-02）
- **颜色值加 # 号 + 自实现输入框**：`ModernExpandMenuSettings` 13 个颜色默认值、`ApplyPalette` 6 套调色板、`ResetColorSettings`、`Dialog_ResetDefaults` 颜色默认值全部加 `#` 前缀；`MD3Theme.FromHex` 与 `TryParseHex` 兼容 `#` 前缀（用户旧配置无 # 号也能正常显示）；`DrawColorRow` 色值输入改用 `MD3Widgets.MD3TextField`（自实现：深色背景 + 主色/红色描边环 + 原版 TextField 输入，非法 hex 红色描边），色块点击复制 hex
- **设置界面滚动**：`DoSettingsWindowContents` 内容包 `MD3BeginScrollView`（`settingsScrollPosition` 字段，`ComputeSettingsContentHeight` 估算各 tab 高度，`MD3EndScrollView(..., 3000, CardCornerRadius)`），各 tab 从局部 y=0 绘制
- **扩高上限 = maxMenuHeight**：`WindowUpdate` 高度目标 `Mathf.Min(TotalViewHeight, CurrentMaxMenuHeight)`（删 `FirstRevealHeightLimit`），达到上限前只扩高菜单页面，超上限后内容改滚动
- **加载条跑完直接回顶**：`ShowLoadingVisual = isLoading || (extraEndTime>=0 && now<extraEndTime)`；删除底部流程（`bottomFlowActive` / `waitAtBottomUntil` 字段与阶段 2/3/4 逻辑，`ProcessPendingActions` 里 `bottomFlowActive = true` 一并删除）；加载完成时剩余组立即全部排定；加载条跑完 → ShowLoadingVisual 结束 → `returnToTopPending` 平滑回顶（ease-out-cubic 按 `scrollReturnDuration`）
- **组展开动画完全结束后才加载子项目**：`DrawGroupActions` 里 `progress < 0.999f` 时子项一律 Hidden（不排定不显示）；progress≥0.999 后 `ComputeBlockAnim` 才排定 appearTime，未到动画时间的下一个子项 `continue`（不占高不绘制）
- **子项目逐项占高**：`ComputeActionsHeight` 只累加 `appearTime>=0 && now>=appearTime` 的项；`ComputeContentHeight` 与组外框高度同步只统计已出现子项（不乘 progress）——下一个子项目未到动画时间前不预留高度，不把下面内容往下推
- **加载中完全不参与交互（连 tooltip 也没有）**：`DrawGroupHeader` 的 hover 层 + `SetHoveredTooltip` 加 `!ShowLoadingVisual` 锁（行 hover/点击、加载条 tooltip 此前已锁）
- **组描边框**：`DrawGroups` 每组先画 `MD3Widgets.DrawRoundedRectOutline`（描边环 + Surface 填充，**一个框把整组包在里面**），再画标题/子项覆盖其上；组动画期间外框只含标题高度，子项逐条出现后随之长高
- **内容视口避开加载条**：`DoWindowContents` 内容视口从加载条下方开始（`loadingBarTop = Padding + LoadingBarHeight + LoadingBarGap`）；`DrawGroups` y 起点 `MD3Theme.Padding`；`ComputeContentHeight` 顶部只算 Padding（不再含加载条空间）；渐变遮罩（`DrawVerticalFade`）删除，悬浮窗不再使用
- 本批无新增翻译键（颜色 # 号无需翻译）；构建部署验证通过


