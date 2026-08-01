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


