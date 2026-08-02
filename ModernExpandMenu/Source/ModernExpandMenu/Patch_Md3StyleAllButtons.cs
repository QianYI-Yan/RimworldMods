using System;
using System.Collections.Generic;
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
                // md3StyleAllButtons：全局按钮；md3StyleMenuSections：药物/食物限制下拉、手术清单等列表界面的按钮
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons && !ModernExpandMenuMod.Settings.md3StyleMenuSections)
                {
                    return true;
                }
                // MD3 圆角按钮背景：表面高色填充 + hover 高亮层 + 按下压暗（杂项配色）
                MD3Widgets.DrawRoundedRect(rect, MiscTheme.SurfaceContainerHigh, 6f);
                if (Mouse.IsOver(rect))
                {
                    if (Input.GetMouseButton(0))
                    {
                        MD3Widgets.DrawRoundedRect(rect, new Color(0f, 0f, 0f, 0.15f), 6f);
                    }
                    else
                    {
                        MD3Widgets.DrawHoverState(rect, 6f, MiscTheme.HoverStateLayer);
                    }
                }
                return false;
            }
        }

        // 复选框滑动开关的圆点滑动动画进度（key = 坐标 hash）
        private static readonly Dictionary<int, float> checkboxSwitchProgress = new Dictionary<int, float>();

        /// <summary>
        /// 原版复选框绘制 → MD3 滑动开关样式（安卓开关）：
        /// 选中主色轨道 + 白色圆点靠右，未选深色轨道 + 圆点靠左；圆点有平滑滑动动画。
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

                // 圆点滑动动画（0=左/关，1=右/开）：安卓 ease-out 曲线（快速起步、指数减速到位）
                int key = Mathf.RoundToInt(x * 7f + y * 13f);
                float target = active ? 1f : 0f;
                float animated = checkboxSwitchProgress.TryGetValue(key, out float cur) ? cur : target;
                animated += (target - animated) * Mathf.Min(1f, Time.deltaTime * 14f);
                if (Mathf.Abs(animated - target) < 0.001f)
                {
                    animated = target;
                }
                checkboxSwitchProgress[key] = animated;

                Color trackColor = active ? (disabled ? MiscTheme.DisabledText : MiscTheme.Primary) : MiscTheme.SurfaceContainerHigh;
                MD3Widgets.DrawRoundedRect(track, trackColor, trackHeight / 2f);
                if (!active)
                {
                    // 未选：内缩表面色，形成描边
                    MD3Widgets.DrawRoundedRect(track.ContractedBy(1f), MiscTheme.Surface, trackHeight / 2f - 1f);
                }
                float knobSize = trackHeight;
                float knobX = track.x + (track.width - knobSize) * animated;
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
                // md3StyleAllButtons：全局；md3StyleMenuSections：健康卡（手术/概况）等列表界面的 tab
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons && !ModernExpandMenuMod.Settings.md3StyleMenuSections)
                {
                    return true;
                }
                // MD3 胶囊 tab：选中主色填充，未选表面高色 + hover 高亮层（杂项配色）
                // 先画 tab 栏背景（表面高色填满整格）再画胶囊，消除相邻胶囊圆角透明接缝产生的黑线
                bool selected = __instance.Selected;
                MD3Widgets.DrawRoundedRect(rect, MiscTheme.SurfaceContainerHigh, 6f);
                if (selected)
                {
                    MD3Widgets.DrawRoundedRect(rect, MiscTheme.Primary, 6f);
                }
                if (!selected && Mouse.IsOver(rect))
                {
                    MD3Widgets.DrawHoverState(rect, 6f, MiscTheme.HoverStateLayer);
                }
                GUI.color = selected ? MiscTheme.OnPrimary : MiscTheme.OnSurface;
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
                    md3ScrollbarTrackTex = SolidColorMaterials.NewSolidColorTexture(MiscTheme.ScrollbarTrack);
                    md3ScrollbarThumbTex = CreateMd3ScrollbarThumbTexture(MiscTheme.ScrollbarThumb);

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

        // 原版滑块数值输入编辑态（key = 滑块坐标 hash，-1 表示无编辑）
        private static int vanillaSliderEditingHash = -1;
        private static string vanillaSliderEditingBuffer = "";
        private static Rect vanillaSliderEditingRect;

        /// <summary>
        /// 原版滑块（Widgets.HorizontalSlider ref 版本，设置界面常用）→ MD3 滑块 + 右侧数值输入：
        /// 滑块换 MD3 样式；右侧加数值框（点击进入编辑态，回车提交 / ESC 或点击外部取消）。
        /// </summary>
        [HarmonyPatch]
        private static class Patch_VanillaSlider
        {
            // ref float 参数需 MakeByRefType 精确定位（不能写在特性实参里）
            private static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Widgets), "HorizontalSlider", new System.Type[]
                {
                    typeof(Rect), typeof(float).MakeByRefType(), typeof(FloatRange), typeof(string), typeof(float)
                });
            }

            private static bool Prefix(Rect rect, ref float value, FloatRange range, string label, float roundTo)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons)
                {
                    return true;
                }
                // label 非空时原版会把滑块下移（label 文本由调用方画在 rect 上方）
                Rect sliderRect = rect;
                if (!label.NullOrEmpty())
                {
                    sliderRect.y += Mathf.Round((rect.height - 10f) / 2f) + 5f;
                    sliderRect.height = Mathf.Max(10f, rect.yMax - sliderRect.y);
                }
                // 布局：左侧滑块 + 右侧数值输入框
                float valueBoxWidth = 56f;
                Rect barRect = new Rect(sliderRect.x, sliderRect.y, sliderRect.width - valueBoxWidth - 6f, sliderRect.height);
                Rect valueRect = new Rect(sliderRect.xMax - valueBoxWidth, sliderRect.y, valueBoxWidth, sliderRect.height);
                int id = Mathf.RoundToInt(sliderRect.y * 97f + sliderRect.x * 31f);

                value = MD3Widgets.MD3Slider(barRect, value, range.TrueMin, range.TrueMax, id, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh);
                if (roundTo > 0f)
                {
                    value = Mathf.Round(value / roundTo) * roundTo;
                }

                // 数值输入框：显示当前值，点击进入编辑态
                string valueText = roundTo > 0f ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
                if (vanillaSliderEditingHash == id)
                {
                    vanillaSliderEditingRect = valueRect;
                    string controlName = "VanillaSliderValue" + id;
                    GUI.SetNextControlName(controlName);
                    MD3Widgets.MD3TextField(valueRect, vanillaSliderEditingBuffer, id, float.TryParse(vanillaSliderEditingBuffer, out _));
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                    {
                        if (float.TryParse(vanillaSliderEditingBuffer, out float parsed))
                        {
                            value = Mathf.Clamp(parsed, range.TrueMin, range.TrueMax);
                        }
                        vanillaSliderEditingHash = -1;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                    {
                        vanillaSliderEditingHash = -1;
                        Event.current.Use();
                    }
                }
                else
                {
                    MD3Widgets.DrawRoundedRect(valueRect, MiscTheme.SurfaceContainerHigh, 4f);
                    if (Mouse.IsOver(valueRect))
                    {
                        MD3Widgets.DrawHoverState(valueRect, 4f, MiscTheme.HoverStateLayer);
                    }
                    GUI.color = MiscTheme.Primary;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(valueRect, valueText);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                    if (Widgets.ButtonInvisible(valueRect))
                    {
                        vanillaSliderEditingHash = id;
                        vanillaSliderEditingBuffer = valueText;
                    }
                }
                // 点击其他区域取消编辑
                if (vanillaSliderEditingHash >= 0 && Event.current.type == EventType.MouseDown && !Mouse.IsOver(vanillaSliderEditingRect))
                {
                    vanillaSliderEditingHash = -1;
                }
                return false;
            }
        }

        /// <summary>
        /// 原版滑块返回值版（Widgets.HorizontalSlider 不带 ref，Listing_Standard.SliderLabeled 使用，
        /// 声音设置 / 自动存档数 / 地图拖拽灵敏度等界面）→ MD3 滑块（开关 md3StyleAllButtons）。
        /// 与原版一致处理 label 下移与滑块上方标签绘制。
        /// </summary>
        [HarmonyPatch]
        private static class Patch_VanillaSliderReturn
        {
            private static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Widgets), "HorizontalSlider", new System.Type[]
                {
                    typeof(Rect), typeof(float), typeof(float), typeof(float),
                    typeof(bool), typeof(string), typeof(string), typeof(string), typeof(float)
                });
            }

            private static bool Prefix(Rect rect, float value, float min, float max, bool middleAlignment, string label, string leftAlignedLabel, string rightAlignedLabel, float roundTo, ref float __result)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleAllButtons)
                {
                    return true;
                }
                // 与原版一致的 rect 调整（label / middleAlignment 时滑块下移，给上方标签让位）
                Rect sliderRect = rect;
                if (middleAlignment || !label.NullOrEmpty())
                {
                    sliderRect.y += Mathf.Round((rect.height - 10f) / 2f);
                }
                if (!label.NullOrEmpty())
                {
                    sliderRect.y += 5f;
                }
                int id = Mathf.RoundToInt(sliderRect.y * 97f + sliderRect.x * 31f);
                __result = MD3Widgets.MD3Slider(sliderRect, value, min, max, id, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh);
                if (roundTo > 0f)
                {
                    __result = Mathf.Round(__result / roundTo) * roundTo;
                }
                // 滑块上方标签（与原版一致：左 / 右 / 居中标签）
                if (!label.NullOrEmpty() || !leftAlignedLabel.NullOrEmpty() || !rightAlignedLabel.NullOrEmpty())
                {
                    float labelHeight = label.NullOrEmpty() ? 18f : Text.CalcSize(label).y;
                    var labelRect = new Rect(rect.x, rect.y - labelHeight + 3f, rect.width, rect.height);
                    Text.Font = GameFont.Small;
                    if (!leftAlignedLabel.NullOrEmpty())
                    {
                        Text.Anchor = TextAnchor.UpperLeft;
                        Widgets.Label(labelRect, leftAlignedLabel);
                    }
                    if (!rightAlignedLabel.NullOrEmpty())
                    {
                        Text.Anchor = TextAnchor.UpperRight;
                        Widgets.Label(labelRect, rightAlignedLabel);
                    }
                    if (!label.NullOrEmpty())
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(labelRect, label);
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                return false;
            }
        }
    }
}
