# MD3 UI 控件库模板

可复用的 **Material Design 3** 风格 UI 控件库源码，供工作区各 RimWorld 模组复制使用。

## 定位

| 目录 | 用途 |
|------|------|
| `_templates/md3-ui-style/` | **规范文档**（色板 Token / 尺寸 / 绘制约定 / 控件 API 清单） |
| `_templates/md3-ui-lib/`（本目录） | **可复制代码**（MD3Theme.cs + MD3Widgets.cs） |

规范看 STYLE_GUIDE.md，代码从这里复制。

## 文件

- `MD3Theme.cs` — 主题 Token（色板 / 阴影 / 滚动条色 / 尺寸常量）+ 十六进制颜色解析
- `MD3Widgets.cs` — 绘制控件（卡片 / 圆角 / hover / 滚动条 / 按钮 / 开关 / 滑块 / 输入框 / 淡出遮罩）

## 使用步骤（新模组接入）

1. **复制源码**到目标项目：
   ```
   Source/YourMod/Theme/MD3Theme.cs
   Source/YourMod/UI/MD3Widgets.cs
   ```
2. **改命名空间**：
   - `MD3Theme.cs`：`namespace YourMod.Theme`
   - `MD3Widgets.cs`：`namespace YourMod.UI`
   - （`MD3Widgets` 内用相对引用 `Theme.MD3Theme`，C# 会向上解析到 `YourMod.Theme`，无需改）
3. **csproj** 加编译项：
   ```xml
   <Compile Include="Theme\MD3Theme.cs" />
   <Compile Include="UI\MD3Widgets.cs" />
   ```
4. **使用**：
   ```csharp
   using YourMod.Theme;
   using YourMod.UI;
   // MD3Widgets.DrawCard(rect, MD3Theme.Surface, MD3Theme.CardCornerRadius);
   ```

## 设计要点

- **已解耦**：MD3Theme 颜色为默认「水影蓝」深色色板，**不依赖任何模组设置**（不再读取 ModernExpandMenuMod.Settings）。如需自定义，可自行加「设置 → 颜色注入」（参考 ModernExpandMenu 的 `ModernExpandMenuSettings` 颜色字段 + `FromHex`）。
- **纹理**：圆角矩形 64x64 九宫格 + 1x16 渐变遮罩，`[StaticConstructorOnStartup]` 主线程生成，消除启动警告。
- **滚动条**：MD3 细条（`MD3BeginScrollView` / `MD3EndScrollView`），不用原版 `showScrollbars`。

## 控件 API 清单

| 方法 | 说明 |
|------|------|
| `DrawCard(rect, surfaceColor, radius)` | 卡片背景（含阴影） |
| `DrawRoundedRect(rect, color, radius)` / `DrawRoundedRectOutline(...)` | 圆角矩形 / 描边环 |
| `DrawHoverState(rect, radius)` | hover 高亮层（主色半透明） |
| `MD3BeginScrollView(rect, ref pos, contentRect)` / `MD3EndScrollView(rect, ref pos, contentHeight, id, cornerInset)` / `MD3Scrollbar(...)` | MD3 细滚动条 |
| `MD3Button(rect, label, emphasized=false)` | MD3 按钮 |
| `MD3ToggleSwitch(rect, value, id)` | 开关 |
| `MD3Slider(rect, value, min, max, id)` | 滑块 |
| `MD3TextField(rect, text, id, valid)` | 输入框（深色底 + 描边环） |
| `MD3NumberField(rect, ref value, ref buffer, min, out submitted, out cancelled)` | 数字输入 |
| `DrawVerticalFade(rect, color, opaqueAtBottom)` | 滚动上下边缘淡出遮罩 |

## 色板 Token（MD3Theme 静态属性）

`Primary` / `OnPrimary` / `Surface` / `SurfaceContainer` / `SurfaceContainerHigh` / `OnSurface` / `OnSurfaceVariant` / `Outline` / `DisabledText` / `HoverStateLayer` / `Shadow` / `ScrollbarTrack` / `ScrollbarThumb` / `ScrollbarThumbDragging`

尺寸常量：`WindowCornerRadius` / `HeaderCornerRadius` / `ActionCornerRadius` / `CardCornerRadius`

## 参考实现

- `ModernExpandMenu/Source/ModernExpandMenu/Theme/MD3Theme.cs` + `UI/MD3Widgets.cs`（完整应用，含颜色设置注入）
- `asrtylsUIMod-ZhCN/Source/AstrylsUIZhCN/Theme/` + `UI/`（解耦版复制，聚合设置界面卡片块）

## 维护注意

- **RimWorld 模组间不能引用对方 DLL**，共享 UI 库只能**源码复制**（各模组独立命名空间），无法直接引用程序集。
- 修改库代码后需手动同步到各使用项目（或写脚本批量替换命名空间复制）。
