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

## 回顶动态曲线（2026-08-02）
- **回顶动画改用户手绘折线曲线**：用户导出 polyline 数据（`ReturnToTopCurvePoints`，7 个点，x=时间进度 0~1、y=滚动进度 0~1），`SampleReturnToTopCurve(t)` 分段线性插值采样，替换原 `ease-out-cubic`
- 曲线形状：0→17% 缓慢起步（y 仅 6.7%）→ 17%~27% 中段最高速猛冲（y 到 79.4%）→ 27%~100% 逐级减速收尾到顶端 —— 即「逐渐加速 → 中段最高速 → 再减速到顶端」
- 曲线点数据（归一化）：(0,0)、(0.17272,0.06708)、(0.2736,0.7942)、(0.40494,0.93886)、(0.57054,0.97312)、(0.81989,0.99406)、(1,1)；构建部署验证通过

## 一批 UI/动画修正 + 全局 MD3 输入框开关（2026-08-02）
- **组边框随收起清除**：`DrawGroups` 组外框高度、`DrawGroupActions` 占高、`ComputeContentHeight` 全部改为 `× 展开进度`（此前不乘 progress，收起动画期间残留全高导致"扩太多"）；展开动画期间子项未排定为 0（不提前占高），收起动画期间按 progress 平滑缩回
- **平滑推位不闪**：上一项导致 `DrawGroupActions` 返回 `y + visibleHeight` 在收起动画期间返回全高，下一组"硬生生闪到位置"——乘 progress 后随高度动画平滑上下移动
- **关闭动画仍保留加载屏幕**：`DoWindowContents` 的 `blockInteraction` 与顶部加载条绘制条件去掉 `&& AnimationsEnabled`（动画总开关关闭时加载条 / 覆盖层 / 环形 / 交互锁定照常）
- **加载时滚轮屏蔽恢复**：blockInteraction 不再依赖动画开关，加载中滚轮事件在滚动视口前被 `Event.current.Use()` 拦截
- **加载没到最大高度不滚动**：`WindowUpdate` 加载流程改为「窗口高度 ≥ maxMenuHeight 才滚动跟随底部踢上面的，否则 scrollPosition=0 直接插入扩大的菜单范围」
- **滚动回归子项目加速**：`StoredItemGroup` 新增 `subItemsEverAppeared` / `subItemsAccelerated`；`ComputeBlockAnim` 加 `isSubItem` 参数，排定时检测"组已展开 + 子项曾排定过"（滚动回归）→ `ScheduleAppear(group, accelerated)`（时长与间隔 ×0.4），动画时长同步缩短；`ToggleGroupCollapsed` 展开时清零（首次展开正常速度）
- **全局 MD3 输入框开关（可选功能）**：新设置 `md3StyleAllInputs`（默认关闭）；新 `Patch_Md3StyleAllInputs.cs` patch `Text.CurTextFieldStyle` getter（Postfix），开启时用 `MD3Widgets.ToMd3TextFieldStyle(original)`（克隆原样式清背景边框，按原样式引用缓存避免 GC）——覆盖所有原版输入框（TextField / TextFieldNumeric 等），仅外观变化输入行为不变；外观卡片加开关行（switchId 21，带说明 tooltip，`DrawCheckboxRow` 新增可选 tooltip 参数）
- 新增翻译键 2 个 × 9 语言：`ModernExpandMenu_Md3StyleAllInputs` + `ModernExpandMenu_Md3StyleAllInputsDesc`
- 顺手修复：`ModernExpandMenuSettings` 颜色 `Scribe_Values.Look` 默认值补 `#` 前缀（与字段默认值一致）
- **构建注意**：`GUIStyle.cursorColor` 在本游戏 Unity 版本不存在（编译报错），已移除该设置
- **部署注意**：游戏运行中 DLL 被锁定（user-mapped section open），需关闭游戏后重新部署

## 一批 UI/动画细节 v2 + 可交互预览 + 多段滑块（2026-08-02）
- **回顶曲线只用于回顶**：用户手绘折线曲线（`SampleReturnToTopCurve`）仅用于加载完成后的平滑回顶，已确认
- **加载时菜单展开速度加快**：`WindowUpdate` 高度动画在 `ShowLoadingVisual` 期间速度 ×4（组插入底端前扩到位），到上限后滚动跟随底部，用户能看到组插入底部
- **滚动收起策略多段滑块（2×2 排列组合）**：新设置 `scrollCollapseMode`（int 0~3，默认 1）：
  - bit0（值1）：已展开组不收起（滚出不消失、回归直接显示）
  - bit1（值2）：未展开组不重新加载（标题滚出不消失、回归直接显示）
  - 档位：0 全部收起并重新加载 / 1 已展开组不收起（默认）/ 2 未展开组不重载 / 3 全部不收起
  - `MD3Widgets.MD3SegmentSlider`（多段滑块：轨道 + 分段圆点，点击/拖动吸附最近档位，已选区段主色填充）
  - 动画 tab 速度卡片末尾新增 `DrawSegmentSliderRow`（第一行标签+当前档位说明，第二行多段滑块）
- **可交互预览（颜色 tab 左侧红框）**：新建 `UI/MenuPreviewWidget.cs`——模拟游戏右键分组菜单（3 组 2~4 子项），点击组标题展开/收起（组动画 + 子项目逐条串行出现动画，回归加速），内容超高内部滚动（`MD3Scrollbar` id 3001），实时反映当前动画速度与 MD3 颜色主题；颜色 tab 改两栏（左 40% 预览 + 右调色板/颜色卡片），`ComputeColorSettingsHeight` 估算右侧高度与预览对齐
- **MD3 输入框背景 + 边框**：`ToMd3TextFieldStyle` 改用生成的 64x64 圆角纹理（深色背景 `SurfaceContainerHigh` + 边缘 2px **反色 20%** 边框色，`Color.Lerp(bg, 反色, 0.2)`），9-slice border=10、padding=(10,10,4,4)；纹理随主题背景色缓存重建（换调色板即时生效）；`CornerAlphaAt` 增加半径参数重载
- **设置滚动范围加大**：`ComputeSettingsContentHeight` 各 tab 末尾余量 24 → 48（原估算比实际小 12，底部内容被裁）
- 新增翻译键：`ScrollCollapseMode` + 4 档位说明 + `PreviewGroup`/`PreviewItem`（9 语言）；构建部署验证通过

## 按钮/复选框 MD3 化 + 预览移出设置 + 逐组插入高度（2026-08-02）
- **原版按钮与复选框 MD3 化（可选开关）**：新设置 `md3StyleAllButtons`（默认关闭，与输入框一致）；新 `Patch_Md3StyleAllButtons.cs`：
  - patch `Widgets.DrawButtonGraphic`（所有 ButtonText/ButtonImage 的背景绘制核心）→ MD3 圆角按钮（表面高色 + hover 高亮层 + 按下压暗）
  - patch `Widgets.CheckboxDraw` → MD3 圆角复选框（选中主色填充 + 勾，未选深色描边）
  - 覆盖设置窗口「关闭」、MessageBox「确定」、对话框按钮等所有原版按钮；外观卡片加开关行（switchId 22）
- **组标题离开但子项未离开不收起（不论收起策略）**：`ComputeBlockAnim` 加 `visibilityHeight` 参数；`DrawGroupHeader` 可见性按「组整体高度」（标题 + 已出现子项 × 进度）判断——标题滚出窗口但子项还在视口内时不触发收起
- **子项目匀速串行**：确认 `ComputeBlockAnim` 出现动画为纯线性（offsetX/alpha 无 ease），`ScheduleAppear` 严格串行（上一个动画完 + 间隔后下一个开始）
- **预览移到设置界面外（左侧固定栏）**：`DoSettingsWindowContents` 改为左 36% 固定预览栏（标题 + `MenuPreviewWidget`，**所有 tab 共用**、不随 tab 切换、不滚动）+ 右侧 tab 栏与内容滚动区；颜色与动画速度共用同一个可交互预览；`DrawColorTab` 恢复单栏
- **预览模拟 5 组、子项目翻倍**：`MenuPreviewWidget` 组数 3 → 5，子项目 2/4/8/16/32（每组翻倍）
- **菜单展开动画重构：逐组插入高度**：`WindowUpdate` 加载时窗口高度目标改为「已 reveal 组累计高度」（`ComputeRevealedHeight`）——申请一组高度 → 扩到能插入组的位置 → 插入组就位 → 再申请下一组；到达上限不再加高，改为滚动把上面的组推上去（有位置再插入下一组）；组 reveal 节奏 = 预估加载视觉总时长 / 组数（每组可分配动画时间），进度条结束即最后一组完成，然后动态曲线回顶
- **输入框边框折断修复**：9-slice 圆角 radius 10 → 6、边框 1.5px、padding (8,8,3,3)——低高度输入框不再出现圆角/边框断裂
- 新增翻译键 `Md3StyleAllButtons` + Desc（9 语言）；构建部署验证通过

