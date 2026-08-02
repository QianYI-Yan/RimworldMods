using HarmonyLib;
using ModernExpandMenu.Theme;
using ModernExpandMenu.UI;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 可选功能：把原版按钮 / 复选框 / tab 分页 / 滚动条统一改成 MD3 样式。
    // 按钮背景经 Widgets.DrawButtonGraphic，复选框经 Widgets.CheckboxDraw，
    // tab 经 TabRecord.Draw（TabAtlas），滚动条经 GUI.skin 样式——patch 后全局替换外观，
    // 点击 / 文字 / 交互逻辑保持不变。由设置 md3StyleAllButtons 控制（默认关闭）。
    // ═══════════════════════════════════════════════════
    public static class Patch_Md3StyleAllButtons
    {
        /// <summary>原版按钮背景（所有 Widgets.ButtonText / ButtonImage 共用）→ MD3 圆角按钮。</summary>
        [HarmonyPatch(typeof(Widgets), "DrawButtonGraphic")]
        private static class Patch_DrawButtonGraphic
        {
            private static bool Prefix(Rect rect)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons)
                {
                    return true;
                }
                // MD3 圆角按钮背景：表面高色填充 + hover 高亮层 + 按下压暗
                MD3Widgets.DrawRoundedRect(rect, MD3Theme.SurfaceContainerHigh, 6f);
                if (Mouse.IsOver(rect))
                {
                    if (Input.GetMouseButton(0))
                    {
                        MD3Widgets.DrawRoundedRect(rect, new Color(0f, 0f, 0f, 0.15f), 6f);
                    }
                    else
                    {
                        MD3Widgets.DrawHoverState(rect, 6f);
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 原版复选框绘制 → MD3 滑动开关样式（安卓开关）：
        /// 选中主色轨道 + 白色圆点靠右，未选深色轨道 + 圆点靠左。
        /// </summary>
        [HarmonyPatch(typeof(Widgets), "CheckboxDraw")]
        private static class Patch_CheckboxDraw
        {
            private static bool Prefix(float x, float y, bool active, bool disabled, float size, Texture2D texChecked, Texture2D texUnchecked)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons)
                {
                    return true;
                }
                // 小号滑动开关：轨道（宽 1.4×size，高 0.7×size）+ 白色圆点
                float trackWidth = size * 1.4f;
                float trackHeight = size * 0.7f;
                var track = new Rect(x + (size - trackWidth) / 2f, y + (size - trackHeight) / 2f, trackWidth, trackHeight);
                Color trackColor = active ? (disabled ? MD3Theme.DisabledText : MD3Theme.Primary) : MD3Theme.SurfaceContainerHigh;
                MD3Widgets.DrawRoundedRect(track, trackColor, trackHeight / 2f);
                if (!active)
                {
                    // 未选：内缩表面色，形成描边
                    MD3Widgets.DrawRoundedRect(track.ContractedBy(1f), MD3Theme.Surface, trackHeight / 2f - 1f);
                }
                float knobSize = trackHeight;
                float knobX = track.x + (track.width - knobSize) * (active ? 1f : 0f);
                var knob = new Rect(knobX, track.y + (track.height - knobSize) / 2f, knobSize, knobSize);
                MD3Widgets.DrawRoundedRect(knob, disabled ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white, knobSize / 2f);
                return false;
            }
        }

        /// <summary>原版 tab 分页（TabRecord.Draw 用 TabAtlas 图集绘制）→ MD3 胶囊 tab。</summary>
        [HarmonyPatch(typeof(TabRecord), "Draw")]
        private static class Patch_TabRecordDraw
        {
            private static bool Prefix(TabRecord __instance, Rect rect)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons)
                {
                    return true;
                }
                // MD3 胶囊 tab：选中主色填充，未选表面高色 + hover 高亮层
                bool selected = __instance.Selected;
                MD3Widgets.DrawRoundedRect(rect, selected ? MD3Theme.Primary : MD3Theme.SurfaceContainerHigh, 6f);
                if (!selected && Mouse.IsOver(rect))
                {
                    MD3Widgets.DrawHoverState(rect, 6f);
                }
                GUI.color = selected ? MD3Theme.OnPrimary : MD3Theme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(rect, __instance.label);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;   // 帧末必须为 true
                GUI.color = Color.white;
                return false;
            }
        }

        // MD3 滚动条样式（轨道深色 + 滑块圆角），开关开启时应用到 GUI.skin
        private static GUIStyle md3ScrollbarStyle;
        private static GUIStyle md3ScrollbarThumbStyle;
        private static Texture2D md3ScrollbarTrackTex;
        private static Texture2D md3ScrollbarThumbTex;

        /// <summary>原版滚动条（Unity GUI.BeginScrollView 用 GUI.skin 滚动条样式）→ MD3 细条。</summary>
        [HarmonyPatch(typeof(Widgets), "BeginScrollView")]
        private static class Patch_BeginScrollView
        {
            private static void Prefix()
            {
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons)
                {
                    return;
                }
                if (md3ScrollbarStyle == null)
                {
                    md3ScrollbarTrackTex = SolidColorMaterials.NewSolidColorTexture(Theme.MD3Theme.ScrollbarTrack);
                    md3ScrollbarThumbTex = CreateMd3ScrollbarThumbTexture(Theme.MD3Theme.ScrollbarThumb);

                    md3ScrollbarThumbStyle = new GUIStyle();
                    md3ScrollbarThumbStyle.normal.background = md3ScrollbarThumbTex;
                    md3ScrollbarThumbStyle.border = new RectOffset(2, 2, 2, 2);
                    md3ScrollbarThumbStyle.margin = new RectOffset(1, 1, 0, 0);
                    md3ScrollbarThumbStyle.overflow = new RectOffset(0, 0, 0, 0);

                    md3ScrollbarStyle = new GUIStyle();
                    md3ScrollbarStyle.normal.background = md3ScrollbarTrackTex;
                    md3ScrollbarStyle.border = new RectOffset(0, 0, 0, 0);
                    md3ScrollbarStyle.margin = new RectOffset(0, 0, 0, 0);
                    md3ScrollbarStyle.padding = new RectOffset(0, 0, 0, 0);
                    md3ScrollbarStyle.overflow = new RectOffset(0, 0, 0, 0);
                    md3ScrollbarStyle.fixedWidth = 6f;
                }
                // Unity 滚动条：轨道与滑块是独立的 GUIStyle（verticalScrollbar / verticalScrollbarThumb）
                GUI.skin.verticalScrollbar = md3ScrollbarStyle;
                GUI.skin.horizontalScrollbar = md3ScrollbarStyle;
                GUI.skin.verticalScrollbarThumb = md3ScrollbarThumbStyle;
                GUI.skin.horizontalScrollbarThumb = md3ScrollbarThumbStyle;
            }

            /// <summary>生成 4x4 圆角滑块纹理（四角透明，中间不透明）。</summary>
            private static Texture2D CreateMd3ScrollbarThumbTexture(Color color)
            {
                const int size = 4;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool corner = (x == 0 && y == 0) || (x == size - 1 && y == 0) || (x == 0 && y == size - 1) || (x == size - 1 && y == size - 1);
                        Color c = color;
                        c.a = corner ? 0f : 1f;
                        pixels[y * size + x] = c;
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                texture.hideFlags = HideFlags.HideAndDontSave;
                return texture;
            }
        }
    }
}
