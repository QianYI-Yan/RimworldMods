# MD3 UI 风格规范（工作区首选）

> 本规范定义工作区所有 RimWorld 模组 UI 的 MD3（Material Design 3）风格要求。
> 参考实现：`ModernExpandMenu` 的 `Theme/MD3Theme.cs`（色板 Token）与 `UI/MD3Widgets.cs`（自绘控件）。

## 1. 核心原则

| 规则 | 说明 |
|------|------|
| 色板 Token 化 | 所有颜色从主题读取（Primary / Surface / OnSurface 等），禁止在 UI 代码里硬编码颜色 |
| 自绘控件 | 圆角矩形、卡片、hover 层、安卓开关、滑块、按钮、滚动条全部自绘（`MD3Widgets`），不使用原版 GUI 皮肤 |
| 圆角统一 | 窗口 / 卡片 / 行 / 按钮圆角用预生成圆角纹理 + 九宫格拉伸（`DrawRoundedRect`），不用原版圆角 |
| 滚动条 | MD3 细滚动条（5px 宽，轨道 + 滑块 + 拖动高亮），不用原版 `showScrollbars` |
| 输入框 | 深色圆角背景 + 主色描边环 + 无边框原生输入（`GUI.TextField`），不叠加原版输入框外观；描边用轮廓不填充，避免盖住文字 |
| 对话框 | 背景自绘卡片 + 自绘关闭按钮，不用原版窗口背景 |

## 2. 色板 Token（MD3 Baseline 深色，全部可设置自定义）

| Token | 用途 | 默认值 |
|-------|------|--------|
| `Primary` | 主色（强调、填充、选中） | `#00A8FF` 水影蓝 |
| `OnPrimary` | 主色上的文本 | `#001421` |
| `Surface` | 窗口/卡片表面 | `#161821` |
| `SurfaceContainer` | 次级容器（卡片内嵌、轨道） | `#1E212D` |
| `SurfaceContainerHigh` | 强调表面（标题行、按钮底） | `#262A3A` |
| `OnSurface` | 主文本 | `#E6E6EC` |
| `OnSurfaceVariant` | 次要文本 | `#9A9BA6` |
| `Outline` | 描边 | `#636676` |
| `DisabledText` | 禁用文本 | `#80808C` |
| `Shadow` | 卡片阴影（alpha 固定 0.35） | `#000000` |
| `ScrollbarTrack` / `Thumb` / `ThumbDragging` | 滚动条三段 | `#26262E` / `#525261` / `#737385` |
| `HoverStateLayer` | hover 半透明层（主色 alpha 40/255） | 跟随主色 |

## 3. 尺寸常量

| 常量 | 值 | 用途 |
|------|-----|------|
| `WindowCornerRadius` | 8 | 悬浮窗/对话框外角 |
| `HeaderCornerRadius` | 10 | 分组标题圆角 |
| `ActionCornerRadius` | 8 | 操作行 hover 圆角 |
| `CardCornerRadius` | 10 | 设置分组卡片圆角 |
| `MenuWidth` | 340 | 悬浮窗宽度 |
| `Padding` | 10 | 窗口内边距 |
| `GroupHeaderHeight` | 34 | 组标题行高 |
| `ItemRowHeight` | 30 | 操作项行高 |
| `GroupGap` | 6 | 组间距 |
| `ActionIndent` | 12 | 操作项缩进 |
| `ScrollbarWidth` | 5 | MD3 滚动条宽度 |

## 4. 控件 API（`MD3Widgets`）