## 杂项 tab + tab/滚动条 MD3 + 跳过上传倒计时 + 修复（2026-08-02）
- **删除"滚动离开时组的收起策略"**：移除 `scrollCollapseMode` 设置、多段滑块行、`DrawSegmentSliderRow` 方法；`ComputeBlockAnim` 恢复固定行为（已展开组滚出不收起、未展开组回归重新加载）；删除 `ScrollCollapse*` 5 键 × 9 语言；`MD3SegmentSlider` 控件保留待用
- **删除动画 tab 内预览卡片**（与左侧固定预览栏重复）：删 `DrawAnimationPreview` / `DrawPreviewHeaderRow` / `DrawPreviewActionRow` 方法
- **重置菜单修复 + 补键**：缺失 9 个 `Reset*` 键（ResetTitle/ResetModEnabled 等，导致显示 key 原文/伪翻译）全部补全 × 9 语言；新增 `ResetMd3StyleAllInputs` / `ResetMd3StyleAllButtons` / `ResetSkipUploadWait` 重置项 + 键
- **右键殖民者头像不再弹 MD3 菜单**：`Patch_ItemGroupedFloatMenu` 当 `context.ClickedThings` 包含 Pawn（殖民者）时不接管（原版针对 Pawn 的菜单）
- **设置界面 tab 重构**：主 tab 改为 常规 / 动画 / **杂项**（原"颜色"主 tab 移入杂项子 tab）；杂项内含子 tab「**全局样式**」/「颜色」；全局 MD3 开关（输入框/按钮）从常规外观卡片移入杂项→全局样式
- **复选框改为滑动开关样式**：`Patch_CheckboxDraw` 从"圆角方块+勾"改为小号 MD3 滑动开关（选中主色轨道+白色圆点靠右，未选深色轨道+圆点靠左）
- **tab 分页 MD3**：patch `TabRecord.Draw`（原版 TabAtlas 图集）→ MD3 胶囊 tab（选中主色填充、未选表面高色 + hover 高亮）
- **滚动条 MD3**：patch `Widgets.BeginScrollView`，开关开启时把 `GUI.skin.verticalScrollbar/Thumb` 等替换为 MD3 细条（轨道深色 + 4x4 圆角滑块纹理）；注意 Unity 滑块是独立 `verticalScrollbarThumb` 样式（无 GUIStyle.thumb）
- **跳过上传等待倒计时**：新设置 `skipUploadWait`（默认关）；新 `Patch_SkipUploadDelay.cs` patch `Dialog_MessageBox.InteractionDelayExpired`（private 属性，TargetMethod 定位），开关开启且 `interactionDelay > 0`（上传确认框 6s）时立即可交互；普通无延迟消息框不受影响
- 新增翻译键：`TabMisc` / `MiscGlobalStyle` / `SkipUploadWait`(+Desc) / `ResetSkipUploadWait` × 9 语言；构建部署验证通过


## 滑动开关安卓曲线 + 空格仅暂停（2026-08-02）
- **滑动开关圆点动画改安卓 ease-out 曲线**：`MD3ToggleSwitch` 与 `Patch_CheckboxDraw` 圆点用 `animated += (target - animated) * Mathf.Min(1f, Time.deltaTime * 14f)` + 阈值收敛（快速起步、指数减速到位），替换原先的线性 MoveTowards
- **空格仅暂停、不解除暂停**：新设置 `spaceOnlyPauses`（默认关）；新 `Patch_SpaceOnlyPause.cs` `Prefix(TickManager.TogglePause)`（AccessTools.Method 定位），开关开启且 `__instance.Paused && KeyBindingDefOf.TogglePause.KeyDownEvent` 时 return false（空格不解除暂停）；翻译键 `SpaceOnlyPauses`(+Desc) + 重置项
- 构建部署验证通过

## 设置两大类重构 + 杂项配色 + 独立预览窗口（2026-08-02）
- **设置界面两大类**：「扩展菜单」|「其他」（`settingsCategory`），各含子 tab：
  - 扩展菜单类：常规 / 动画 / 颜色 / 预览
  - 其他类：全局样式 / 颜色 / 预览
- **两套独立配色**：扩展菜单配色（`color*`，MD3Theme，右键菜单用）+ 杂项配色（`miscColor*`，新 `Theme/MiscTheme.cs`，全局 MD3 替换功能用）；各自独立调色板 / 16 进制自定义 / 恢复默认分组（`ResetSectionMiscColors`）
- **全局 MD3 patch 改用杂项色**：`Patch_Md3StyleAllButtons`（按钮/复选框/tab/滚动条/滑块数值框）与 `Patch_Md3StyleAllInputs`（输入框）全部从 MD3Theme 切换为 MiscTheme
- **MD3Widgets 支持可选配色**：`DrawHoverState` / `MD3Slider` / `MD3ToggleSwitch` / `MD3TextField` / `ToMd3TextFieldStyle` / `GetMd3TextFieldStyle` 增加可选色参数（默认菜单色）；输入框背景纹理改字典缓存（菜单色/杂项色各一份）
- **预览移出设置界面**（不再占左侧固定栏）：设置内"预览"子 tab 提供按钮打开**独立窗口**
  - `Dialog_MenuPreview.cs`：可交互模拟右键分组菜单（MenuPreviewWidget，菜单配色）
  - `Dialog_MiscPreview.cs`：杂项控件样式预览（按钮/开关/滑块/输入框/滚动条，杂项配色）
- **恢复默认对话框**：补 `ResetSpaceOnlyPauses` 项 + `ResetSectionMiscColors` 分组（13 项杂项色）
- 语言精简为 3 种（English/ChineseSimplified/ChineseTraditional）；新增键：`SettingsCategoryMenu`/`SettingsCategoryMisc`/`SubTabPreview`/`OpenMenuPreviewWindow`/`OpenMiscPreviewWindow`/`MenuPreviewTitle`/`MiscPreviewTitle`/`MiscPreview*`（控件演示）等
- 构建部署验证通过

## 原版 UI 可选 MD3 化（第三批，2026-08-02）
- **tab 黑线修复**：MD3 胶囊 tab 相邻圆角透明接缝透出背景 → 每个 tab 先画整格 tab 栏背景（表面高色）再画胶囊（选中主色覆盖），消除接缝黑线
- **输入框 / 滚动条取色**：全局输入框（`Patch_Md3StyleAllInputs`）与全局滚动条（`Patch_BeginScrollView`）已全部改用**杂项配色**（`MiscTheme`），在设置「其他 → 颜色」独立自定义
- 新 `Patch_Md3StyleMore.cs`（全部可选开关，默认关，仅改外观不改交互）：
  - `md3StyleWindows`：patch `Widgets.DrawWindowBackground` → MD3 圆角表面卡片 + 主色描边（所有原版窗口）
  - `md3StyleCommands`：patch `Command.BGTexture`/`BGTextureShrunk` getter → MD3 圆角背景纹理（征召/解散/攻击等底部命令按钮）
  - `md3StyleMenuSections`：patch `Widgets.DrawMenuSection` → MD3 圆角卡片（药物/食物限制、手术清单、文化列表等区块）
  - `md3StyleSchedule`：patch `TimeAssignmentSelector.DrawTimeAssignmentSelectorFor` → MD3 胶囊（选中主色 + 白色文字）
  - `md3StyleInspectPane`：patch `InspectPaneFiller.DoPaneContentsFor`（内容 MD3 卡片背景）+ **Transpiler 修栏重叠**（`InspectPaneOnGUI` 内容起始 y 26f → 52f，标题区 50f 原先与内容重叠）
  - `md3StyleStatistics`：重写 `MainTabWindow_History.DoStatisticsPage`（private，TargetMethod）→ MD3 分组卡片（基础/财富/袭击/伤亡/结局，`DrawStatsCard`）
  - `md3StyleIdeo`：patch `IdeoUIUtility.DoMeme`（模因大方块圆角描边，选中主色）+ `DrawIdeoRow`（文化行描边 + hover 主色层，注意 out bool 参数需 `ref bool` 匹配）
