# ModernRClickMenu 开发笔记

## 模组信息
- mod ID: `yintx.deepseek.modernRclickmenu`
- 位置: `D:\Github\RimworldMods\ModernRClickMenu`
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

