using UnityEngine;

namespace ModernExpandMenu.Theme
{
    // ═══════════════════════════════════════════════════
    // 杂项配色（全局 MD3 替换功能专用）：
    // 按钮 / 复选框 / tab / 滚动条 / 滑块 / 输入框等"杂项"控件用这一套配色，
    // 与"扩展菜单"（右键分组菜单）的 MD3Theme 配色分开，各自独立自定义。
    // ═══════════════════════════════════════════════════
    public static class MiscTheme
    {
        public static Color Primary => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorPrimary, MD3Theme.DefaultPrimary);
        public static Color OnPrimary => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorOnPrimary, MD3Theme.DefaultOnPrimary);
        public static Color Surface => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorSurface, MD3Theme.DefaultSurface);
        public static Color SurfaceContainer => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorSurfaceContainer, MD3Theme.DefaultSurfaceContainer);
        public static Color SurfaceContainerHigh => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorSurfaceContainerHigh, MD3Theme.DefaultSurfaceContainerHigh);
        public static Color OnSurface => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorOnSurface, MD3Theme.DefaultOnSurface);
        public static Color OnSurfaceVariant => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorOnSurfaceVariant, MD3Theme.DefaultOnSurfaceVariant);
        public static Color Outline => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorOutline, MD3Theme.DefaultOutline);
        public static Color DisabledText => MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorDisabledText, MD3Theme.DefaultDisabledText);

        /// <summary>hover 状态层（跟随杂项主色，半透明）。</summary>
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
                Color c = MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorShadow, MD3Theme.DefaultShadow);
                c.a = 0.35f;
                return c;
            }
        }

        public static Color ScrollbarTrack
        {
            get
            {
                Color c = MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorScrollbarTrack, MD3Theme.DefaultScrollbarTrack);
                c.a = 0.25f;
                return c;
            }
        }

        public static Color ScrollbarThumb
        {
            get
            {
                Color c = MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorScrollbarThumb, MD3Theme.DefaultScrollbarThumb);
                c.a = 0.65f;
                return c;
            }
        }

        public static Color ScrollbarThumbDragging
        {
            get
            {
                Color c = MD3Theme.FromHex(ModernExpandMenu.ModernExpandMenuMod.Settings?.miscColorScrollbarThumbDragging, MD3Theme.DefaultScrollbarThumbDragging);
                c.a = 0.8f;
                return c;
            }
        }
    }
}