- 1.6 类型名勘误（旧版资料过时）：信息卡 = `InspectPaneFiller`/`InspectPaneUtility`/`IInspectPane`（无 `InspectPane` 类）；手术 = `ITab_Pawn_Health` + `HealthCardUtility`；食物限制 = `Dialog_ManageFoodPolicies`（非 FoodRestrictions）；文化 = `IdeoUIUtility`/`MainTabWindow_Ideos`；模因 = `MemeDef`（无 `Meme` 类）；统计 = `MainTabWindow_History`（无 Overview）；`IdeoUIUtility.selected`（非 selectedIdeo）
- **dnSpy 反编译经验**：`-t` 需精确类名（用 PowerShell 反射 `GetTypes()` 确认存在性与完整名），带 `_` 的类名有时匹配失败（如 MainTabWindow_Schedule），用完整命名空间 `RimWorld.xxx` 可解决大部分；MCP `read_csharp_symbol` 索引已部分恢复（InspectPaneUtility/MainTabWindow_Ideos/Dialog_ManageFoodPolicies 等可查）
- 新增 7 组开关 + 7 个重置项 + 5 个统计分组标题键 × 3 语言；全局样式卡片扩为 11 行；构建部署验证通过

## 杂项预览窗口覆盖全部杂项 UI 样式（2026-08-02）
- `Dialog_MiscPreview` 从「按钮/开关/滑块/输入框/滚动条」5 区扩展为**覆盖全部 7 个原版 UI MD3 化**的完整预览（改为滚动视口，720x680，主视口 id 504 + 滚动条演示子视口 id 505，独立滚动位置字段 `scrollbarPreviewPosition`）
- 新增预览区：窗口边框（md3StyleWindows 圆角卡片+描边示意）、命令按钮（md3StyleCommands 一排 MD3 圆角按钮+图标占位+hover）、菜单区块/列表行（md3StyleMenuSections 区块+3 行）、管制栏（md3StyleSchedule 5 个胶囊可点击切换，`scheduleSelectedIndex`）、信息卡（md3StyleInspectPane 标题+状态条+文本卡片）、统计卡片（md3StyleStatistics 财富分组卡片，复用原版财富键）、文化菜单（md3StyleIdeo 文化行选中主色描边 + 模因大方块）
- 注意：访问实例字段的辅助方法（DrawSchedulePreview / DrawScrollbarPreview）须为**实例方法**（CS0120）；其余静态
- 新增 17 个预览翻译键 × 3 语言（MiscPreviewWindows/Commands/MenuSections/MenuRow/Schedule/InspectPane/InspectTitle/InspectText/Statistics/Ideo/IdeoName/MemeName/WindowSample*/Cmd*）；构建部署验证通过

## 日志排障：About.xml & 与 TogglePaused 方法名（2026-08-02）
- **About.xml 裸 & 导致 packageId 丢失**：更新描述时写入未转义的 `&`（menu palette & misc palette 等），XML 解析失败（EntityName 错误）→ 整个 About 加载默认值 → 游戏报 missing packageId。修复：`&` → `&amp;`（3 处）；顺带英文语言文件 `Md3StyleAllButtons` 裸 `&` 一并修复。教训：**About.xml / 语言 XML 中写 `&` 必须转义**
- **致命：`TickManager.TogglePause` 在 1.6 不存在 → 模组实例化失败**：`Patch_SpaceOnlyPause.TargetMethod()`（`AccessTools.Method` 定位）返回 null → HarmonyException → `Error while instantiating a mod`，整个模组加载失败。1.6 实际方法是 **`TogglePaused`**（实例、public）。修复方法名后正常
- **教训**：所有 `AccessTools.Method` / `TargetMethod` 定位的目标必须先用反射（PowerShell `GetTypes()` + `GetMethod/GetProperty`）验证存在性与可见性；Harmony 特性 patch 找不到目标方法同样会导致 PatchAll 抛异常使模组无法加载
- 已用反射验证全部 patch 目标存在：TickManager.TogglePaused、Widgets.HorizontalSlider（重载用 Type[] 精确定位）、Dialog_MessageBox.InteractionDelayExpired（属性）、TimeAssignmentSelector.DrawTimeAssignmentSelectorFor、MainTabWindow_History.DoStatisticsPage、IdeoUIUtility.DrawIdeoRow/DoMeme、Widgets.DrawWindowBackground/DrawMenuSection/DrawButtonGraphic/CheckboxDraw/BeginScrollView、TabRecord.Draw、Command.BGTexture(Shrunk)、InspectPaneFiller.DoPaneContentsFor、InspectPaneUtility.InspectPaneOnGUI、Text.CurTextFieldStyle；构建部署验证通过

## 两大根组 + 声音滑块 + 文化/列表界面适配（2026-08-02）
- **右键菜单两大根组**：`BuildGroups` 中「其他」根组由末尾改为**置顶**（`groups.Insert(0, otherGroup)`）；窗口构造中**其他组默认全展开**（加入 `expandedTargets`），物品组保持默认折叠（显示组标题、子项点击展开）

## 圆角纹理重构：按 radius 生成 + 缓存（2026-08-10）
- **根因教训**：旧方案「固定 16px 圆角纹理 + 动态 cornerUv（radius/64）」在 radius<8 时，取的纹理子区域位于角部距离场之外 → alpha 全透明 → 圆角破损成棱角（用户反馈「修黑线后所有 UI 变有棱有角」）；「固定 16/64 UV」则小 radius 四角采样错位 → 拼接竖线
- **新方案**：`GetRoundedRectTexture(radius)` 按 radius **动态生成纹理并缓存**（字典 `roundedRectTextureCache`，clamp 1~16）；`DrawRoundedRect` 取 cornerUv = radius/TextureSize（纹理圆角=目标 radius，子区域正好是完整圆角过渡，四块拼接 UV 连续无接缝）；`DrawTextureWithUv` 增加 `Texture2D texture` 参数（9 处调用全改）
- **新增 `DrawRoundedRectBorder`（只画描边环）**：描边宽度 = 圆角半径（角块 r×r 与边带 r 宽完全匹配，角边无缝），用于「画在已有内容之上且不填充内部」——赛博开关流光边框专用；内部内容（网格/扫光）先画、边框最后画

## 开关风格三段选择器 + 赛博炫酷开关（2026-08-10）
- **设置新增 `switchStyle`（枚举 `SwitchStyle`：Vanilla=0 / Slider=1 / Cyber=2，默认 Slider）**，独立控制**所有复选框/开关**外观（与 `md3StyleAllButtons` 解耦，后者只管按钮/tab/滚动条）；`Patch_CheckboxDraw` 不再看 `md3StyleAllButtons`
- **`MD3SegmentedControl`**（多段选择器，Gemini demo 移植）：外层圆角容器 + 滑动胶囊指示器 + 等宽选项；指示器动画 **easeOutBack**（c1=1.70158，c3=c1+1，`1+c3*(t-1)^3+c1*(t-1)^2`）近似 demo 的 cubic-bezier(0.34,1.56,0.64,1) 回弹；`cyberStyle=true` 时容器变赛博（深色 + 网格 + 流光边框 + 发光指示器）
- **`MD3CyberSwitch`**（卡片式整行赛博开关）：HSV 色相旋转流光边框（`CyberAccentColor(cycle)`）、平铺网格纹理（8x8 十字线 + Repeat wrap + offset 缓慢移动）、开启发光、切换瞬间从左到右扫光亮带（easeOutCubic 0.6s）、徽章处双重冲击波（放大+淡出）、右侧徽章（开启：强调色 + ✓；关闭：暗灰描边圆）；点击整行切换
- **`DrawCyberCheckbox`**（小尺寸赛博开关，原版 checkbox patch 用）：圆角方形轨道 + 流光边框 + 发光圆点 + 扫光，纯绘制（交互由原版继续处理）；动画 id 用坐标 hash + 200000 偏移避免与卡片式冲突
- **设置界面**：`DrawGlobalStyleTab` 顶部加「开关风格」分段选择器行（46f 高，controlId=1000）；`DrawCheckboxRow` 按风格渲染（Vanilla=原版 Checkbox / Slider=MD3ToggleSwitch / Cyber=整行卡片式）；`MiscPreviewWidget` 预览开关同步；`Dialog_ResetDefaults` 加 `ResetSwitchStyle` 项
- 新增键 ×3 语言：`SwitchStyleVanilla/Slider/Cyber/SwitchStyleDesc/ResetSwitchStyle`；lang-exports 同步；**待构建部署验证**
- **声音设置滑块 MD3（图2）**：原版声音设置用 `Listing_Standard.SliderLabeled` → **返回值版** `Widgets.HorizontalSlider(Rect, float, float, float, bool, string, string, string, float)`（非 ref 版），原 ref 版 patch 不覆盖。新增 `Patch_VanillaSliderReturn`（Prefix，`ref __result` 设返回值），与原版一致处理 label 下移与上方标签绘制
- **食物/医药限制、手术清单适配（图4）**：`md3StyleMenuSections` 扩展触发 `DrawButtonGraphic` 与 `TabRecord.Draw` 的 MD3 化（原仅 `md3StyleAllButtons`）——开「菜单区块/列表行」即可让药物/食物限制下拉（Dropdown→ButtonTextDraggable→DrawButtonGraphic）、健康卡「概况/手术」tab（TabDrawer→TabRecord.Draw）、手术清单（AddBill 按钮 + DrawMenuSection 左栏）全部 MD3
- **文化菜单 MD3（图1）**：模因大方块改为 **Prefix 完整重写** `IdeoUIUtility.DoMeme`（原 Postfix 描边不够）——MD3 圆角卡片（表面高色 + 选中主色描边/未选轮廓色 + hover 主色层），保留图标/影响/名称/编辑点击（Dialog_ChooseMemes）；戒律块 `Precept.DrawPreceptBox` Postfix 叠加 MD3 圆角描边 + hover 层；新增 `ClickToEditHint` 翻译键 ×3
- 构建部署验证通过

