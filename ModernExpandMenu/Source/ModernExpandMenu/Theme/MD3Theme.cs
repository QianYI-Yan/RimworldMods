using UnityEngine;

namespace ModernExpandMenu.Theme
{
    // ═══════════════════════════════════════════════════
    // MD3（Material Design 3）主题 Token
    // 集中管理颜色、圆角、间距等样式变量。
    // 所有 UI 绘制只从这里取值，不硬编码颜色，
    // 便于后续接入 CSS 解析器做外观自定义。
    // ═══════════════════════════════════════════════════
    public static class MD3Theme
    {
        // ── 色板：水影（LiquidBounce）风格深色蓝色调 ──────
        public static readonly Color Primary = new Color32(0, 168, 255, 255);            // 主色（水影标志蓝）
        public static readonly Color OnPrimary = new Color32(0, 20, 33, 255);           // 主色上的前景

        public static readonly Color Surface = new Color32(22, 24, 33, 255);            // 窗口表面（深蓝黑）
        public static readonly Color SurfaceContainer = new Color32(30, 33, 45, 255);   // 次级表面
        public static readonly Color SurfaceContainerHigh = new Color32(38, 42, 58, 255); // 强调表面（组标题）

        public static readonly Color OnSurface = new Color32(230, 230, 236, 255);       // 表面上的主文本（浅色）
        public static readonly Color OnSurfaceVariant = new Color32(154, 155, 166, 255); // 表面上的次要文本
        public static readonly Color Outline = new Color32(99, 102, 118, 255);          // 描边

        public static readonly Color HoverStateLayer = new Color32(0, 168, 255, 40);    // hover 状态层（蓝色半透明）
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.35f);             // 卡片阴影
        public static readonly Color DisabledText = new Color(0.5f, 0.5f, 0.55f, 1f);   // 不可执行项文本

        // 深灰细滚动条（更明显，支持鼠标拖动）
        public static readonly Color ScrollbarTrack = new Color(0.15f, 0.15f, 0.18f, 0.25f);
        public static readonly Color ScrollbarThumb = new Color(0.32f, 0.32f, 0.38f, 0.65f);
        public static readonly Color ScrollbarThumbDragging = new Color(0.45f, 0.45f, 0.52f, 0.8f);

        // ── 尺寸 ───────────────────────────────────────
        public const float WindowCornerRadius = 8f;    // 悬浮窗外角圆角
        public const float HeaderCornerRadius = 10f;   // 物品组标题圆角
        public const float ActionCornerRadius = 8f;    // 操作项 hover 圆角
        public const float CardCornerRadius = 10f;     // 设置界面分组卡片圆角

        public const float MenuWidth = 340f;           // 悬浮窗宽度
        public const float MaxMenuHeight = 560f;       // 悬浮窗最大高度（超出滚动）
        public const float Padding = 10f;              // 窗口内边距（上下左右均衡）

        public const float GroupHeaderHeight = 34f;    // 组标题行高
        public const float ItemRowHeight = 30f;        // 操作项行高
        public const float GroupGap = 6f;              // 组间距
        public const float ActionIndent = 12f;         // 操作项缩进（体现子菜单层级）
        public const float ScrollbarWidth = 5f;        // 自定义浅色滚动条宽度
    }
}
