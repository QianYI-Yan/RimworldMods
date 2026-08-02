using System;
using UnityEngine;

namespace ModernExpandMenu.Theme
{
    // ═══════════════════════════════════════════════════
    // MD3（Material Design 3）主题 Token
    // 颜色全部可从模组设置读取 16 进制自定义（未配置时回退默认水影蓝）。
    // 所有 UI 绘制只从这里取值，不硬编码颜色。
    // ═══════════════════════════════════════════════════
    public static class MD3Theme
    {
        // ── 默认色板：水影（LiquidBounce）风格深色蓝色调 ──
        public static readonly Color DefaultPrimary = new Color32(0, 168, 255, 255);
        public static readonly Color DefaultOnPrimary = new Color32(0, 20, 33, 255);
        public static readonly Color DefaultSurface = new Color32(22, 24, 33, 255);
        public static readonly Color DefaultSurfaceContainer = new Color32(30, 33, 45, 255);
        public static readonly Color DefaultSurfaceContainerHigh = new Color32(38, 42, 58, 255);
        public static readonly Color DefaultOnSurface = new Color32(230, 230, 236, 255);
        public static readonly Color DefaultOnSurfaceVariant = new Color32(154, 155, 166, 255);
        public static readonly Color DefaultOutline = new Color32(99, 102, 118, 255);
        public static readonly Color DefaultDisabledText = new Color32(128, 128, 140, 255);
        public static readonly Color DefaultShadow = new Color32(0, 0, 0, 90);
        public static readonly Color DefaultScrollbarTrack = new Color32(38, 38, 46, 64);
        public static readonly Color DefaultScrollbarThumb = new Color32(82, 82, 97, 166);
        public static readonly Color DefaultScrollbarThumbDragging = new Color32(115, 115, 133, 204);

        // ── 颜色（从设置读取 16 进制自定义；Settings 未初始化时用默认）──
        public static Color Primary => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorPrimary, DefaultPrimary);
        public static Color OnPrimary => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorOnPrimary, DefaultOnPrimary);
        public static Color Surface => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorSurface, DefaultSurface);
        public static Color SurfaceContainer => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorSurfaceContainer, DefaultSurfaceContainer);
        public static Color SurfaceContainerHigh => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorSurfaceContainerHigh, DefaultSurfaceContainerHigh);
        public static Color OnSurface => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorOnSurface, DefaultOnSurface);
        public static Color OnSurfaceVariant => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorOnSurfaceVariant, DefaultOnSurfaceVariant);
        public static Color Outline => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorOutline, DefaultOutline);
        public static Color DisabledText => FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorDisabledText, DefaultDisabledText);

        // hover 状态层跟随主色（半透明）；阴影 / 滚动条仅自定义 RGB，透明度固定
        public static Color HoverStateLayer
        {
            get
            {
                Color c = Primary;
                c.a = 40f / 255f;
                return c;
            }
        }

        public static Color Shadow
        {
            get
            {
                Color c = FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorShadow, DefaultShadow);
                c.a = 0.35f;
                return c;
            }
        }

        public static Color ScrollbarTrack
        {
            get
            {
                Color c = FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorScrollbarTrack, DefaultScrollbarTrack);
                c.a = 0.25f;
                return c;
            }
        }

        public static Color ScrollbarThumb
        {
            get
            {
                Color c = FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorScrollbarThumb, DefaultScrollbarThumb);
                c.a = 0.65f;
                return c;
            }
        }

        public static Color ScrollbarThumbDragging
        {
            get
            {
                Color c = FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.colorScrollbarThumbDragging, DefaultScrollbarThumbDragging);
                c.a = 0.8f;
                return c;
            }
        }

        /// <summary>解析 16 进制颜色（可带 # 前缀，如 #RRGGBB 或 RRGGBB），失败返回兜底色。</summary>
        public static Color FromHex(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return fallback;
            }
            string clean = hex.TrimStart('#').Trim();
            if (clean.Length < 6)
            {
                return fallback;
            }
            try
            {
                int r = Convert.ToInt32(clean.Substring(0, 2), 16);
                int g = Convert.ToInt32(clean.Substring(2, 2), 16);
                int b = Convert.ToInt32(clean.Substring(4, 2), 16);
                return new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

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