## 设置界面左侧实时预览区（2026-08-02）
- **需求**：设置菜单左侧扩出独立预览区域（不再依赖单独打开的预览页面），且能实时联动调整
- **布局**：`DoSettingsWindowContents` 改两栏 —— 左侧 36% 实时预览区 + 右侧设置内容区；顶部两大类 tab 不变
- **左侧预览区**（`DrawLivePreview`）：标题「实时预览」+ 两个页签「扩展菜单 | 杂项」
  - 扩展菜单页签：`MenuPreviewWidget`（可交互模拟右键分组菜单，实时反映扩展菜单配色 + 动画速度）
  - 杂项页签：新 `UI/MiscPreviewWidget.cs` —— **模拟原版操作界面被 MD3 化后的样子**：模拟窗口卡片（md3StyleWindows）+ 模拟 tab（概况/手术，md3StyleMenuSections/AllButtons）+ 模拟下拉（Dropdown）+ 模拟按钮行 + 模拟滑块（HorizontalSlider）+ 模拟复选框滑动开关（CheckboxDraw）+ 模拟输入框（TextField）+ 模拟滚动列表（BeginScrollView），全部实时读取杂项配色（MiscTheme），调整「其他→颜色」即时生效
- 原「预览」子 tab（打开独立窗口）保留；新增翻译键：`LivePreviewTitle`/`PreviewMenuTab`/`PreviewMiscTab`/`MiscPreviewWindowTitle`/`MiscPreviewTabOverview`/`MiscPreviewTabSurgery`/`MiscPreviewDropdownLabel`/`MiscPreviewDropdownValue` ×3；构建部署验证通过

## 加载动画对齐 Gemini demo：平滑推顶（2026-08-10）
- 用户提供 Gemini 动画 demo（HTML：高度伸展 + 平滑上推），要求对齐手感
- demo 核心：每插入一项 → 菜单高度平滑伸展（0.35s MD3 曲线）→ 内容超限时用 **350ms easeOutCubic** 从当前滚动位置平滑推顶到新目标（快速起步、指数减速到位）；项目随后从左滑入（0.45s）
- **改造：加载时滚动跟随从「线性 MoveTowards（速度 80）」改为「时间设定平滑推顶」**：
  - 新设置 `scrollFollowDuration`（默认 0.35s，时间设定）替换 `scrollFollowSpeed`（速度式）；设置滑块 id 14 改 0.05~2s
  - `WindowUpdate` 加载块：窗口到最大高度后，每次目标 `maxScroll` 变化（新组插入）→ 从当前 `scrollPosition` 用 `1-(1-t)^3`（easeOutCubic）推顶到新目标；连续插入时持续平滑跟随；未超限时重置（下次插入重新推顶）
  - 动画总开关关闭时仍直接置底
- 弹出动画核对：MEM 已是「从鼠标锚点 ease-out-cubic 缩放展开」（约等于 demo 的 scale 0.9→1 + top-left origin），未改
- 翻译键：`ScrollFollowSpeed`→`ScrollFollowDuration`、`ResetScrollFollowSpeed`→`ResetScrollFollowDuration` ×3；重置项默认 0.35；构建部署验证通过

## 强制上传 / 更新到创意工坊按钮（2026-08-10）
- **需求**：游戏内「上传 / 更新到创意工坊」按钮强制显示并可强制触发上传更新（此前本地副本未订阅时按钮消失）
- **根因**（调研 `Page_ModsConfig` 反编译）：上传入口在模组行「More Actions」右键菜单，条件 `Prefs.DevMode && SteamManager.Initialized && mod.CanToUploadToWorkshop()`；`CanToUploadToWorkshop` 校验 `MayHaveAuthorNotCurrentUser`（工坊作者是当前用户），本地 Mods 目录副本**未订阅**时返回 false → 按钮消失
- **实现**：新 `Patch_ForceWorkshopUpload.cs`（设置 `forceWorkshopUpload`，默认关）：
  - `ModMetaData.CanToUploadToWorkshop` Postfix → 开关开启时强制 `__result = true`（跳过作者校验）
  - `Page_ModsConfig.DoModInfo` Transpiler → 把该方法内**唯一的** `Prefs.DevMode` getter 调用替换为读取运行时标志 `forceDevMode`（Prefix 每帧按设置刷新；不全局改 DevMode），开关开启时上传按钮无需开发者模式
  - 点击后走原版 `Workshop.Upload(mod)`：有 `PublishedFileId` 则更新（`StartItemUpdate`+`SubmitItemUpdate`），无则新建
- 设置界面全局样式卡片扩为 12 行（switchId 32）；翻译键 `ForceWorkshopUpload`(+Desc)+`ResetForceWorkshopUpload` ×3；反射验证 `Page_ModsConfig.DoModInfo`/`CanToUploadToWorkshop`/`Prefs.DevMode` 均存在；构建部署验证通过
- **⚠️ Transpiler 替换指令必须转移 labels**：初版 Transpiler 把 `get_DevMode` 替换为 `Ldsfld` 时未保留原指令 `labels` → 跳转分支引用不存在的 label → `ArgumentException: Label #14 is not marked` → 模组加载失败（用户「不能更新」根因）。修复：`replacement.labels.AddRange(instruction.labels)`。教训：**Transpiler 增删/替换指令时，分支跳转目标的 labels 必须完整转移到新指令**
- **采纳 Gemini 设置页代码的「大类↔预览联动」**：点击顶部大类「扩展菜单」→ 左侧预览自动切到「扩展菜单」页签；点「其他」→ 自动切到「杂项」页签（`DoSettingsWindowContents` 中 `previewTab = 0/1`）；其余 Gemini 代码与当前实现一致；构建部署验证通过

## 开关竖线修复 + 开关动画平滑化（2026-08-10）
- **竖线根因**：`DrawRoundedRect` 的 9-slice UV 固定用 `cornerUv = 16/64`（纹理圆角 16px），但目标 radius 由调用传（滑动开关轨道 10、16px 圆点 8、8px 内点 4）。当 radius 远小于 16 时四角采样纹理 16px 圆角区域 → 四块在中心线拼接处纹理不连续 → **中间出现竖线/接缝截断控件整体**（小尺寸控件最明显，如滑动开关圆点）
- **修复**：`cornerUv = Mathf.Clamp(radius / TextureSize, 0.02f, TextureCorner / TextureSize)` 动态化——取纹理「radius 比例」子区域即得对应半径圆角，拼接连续。影响所有 DrawRoundedRect 调用（更精确）
- **开关动画对齐 Gemini demo**：`MD3ToggleSwitch` 与 `Patch_CheckboxDraw` 圆点动画从「速度式指数 ease-out（deltaTime*14）」改为「**固定 0.18s 时长 + smoothstep 近似 Material standard（cubic-bezier(0.2,0,0,1)）**」——起步更从容、尾段更平滑；新增 `switchAnimationStartTime/StartValue/Target` 与 `checkboxSwitchStart*` 字典，target 变化时从当前进度开始（不跳变）
- 构建部署验证通过