| API | 用途 |
|-----|------|
| `DrawRoundedRect(rect, color, radius)` | 圆角矩形（预生成纹理九宫格拉伸，无角时退化为实心） |
| `DrawRoundedRectOutline(rect, outline, radius, thickness, fill)` | 圆角描边（外框 + 内缩填充，不覆盖内部文字） |
| `DrawCard(rect, surface, radius)` | 卡片：底部柔和阴影 + 圆角表面 |
| `DrawHoverState(rect, radius)` | hover 半透明主色层 |
| `DrawVerticalFade(rect, color, opaqueAtBottom)` | 垂直渐变遮罩（滚动内容上下边缘淡出） |
| `MD3ToggleSwitch(rect, value, switchId)` | 安卓滑动开关（圆角轨道 + 白色圆点 + 滑动动画） |
| `MD3Slider(rect, value, min, max, sliderId)` | 滑块（主色填充 + 圆形滑块 + 点击/拖动） |
| `MD3SegmentSlider(rect, value, segmentCount, sliderId)` | 多段滑块（离散档位，点击吸附最近档位，用于排列组合类选项） |
| `MD3Button(rect, label, emphasized)` | 按钮（主色强调或深色次要 + hover 高亮） |
| `MD3TextField(rect, text, fieldId, valid)` | 输入框（深色圆角背景 + 主色/红色描边环 + 无边框原生输入，非法时红框） |
| `MD3NumberField(rect, ref value, ref buffer, min, out sub, out cancel)` | 数值输入框（深色背景 + 主色描边环 + 原版可靠输入，Enter 提交 / ESC 取消） |
| `ToMd3TextFieldStyle(original)` | 把原版输入框样式转成 MD3（深色圆角背景 + 反色 20% 边框，供"原版输入框全局 MD3"可选功能） |
| `MD3BeginScrollView` / `MD3EndScrollView` / `MD3Scrollbar` | MD3 滚动视口 + 细滚动条（拖动支持，scrollbarId 区分多个） |

## 5. 绘制约定

- **帧末状态还原**：修改 `Text.Anchor` / `Text.WordWrap` / `GUI.color` 后必须还原（`WordWrap` 帧末必须为 true，否则 RimWorld 报错）
- **滚动视口**：`MD3BeginScrollView` 内 `Mouse.IsOver` / `ButtonInvisible` 用局部坐标（BeginGroup 会转换鼠标位置）
- **窗口弹出**：MD3 悬浮窗从鼠标锚点缩放弹出（ease-out-cubic）
- **列表出现动画**：分组标题 / 操作项从左侧水平滑入 + alpha 渐变；滚动进出可视范围时播放出现/消失动画
- **加载视觉**：半透明覆盖层（黑 alpha 0.25）+ 中央环形进度 + 百分比 + 顶部加载条
- **坐标**：`windowRect` 用 GUI 左上原点；鼠标位置用 `UI.MousePositionOnUIInverted`（不是 `MousePositionOnUI`）

## 6. 设置界面规范

- 设置窗口用 MD3 卡片 + Tab 栏（选中主色填充的胶囊按钮）
- 开关用 `MD3ToggleSwitch`（switchId 分段避免冲突：主界面 0~4、滑块 10~16、对话框 1000+/2000+）
- 滑块用 `MD3Slider` + 可点击数值按钮（点击进入编辑态，`GUI.FocusControl` 自动聚焦）
- 离散选项（排列组合）用 `MD3SegmentSlider` 多段滑块（点击吸附最近档位）
- 可交互预览：左侧固定预览栏（所有 tab 共用），模拟游戏菜单（组标题点击展开/收起 + 子项逐条出现动画），实时反映动画速度与颜色主题
- 颜色用 16 进制输入框 + 色块（点击复制）+ 粘贴按钮 + 调色板预设
- 恢复默认用差异对比对话框（树状分组：常规 / 动画 / 颜色）

## 7. 参考

- 完整实现：`ModernExpandMenu/Source/ModernExpandMenu/`
  - `Theme/MD3Theme.cs` — 色板 Token 与尺寸常量
  - `UI/MD3Widgets.cs` — 自绘控件库
  - `UI/MD3FloatMenuWindow.cs` — MD3 悬浮窗（含动画体系）
  - `ModernExpandMenuMod.cs` — MD3 设置界面（Tab / 卡片 / 开关 / 滑块 / 多段滑块 / 调色板 / 可交互预览）
  - `Patch_Md3StyleAllInputs.cs` / `Patch_Md3StyleAllButtons.cs` — 可选功能：原版输入框 / 按钮 / 复选框全局 MD3 化
  - `UI/Dialog_ResetDefaults.cs`、`UI/Dialog_ConfigManager.cs` — MD3 对话框