## 待办（第三批：原版 UI 可选 MD3 patch）
- tab 页黑线（MD3 胶囊 tab 相邻重叠）
- 药物/食物限制下拉、手术清单（医疗）、信息卡（内容 MD3 + **栏重叠 bug**）、窗口边框、征召/解散/攻击命令按钮、文化菜单（文化与戒律块）+ 模因大方块、统计界面分组块美化、管制栏（时间表）
- 每项做可选开关（新 Settings bool + 全局样式开关行 + 翻译键 + 重置项）

## 赛博开关 + 分段选择器完全对齐 Gemini demo（2026-08-10）
- **需求**：把已实现的「赛博炫酷开关」和「多段选择器」完全重构，做到与 Gemini 两个 HTML demo（cyber-switch / segmented-switch，已归档 `_AIcoding/ui-refs/`）一致
- **细边框纹理**：`DrawRoundedRectBorder` 从「环宽=圆角半径」改为**固定 2px 细环** —— 新增 `roundedRectBorderTextureCache`（按 radius 生成 64×64 边框环纹理，`BorderAlphaAt` = 外圆角距离场 - 内缩 2px 圆角挖空）+ 9-slice 绘制（角 r×r、边 2px，角边无缝）；修复旧版粗环不符合 demo 的问题
- **三色渐变流光边框**：删除单色 HSV `CyberAccentColor`，新增三色渐变 `CyberGradientColor(t)`（蓝 #00A8FF → 青 #00FFCC → 粉 #FF007F → 蓝 循环）；`DrawCyberGradientBorder` 沿边框 8 段（上/右上/右/右下/下/左下/左/左上）取不同相位着色，`flow = now/3`（3 秒一圈，对齐 demo rainbowGlow 3s）
- **双径向发光**：`DrawCyberRadialGlow`（5 层同心圆递减 alpha 模拟 radial-gradient）——激活卡片右上青色 0.15 + 左下蓝色 0.25（对齐 demo 激活态背景）
- **斜向扫光束**：`DrawCyberSweep` 用 `GUIUtility.RotateAroundPivot(-25°, rect.center)` 旋转矩阵画斜亮条（60% 宽，easeOutCubic 从左到右，淡出；对齐 demo skewX(-25°) sweepAnim）
- **描边冲击波**：`DrawCyberShockwaves` 从「实心圆」改为**描边圆环**（外圆环色 + 内圆卡片背景色挖中心形成 2px 环），颜色青色→粉色 lerp（对齐 demo pulseWave border-color）
- **3D 压感**：`MD3CyberSwitch` 按下时 `rect.ContractedBy(2f)`（近似 demo scale 0.96）
- **徽章**：激活 = 渐变填充（相位流光近似旋转）+ 青色光晕环 + 深色勾（#041019）；关闭 = 半透明白底 + 白描边 + 灰勾
- **分段选择器对齐 demo2**：容器圆角 16（`containerRadius`）+ 指示器圆角 12（`indicatorRadius`）分离；指示器新增**下方投影光晕**（`box-shadow 0 2px 8px` 近似）；cyber 模式指示器用渐变相位色
- 网格移动速度 8→4（对齐 demo 8s 移 2 格）；构建部署验证通过

## 原版「选项」界面设置行卡片化 + 滑块数值输入（2026-08-10）
- **需求（用户截图原版图像设置）**：把原版「选项」界面（Dialog_Options，1.6 已是「左类别列表 + 右 Listing_Standard 流式内容」）里每个设置行（hover 变背景的块）整块变成目标样式（demo 赛博卡片），并给所有滑块加「手动输入数值」
- **新设置 `md3StyleOptions`**（默认关，全局样式卡片第 13 行 switchId 33）：仅当开启**且 Dialog_Options 窗口打开**时生效（不影响健康卡/mod 设置等其它用 Listing_Standard 的界面）
- **新文件 `Patch_Md3StyleOptions.cs`**（已加 csproj）：
  - 行卡片化：patch `Listing_Standard.CheckboxLabeled`（2 重载）/`ButtonTextLabeledPct`/`SliderLabeled`，Prefix 在绘制前画整行卡片背景；样式跟随「开关风格」三切（Vanilla=MD3 圆角卡 / Slider=表面高色+主色描边 / Cyber=完整赛博卡片：流光渐变边框+网格+双发光+hover 发光）——`MD3Widgets.DrawOptionsRowCard`
  - 滑块数值输入：`SliderLabeled` **Prefix 完全重写**（return false + ref __result）——行卡片 + label + 原版滑块 + 行尾 48px 数值按钮，点击进入编辑态（`Widgets.TextFieldNumeric` 限制 min~max），回车/点外部提交；编辑态按 label hash 存静态字典
  - **⚠️ 两个编译坑**：① `HarmonyPatch` 特性参数数组不能含 `MakeByRefType()`（CS0182，需编译期常量）→ 改用 `[HarmonyPatch]` + 静态 `TargetMethod()`（运行时 AccessTools.Method 解析重载）；② `Find.WindowStack.windows` 是 internal（CS1061）→ 改用 `AccessTools.Field(typeof(WindowStack), "windows")` 反射读取
  - `Listing` 基类 `curY`/`listingRect` 是 protected → 反射读写；`ColumnWidth`/`GetRect`/`verticalSpacing` 是 public
- 新增键 ×3 语言 + lang-exports：`Md3StyleOptions`(+Desc)/`ResetMd3StyleOptions`；重置项已加；构建部署验证通过

## 终端中文乱码修复（2026-08-10）
- **现象**：PowerShell 里跑 `cmd /c "build.bat"` 输出中文变「閮ㄧ讲鍒」「鉁?閮ㄧ讲瀹屾垚」
- **根源**：build.bat 以 UTF-8 保存 + `chcp 65001` 输出 UTF-8 字节；而 PowerShell `[Console]::OutputEncoding` 默认 gb2312（cp936）→ 用 GBK 解码 UTF-8 字节 → 乱码
- **修复**：新建 `build.ps1`（PowerShell 版构建脚本）——先 `[Console]::OutputEncoding = UTF8` 再调 `cmd /c "build.bat < NUL"`，透传退出码；以后构建统一用 `./build.ps1`（我跑构建时也用它，不再乱码）
- 其它模组目录（TailorMade 等）build.bat 同样问题，如需要可复制同样 build.ps1 方案

## 设置界面代码分离为独立文件（2026-08-10）
- **需求**：把设置页面的 C# 代码分离成独立文件，便于发给 Gemini 单独重构
- **新文件 `Source/ModernExpandMenu/SettingsUI.cs`**（`public static class SettingsUI`）：设置界面**全部代码**集中在此——
  - 入口 `DrawSettings(Rect)`（原 `DoSettingsWindowContents` 主体）
  - 全部绘制方法：DrawLivePreview / DrawSubTabBar / DrawGeneralTab / DrawAnimationTab / DrawColorTab / DrawGlobalStyleTab / DrawCheckboxRow / DrawSliderRow / DrawColorRow / DrawPaletteCard / 高度计算等
  - 状态字段：settingsCategory / menuSubTab / miscSubTab / previewTab / settingsScrollPosition / 滑块数值编辑态（editingSliderId 等）
  - 用属性 `Settings => ModernExpandMenuMod.Settings` 便捷访问全局设置（方法体无需改动）
- **`ModernExpandMenuMod.cs` 精简为 ~40 行**：仅保留 Mod 核心——HarmonyId、构造函数（PatchAll）、`Settings` 静态实例、`lastPseudoTranslationSetting`（被 Patch_ModSettingsUI 引用，必须留）、`DoSettingsWindowContents` 委托给 `SettingsUI.DrawSettings`、`SettingsCategory()`
- **顺带修复 bug**：`ComputeSettingsContentHeight` 里 globalStyleHeight 从 `rowHeight*12f` 修正为 `rowHeight*13f`（新增 md3StyleOptions 后全局样式卡已是 13 行，原高度算少了最后一行）
- csproj 加 `SettingsUI.cs`；构建部署验证通过；发 Gemini 重构时给 `SettingsUI.cs` + `ModernExpandMenuSettings.cs` 两个文件即可

## 赛博开关完全对齐 Gemini CyberSwitchCard 移植（2026-08-10）
- **需求**：用户反馈之前的赛博开关「完全不符合 demo」，并提供 Gemini 的 WinForms 移植文件 `CyberSwitchCard.cs`（`System.Windows.Forms.Control` + GDI+，无法直接用于 RimWorld，参考其绘制逻辑移植到 Unity GUI）
- **MD3CyberSwitch 完全重写**（签名加 `description` 参数，建议高度 56~64px）：
  - **布局对齐**：左 28px 图标（圆+内点，激活强调色流光/未激活灰）+ 标题/描述两行（激活标题白/描述强调色，未激活灰）+ 右 36px 徽章；卡片背景 #10141E（激活）/ #141720（未激活），圆角 18
  - **动效对齐**：三色流光渐变边框（3s）+ 16px 流动网格（moveSpeed 12）+ 斜向**渐变**扫光（新 `GetCyberSweepGradientTexture` 64×1 正弦渐变纹理，`DrawTextureWithTexCoords` 拉伸 + 旋转 -25°，0.4s easeOutCubic）+ 双重冲击波（青→粉，0.4s，延迟 0.2s）+ 按压缩放
- **设置界面行高动态化**：`CheckboxRowHeight`（赛博 56px / 其它 30px），DrawGeneralTab（外观 5 开关）/DrawGlobalStyleTab（13 开关）/DrawAnimationTab（总开关）/`ComputeSettingsContentHeight` 全部按此动态计算；滑块行保持 30px
- `DrawCheckboxRow` 赛博分支传 `tooltip` 作为描述；`MiscPreviewWidget` 同步（56px + 描述参数）
- 构建部署验证通过
- **⚠️ Gemini 聚合设置中心**：`ModernExpandMenuMod.gemini.cs`（已提供）调用 `SettingsUI.DrawAggregatedSettingsHub(inRect, mods)`（左模组列表 + 右设置）——**该 SettingsUI 聚合版实现文件 Gemini 未提供**，如需整合需自行实现或让 Gemini 补

## WebView2 游戏内集成 PoC（2026-08-10）
- **需求**：用户想「把重构 UI 全部用 WebView2（HTML/CSS）实现」，先做游戏内最小可行原型验证
- **独立验证已完成**：`_AIcoding/ui-preview/WebView2Preview/`（net10.0-windows + Microsoft.Web.WebView2 1.0.4129.50）——WebView2 渲染 Gemini 两个 demo HTML **完美还原**（浏览器级效果）；WebView2 Runtime 已装 151.x
- **游戏内 PoC 架构**：后台 STA 线程跑 WinForms 消息泵（`Application.Run` 隐藏屏幕外透明 Form + WebView2 控件，`NavigateToString` 载入 HTML）→ `CoreWebView2.CapturePreviewAsync(Png, Stream)` 截帧 → PNG 字节传回 Unity 主线程 `Texture2D.LoadImage` 显示
  - `WebView2Host.cs`：静态宿主（Start/Shutdown/RequestCapture/PullLatestPng，跨线程 volatile 交接 PNG）
  - `UI/Dialog_WebView2Test.cs`：测试窗口（约 8fps 定时截帧）
  - 触发入口：设置 → 其他 → 预览 → 「WebView2 测试（PoC）」按钮（临时）
- **⚠️ API 勘误**：WebView2 1.0.4129.50 截图方法是 **`CapturePreviewAsync(imageFormat, Stream)`（Task 版）**，不是旧版 `CapturePreview`（CS1061）；`Window` 关闭回调是 `PreClose()`（不是 `OnClose`，CS0115）
- **部署注意**：WebView2 依赖 dll（`Microsoft.Web.WebView2.Core.dll`/`WinForms.dll`）必须随模组进 `Assemblies/`（RimWorld 只加载模组目录 dll）；build.bat 已加自动复制；`html/cyber-switch-demo.html` 随构建部署
- 待游戏内实测：**未验证**（需用户启动游戏点测试按钮；风险点：Unity 进程内 WinForms 消息泵兼容性、隐藏窗口渲染、截帧性能）

## WebView2 PoC v2：绕开 WinForms（2026-08-10）
- **v1 失败根因（日志）**：`ReflectionTypeLoadException getting types in Microsoft.Web.WebView2.WinForms` + `Could not load type of field 'WebView2Host:_hostForm'`——**RimWorld 跑在 Unity Mono 上，不含 System.Windows.Forms**，所有 WinForms 依赖在游戏进程无法解析 → 整个 WebView2Host 类型加载失败 → Update 持续抛 TypeLoadException
- **v2 方案（纯 Core + Win32，绕开 WinForms）**：`WebView2Host.cs` 重写——
  - 只用 `Microsoft.Web.WebView2.Core.dll`（纯 COM 包装，Unity Mono 可加载）
  - P/Invoke user32 自建**屏幕外隐藏 Win32 窗口**（RegisterClassEx/CreateWindowEx/DefWindowProc）+ **后台 STA 线程消息循环**（GetMessage/TranslateMessage/DispatchMessage）
  - **自定义 SynchronizationContext**（`HostSyncContext`：Post 到 `ConcurrentQueue<Action>`，消息循环每圈 DrainActions）保证 async 延续回到 host 线程
  - `CoreWebView2Environment.CreateAsync` → `CreateCoreWebView2ControllerAsync(hwnd)` → `NavigateToString` → `CapturePreviewAsync(Png)` 截帧
- csproj 移除 System.Windows.Forms/System.Drawing 引用；build.bat 只部署 Core.dll（不部署 WinForms.dll）；已清理游戏部署目录
- **v2 实测崩溃（用户反馈「点测试按钮后无响应崩溃」）**：游戏启动正常（Core.dll 能加载），但点击「WebView2 测试」→ WebView2 初始化（Chromium 进程 + COM）→ **Unity Mono 与 WebView2 运行时集成不兼容 → 崩溃**。结论：**RimWorld（Unity Mono）无法游戏内集成 WebView2**，无论是 WinForms 路线（无 WinForms）还是纯 Core 路线（运行时崩）
- **已回滚**：WebView2Host.cs / Dialog_WebView2Test.cs 归档到 `_AIcoding/ui-preview/poc-backup/`；csproj 移除 WebView2 包与文件；build.bat 移除 Core.dll 部署；设置界面移除测试按钮；游戏恢复可玩
- **替代方向**：① 独立渲染进程 + IPC（命名管道/共享内存）传帧回游戏——效果 100%（浏览器级）但工程量大、常驻 Chromium ~300MB、有 IPC 稳定性风险；② 自绘（WinForms GDI+ demo 与 WebView2 渲染视觉一致）零风险——**推荐用于开关/设置 UI**

## Web Overlay 方案（外挂式悬浮层，2026-08-10）
- **用户思路**：「像游戏外挂 overlay 一样」——独立进程悬浮层承载 HTML UI，不碰游戏进程
- **overlay 方案优势**：WebView2 独立进程渲染（100% 效果）+ 天然接收输入（无需转发）+ 游戏零崩溃风险；省掉「传帧回游戏」和「输入转发」两大最难环节
- **原型已验证（`_AIcoding/ui-preview/Overlay/`）**：`MEMOverlay.exe`（net10.0-windows + WebView2）
  - v1 透明版（TransparencyKey + 分层窗口 + WebView2 透明背景）→ **点击后窗口消失**（透明组合不稳定）
  - v2 稳定版（置顶无边框不透明深色面板）→ 三项全过：悬浮置顶 ✓ HTML 渲染 ✓ 点击交互 ✓
  - v3（当前）：命名管道服务端 + HTML 设置面板（`settings-panel.html` 复用 Gemini demo1 赛博开关卡片 + demo2 三切）+ 双向同步
    - 游戏 → overlay：`{"cmd":"sync","settings":{...}}` / `{"cmd":"set","key","value"}` / `{"cmd":"quit"}`
    - overlay → 游戏：`{"cmd":"ready"}` / `{"cmd":"changed","key","value"}`（HTML 里 `chrome.webview.postMessage` → `WebMessageReceived` 转发管道）
- **游戏端**：`OverlayController.cs`（csproj + System.Text.Json 8.0.5 包）——`Process.Start` 启动 overlay、命名管道 Client、`SendSync` 同步全部设置、收到 `changed` 用 `LongEventHandler.QueueLongEvent(action, key, false, null)` 主线程写回 `Settings`（签名 4 参，注意无 `doAsynchronous` 命名参数）
- **触发**：设置 → 其他 → 预览 tab → 「打开/关闭 Web 悬浮设置面板（HTML）」按钮（`OverlayController.IsRunning` 切换）
- **部署**：build.bat 把 `_AIcoding/ui-preview/Overlay/bin/Release/net10.0-windows/` 整个目录复制到游戏 `Mods\ModernExpandMenu\Overlay\`（exe + WebView2 dll + html，336 文件）
- **⚠️ build.bat 相对路径坑**：`_AIcoding` 在模组目录内，路径应写 `_AIcoding\...` 而非 `..\_AIcoding\...`（前者导致部署失败）
- 管道独立测试通过（PowerShell NamedPipeClientStream 收到 ready + set 生效）；待游戏内实测

## 回归自绘 + 纹理增强 + 清理 Web 路线（2026-08-10）
- **用户决定**：overlay 不做了，还是「游戏内绘制」，但要 C# 自绘表现接近 HTML
- **纹理增强（MD3Widgets）**：
  - 流光边框 `DrawCyberGradientBorder`：8 段 → **36 段沿周长平滑采样**（每条边 8 小段 + 4 圆角块，段间颜色连续，消除色阶分块感）
  - 发光 `DrawCyberRadialGlow`：5 层圆叠加 → **预生成 128×128 径向渐变纹理**（中心白→边缘透明，smoothstep 衰减 + Bilinear 过滤，`GetRadialGradientTexture`），接近 CSS radial-gradient
  - 结论：CSS 的渐变/模糊/阴影/圆角本质是像素处理，预生成纹理 + DrawTexture 完全可复刻（GPU 绘制）；自定义 Shader 在 RimWorld 模组不可行（需 Unity 编辑器打包 AssetBundle）
- **清理 Web 路线全部产物**：
  - 删除：OverlayController.cs、csproj 的 System.Text.Json 包、设置界面 Web 面板按钮、build.bat 的 overlay/html/Core.dll 部署段、游戏 `Mods\...\Overlay\`+`html\` 目录、`_AIcoding/ui-preview/` 下 Overlay/Renderer/WebView2Preview/poc-backup
  - 保留：`CyberSwitchPreview`（WinForms 自绘对照 demo）+ `ui-refs/`（demo HTML 参考）+ 截图
  - 游戏模组目录恢复干净（About/Assemblies/Languages/Source）；构建部署通过

## 文件梳理清理 + 右键菜单对齐 Gemini「高度伸展与平滑上推」demo（2026-08-11）
- **参考 demo**：`C:\Users\YINtx\Downloads\gemini-code-1786403241733.html`（已读，核心动效：容器高度 0.35s cubic-bezier 平滑伸展、每项 wrapper 高度 0→ITEM_HEIGHT 顶出、超出 220px 平滑上推 350ms easeOutCubic、每项从左滑入 0.45s、整体缩放 0.9→1+淡入）
- **梳理清理**：
  - 游戏部署 `Mods\ModernExpandMenu\`：删除 `Source\OverlayController.cs`、`Source\WebView2Host.cs`、`Source\UI\Dialog_WebView2Test.cs`、`Source\bin`、`Source\obj`、`Source\ModernExpandMenu\bin\obj`、`About\Preview.png.old`、顶层残留 `Source\ModernExpandMenu\` 目录
  - 源码：删 `About\Preview.png.old`、`_AIcoding/ui-preview/` 下 `overlay-test.png`、`webview2-preview.png`（WebView2/overlay 废弃截图）
  - 保留：`CyberSwitchPreview`（对照 demo）、`ui-refs/`、`settings-ui-replica.html`（给 Gemini 的参考）、`animation-requirements.md`、`settings-ui-redesign-brief.md`
- **⚠️ build.bat bug 修复（重要）**：`xcopy /E /Y /Q /I Source\ModernExpandMenu\*.cs` 的 `/E` 会**递归**把 `obj\` 内生成的 `.cs`（如 AssemblyInfo.cs）复制进部署目录，而清理逻辑在部署**前**执行 → 每次构建后 `%MODS_DIR%\Source\bin`、`obj` 又出现。修复：该行去掉 `/E`（只复制顶层 .cs，Theme/UI/Properties 由单独逻辑复制）+ 部署末尾追加清理 bin/obj（含 `Source\ModernExpandMenu\bin\obj` 历史残留）
- **动画对齐 demo（MD3FloatMenuWindow.cs，高度判定仍用模组自定义 maxMenuHeight/ComputeContentHeight，不照搬 demo 的 220/42px）**：
  - 弹出淡入：新增 `popAlpha` 与缩放同步（ease-out-cubic），窗口背景/描边/覆盖层按 alpha 淡入
  - 窗口高度动画：`MoveTowards`（线性速度）→ **指数衰减 ease-out**（`height += (target-height) * (1-exp(-dt*speed*0.02))`，快速起步减速到位，对齐 demo height 平滑伸展）
  - **每项占高平滑伸展**（对齐 demo wrapper 高度动画）：新增 `GetEntryHeightFactor`——项排定后占高从 0 ease-out-cubic 伸展到 1（时长=itemAppearDuration），`ComputeActionsHeight` 按因子累计 → 新项插入时平滑「顶出」
  - 滑入动画：`-20px 线性` → **ease-out-cubic 缓动 + 56px 距离**（`SlideInDistance` 常量），更接近 demo 左滑入
  - 平滑上推（scrollFollow easeOutCubic 0.35s）与回顶曲线（用户手绘）**保持原有**（已与 demo 一致）
  - 构建部署通过，部署目录彻底干净

## 竖线修复 + 删除选项界面卡片化 + 展开菜单全部 MD3 接管（2026-08-11）
- **滑动开关白色圆球竖线 / 颜色预设块竖线（根治）**：根因 = `DrawRoundedRect` 的 9-slice 四角在中心拼接，非整数 radius（如小开关圆点）产生 1px 垂直缝隙（露底色竖线）。修复：`DrawRoundedRect` 加**圆形快速路径**——当 `|宽-高|<0.5 && |宽-2×radius|<0.5`（正方形+半宽圆角=圆形）时改用单张 64×64 圆纹理（`GetCircleTexture`，中心不透明边缘平滑）整块绘制，无拼接。圆球/圆点/胶囊全部走此路径，竖线根治
- **彻底删除「原版选项界面卡片化 + 滑块数值输入」（md3StyleOptions）**：删除 `Patch_Md3StyleOptions.cs`（csproj 引用、设置字段、SettingsUI 开关行、Dialog_ResetDefaults 重置项、MD3Widgets 的 DrawOptionsRowCard/DrawCyberRowCard、3 语言 + lang-exports 键全部清掉）
- **新增设置 `md3StyleFloatMenus`（默认 true）：原版展开菜单（FloatMenu/FloatMenuGrid）全部 MD3 接管** —— 新文件 `Patch_Md3StyleFloatMenus.cs`：
  - `Patch_FloatMenuOptionDoGUI`：Prefix 完全接管每行绘制（保留原版布局/交互/图标/extraPart/tooltip/Chosen，仅换外观：hover 主色圆角行 + MD3 文本色；私有字段用 AccessTools 缓存反射）
  - `Patch_FloatMenuDoWindowContents`：整窗 MD3 卡片背景（圆角 + 描边）
  - `Patch_FloatMenuGridOptionOnGUI` + `Patch_FloatMenuGridDoWindowContents`：网格式菜单同样处理
  - 覆盖场景：**「选择语言」下拉**（Dialog_Options → FloatMenu）、殖民者菜单、Mod 设置下拉、Ideo 图标网格等全部原版展开菜单；模组自绘右键菜单（MD3FloatMenuWindow）不走此路径不受影响
  - 注意：`MouseoverSounds` 在 `Verse.Sound`、`TutorSystem` 在 `RimWorld`、`GenStuff` 在 `RimWorld`、`MD3Widgets` 在 `ModernExpandMenu.UI`
- **右键菜单动画对齐 Gemini「高度伸展与平滑上推」demo**：等 Gemini 新版 demo 后重写（本轮未动）
- 构建部署通过（0 错误），部署目录干净

## 医药设置 MD3 + 竖线彻底根治 + 跑马灯边框 + 接管范围（2026-08-11）
- **竖线彻底根治（DrawRoundedRect 取整）**：上轮圆形快速路径只解决圆形，非整数坐标的 9-slice 仍会在按钮右侧 / 开关轨道左侧产生拼接缝。本轮在 `DrawRoundedRect` 开头把 rect 坐标与 radius 统一 `Mathf.Round` 取整 → 所有 9-slice 块整数对齐，缝隙消失（根治）
- **展开菜单接管范围（新设置 `floatMenuTakeoverScope` 枚举，默认 All）**：`All`（全部 FloatMenu+Grid）/ `DialogDropdowns`（仅对话框下拉——窗口栈中存在类名以 `Dialog` 开头的窗口时接管；原版无统一 Dialog_ 基类，按类名约定判断）/ `Off`（关闭）。网格菜单仅在 All 时接管。`FloatMenuTakeoverHelper.ShouldTakeover` 统一判断
- **菜单跑马灯边框（新设置 `menuMarqueeBorder`，默认 true）**：新增 `MD3Widgets.DrawMarqueeBorder`——沿周长 36 段主色亮度波峰流动（波峰亮×1.15、波谷暗×0.35，仿 VS Code Copilot 输入框等待输出的流动光带）。应用于右键菜单（MD3FloatMenuWindow）与原版展开菜单（FloatMenu/Grid）背景：开启跑马灯用主色流动边框，关闭用加厚描边（1px→2px）；FloatMenu 行内缩 2px 与边框留间隔
- **「默认医药设置」选择器 MD3 化（新设置 `md3StyleMedicalCare`，默认关，新文件 `Patch_Md3StyleMedicalCare.cs`）**：Patch `MedicalCareUtility.MedicalCareSetter`（Prefix 完全接管，覆盖 Dialog_MedicalDefaults 与健康 tab 所有调用处）——5 个图标格 MD3 圆角：hover 主色底、选中主色描边；保留拖动连续涂色（`medicalCarePainting`）、tooltip、音效。注意：`DraggableResult.AnyPressed` 是 internal 扩展（模组不可调用），用 `==Pressed || ==DraggedThenPressed` 等价替代
- **踩坑**：RimWorld 无 `Dialog_` 基类（对话框直接继承 `Window`）——判断对话框按类名 `StartsWith("Dialog")`
- 设置 UI：全局样式卡新增「展开菜单接管范围」三段选择器 + 跑马灯开关（34）+ 医药开关（35），卡高更新（两个分段选择器 + 15 行开关）；ResetDefaults 三新项；3 语言 + lang-exports 同步（旧 Md3StyleFloatMenus 键已移除）
- 构建部署通过（0 错误），部署目录干净

## Gemini demo 重写：动画双模式 + Copilot 顶条 + 圆环自适应 + 杀线程 + 回顶等待（2026-08-11）
- **参考 demo**：`C:\Users\YINtx\Downloads\gemini-code-1786406813368.html`（Copilot 极致控制台右键菜单 demo：顶条光带跑马灯、中央 SVG 圆环 MIN_LOADING_HEIGHT 申请、杀线程 AbortController、回顶前等待 300ms、全局倍速、二级子项目、高级控制面板）
- **动画速度双模式（设置页动画 tab，用户明确要求）**：
  - 新增 `AnimationSpeedMode` 枚举（Multiplier 倍率 / Custom 自定义，默认倍率）+ `animationSpeedMultiplier`（0.2~3，默认 1）
  - 动画 tab 新增「动画速度模式」卡片：分段选择器切换模式；倍率模式 → 全局倍率滑块可用，7 个单项滑块灰显禁用（`DrawSliderRow` 新增 disabled 参数：标签/数值/滑块全部低饱和灰化且拒绝交互）；自定义模式 → 单项可用，倍率灰显禁用
  - `MD3FloatMenuWindow` 所有 `Current*` 动画参数应用倍率：时长类（滑入/间隔/弹出/推顶/回顶）÷ 倍率、速度类（展开/高度）× 倍率
- **顶端加载条 Copilot 改造**（`DrawLoadingBar` 重做）：轨道 + 平滑进度 + 前端亮点 + **脉冲缓冲**（呼吸脉动，缓冲长度匹配实际进度）+ **Copilot 光带跑马灯**（新开关 `loadingBarMarquee`，`MD3Widgets.DrawHorizontalSweep` 用 64×1 渐变纹理横向循环扫过）
- **中央圆环空间自适应**：加载时窗口高度目标 `Mathf.Clamp(内容, MinLoadingHeight=120, Max)` —— 初始高度不足时自动申请 120px 容纳圆环（对齐 demo MIN_LOADING_HEIGHT）；加载完成后目标 = 实际内容高度，自动平滑缩回包裹内容
- **杀线程**：`Patch_ItemGroupedFloatMenu` 再次触发右键时先 `Close(false)` 关闭上一次 MD3FloatMenuWindow 再开新窗（替代原来「已存在则 return」，避免新旧窗口叠加卡顿）
- **回顶前等待**：新设置 `scrollReturnWaitSeconds`（默认 0.3s，对齐 demo 300ms）——加载视觉结束后先停顿再执行自定义折线回顶
- **新开关/参数**：`loadingBarMarquee`（顶条光带）、`loadingMaskOpacity`（加载遮罩透明度，默认 0.25）、`scrollReturnWaitSeconds`；均入设置 UI（加载卡/动画卡）+ 重置项 + 3 语言 + lang-exports
- 构建部署通过（0 错误），部署目录干净

## 设置窗口放大 + 描边归组 + 边框三态（2026-08-11）
- **设置窗口放大**：Patch `Dialog_ModSettings.InitialSize`（Getter），仅当 `mod` 是本模组时返回 1180×860（原 900×700），不影响其他 Mod 设置窗口
- **「文本-描边」归类修正**：`colorOutline`（Outline 描边色）确实放错了组——从「文本」颜色卡移到**新建的「边框」颜色卡**（文本卡变 3 行：OnSurface/OnSurfaceVariant/DisabledText；边框卡 1 行：Outline）
- **边框样式三态（对齐 Ultimate Cyberpunk Switch Demo 的彩色边框动效）**：`menuMarqueeBorder`（bool）升级为 `MenuBorderStyle` 枚举 { Outline 普通描边 / Marquee 主色跑马灯 / Rainbow 彩色流光 }，应用于**右键菜单与展开菜单共用**：
  - Outline：加厚描边（2px，Outline 色）
  - Marquee：主色亮度波沿边框流动（DrawMarqueeBorder）
  - Rainbow：三色渐变沿边框流动（DrawCyberGradientBorder，3s 一圈，对齐 demo rainbowGlow）
  - 全局样式卡原「跑马灯开关」改为**边框样式三段选择器**；重置项、3 语言 + lang-exports 同步（旧 MenuMarqueeBorder 键已删）
- 构建部署通过（0 错误），部署目录干净

## 绘制窗口/按钮毛刺修复（2026-08-11）
- **根因**：`CornerAlphaAt` 圆角 alpha 是**硬边**（`Clamp01(radius - distance)`，1px 内 0→1 跳变）——圆角边缘在放大/双线性过滤后出现锯齿、发毛；且圆角矩形/描边环纹理未显式设置过滤模式
- **修复**：
  1. `CornerAlphaAt` 圆角边缘 **1.5px smoothstep 平滑过渡**（消除硬边锯齿），所有基于它的绘制（卡片/按钮/圆角矩形/描边环/圆形快速路径）全部受益
  2. `CreateRoundedRectTexture` / `CreateRoundedRectBorderTexture` 显式设置 `wrapMode=Clamp` + `filterMode=Bilinear`（9-slice UV 不越界、边缘柔和）
- 构建部署通过（0 错误），部署目录干净

## 展开菜单统一右键菜单同款 + 其他类置顶（2026-08-11）
- **展开菜单（FloatMenu/FloatMenuGrid 接管）统一右键菜单同款样式**：`Patch_FloatMenuOptionDoGUI` 行绘制加**左侧主色竖条（常驻，disabled 行不显示）** + hover 主色圆角 —— 与 `MD3FloatMenuWindow` 操作项（竖条 + hover 圆角 + 图标 + 文本）完全一致，消除两套观感
- **「其他 / 物品」两大类排序修复（重要 bug）**：`BuildGroups` 已把「其他」组 `Insert(0)` 置顶，但分帧生成后 `FinalizeGroups` 的排序写反（`return aIsOther ? 1 : -1` 把其他排到**末尾**）→ 最终其他类跑到物品类下面。修复：改为 `return aIsOther ? -1 : 1`（其他类置顶），与 BuildGroups 一致
- 构建部署通过（0 错误），部署目录干净


