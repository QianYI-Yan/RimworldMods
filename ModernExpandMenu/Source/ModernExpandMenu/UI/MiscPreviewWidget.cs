using ModernExpandMenu.Theme;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    /// <summary>
    /// 可交互杂项样式预览：模拟一份"原版操作界面"被 MD3 化后的样子
    /// （窗口边框 / tab / 下拉 / 按钮 / 滑块 / 复选框滑动开关 / 输入框 / 滚动列表），
    /// 全部实时读取杂项配色（MiscTheme）——调整"其他 → 颜色"时即时生效。
    /// 用于设置界面左侧实时预览区。
    /// </summary>
    public static class MiscPreviewWidget
    {
        // 预览交互状态（模拟原版界面中的可交互控件）
        private static int previewTabIndex;         // 模拟 tab：0=概况，1=手术
        private static bool switchValue;
        private static float sliderValue = 0.6f;
        private static string inputText;
        private static Vector2 scrollPosition;

        /// <summary>绘制模拟原版操作界面（MD3 化后），实时反映杂项配色。</summary>
        public static void Draw(Rect rect)
        {
            float x = rect.x;
            float width = rect.width;
            float y = rect.y;

            // ── 模拟原版窗口（md3StyleWindows 效果：圆角卡片 + 主色描边）──
            var winRect = new Rect(x, y, width, rect.height);
            MD3Widgets.DrawRoundedRect(winRect, MiscTheme.Surface, 8f);
            MD3Widgets.DrawRoundedRectOutline(winRect, MiscTheme.Outline, 8f, 1.5f, MiscTheme.Surface);
            float innerX = winRect.x + 12f;
            float innerWidth = winRect.width - 24f;
            float cy = winRect.y + 10f;

            // 窗口标题栏
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(innerX, cy, innerWidth, 20f), "ModernExpandMenu_MiscPreviewWindowTitle".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            cy += 26f;

            // ── 模拟 tab（TabRecord MD3 胶囊）──
            string[] tabs = { "ModernExpandMenu_MiscPreviewTabOverview".Translate(), "ModernExpandMenu_MiscPreviewTabSurgery".Translate() };
            float tabGap = 6f;
            float tabWidth = (innerWidth - tabGap) / 2f;
            for (int i = 0; i < tabs.Length; i++)
            {
                var tabRect = new Rect(innerX + (tabWidth + tabGap) * i, cy, tabWidth, 24f);
                bool selected = previewTabIndex == i;
                MD3Widgets.DrawRoundedRect(tabRect, selected ? MiscTheme.Primary : MiscTheme.SurfaceContainerHigh, 6f);
                if (!selected && Mouse.IsOver(tabRect))
                {
                    MD3Widgets.DrawHoverState(tabRect, 6f, MiscTheme.HoverStateLayer);
                }
                GUI.color = selected ? MiscTheme.OnPrimary : MiscTheme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(tabRect, tabs[i]);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(tabRect))
                {
                    previewTabIndex = i;
                }
            }
            cy += 32f;

            // ── 模拟下拉（Dropdown MD3 按钮）──
            DrawRowLabel(innerX, cy, "ModernExpandMenu_MiscPreviewDropdownLabel".Translate());
            var dropdownRect = new Rect(innerX + 78f, cy - 2f, innerWidth - 78f, 24f);
            MD3Widgets.DrawRoundedRect(dropdownRect, MiscTheme.SurfaceContainerHigh, 6f);
            MD3Widgets.DrawRoundedRectOutline(dropdownRect, MiscTheme.Outline, 6f, 1f, MiscTheme.SurfaceContainerHigh);
            if (Mouse.IsOver(dropdownRect))
            {
                MD3Widgets.DrawHoverState(dropdownRect, 6f, MiscTheme.HoverStateLayer);
            }
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(dropdownRect.x + 8f, dropdownRect.y, dropdownRect.width - 24f, dropdownRect.height), "ModernExpandMenu_MiscPreviewDropdownValue".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            // ▾ 箭头
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = MiscTheme.Primary;
            Widgets.Label(new Rect(dropdownRect.xMax - 20f, dropdownRect.y, 14f, dropdownRect.height), "▾");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(dropdownRect))
            {
                switchValue = !switchValue;   // 无实际菜单，仅作点击反馈
            }
            cy += 32f;

            // ── 模拟按钮行（ButtonText MD3）──
            float btnWidth = (innerWidth - 8f) / 2f;
            if (MD3Widgets.MD3Button(new Rect(innerX, cy, btnWidth, 26f), "ModernExpandMenu_MiscPreviewButtonNormal".Translate(), false)) { }
            if (MD3Widgets.MD3Button(new Rect(innerX + btnWidth + 8f, cy, btnWidth, 26f), "ModernExpandMenu_MiscPreviewButtonEmphasized".Translate(), true)) { }
            cy += 34f;

            // ── 模拟滑块（HorizontalSlider MD3）──
            DrawRowLabel(innerX, cy, "ModernExpandMenu_MiscPreviewSlider".Translate());
            sliderValue = MD3Widgets.MD3Slider(new Rect(innerX + 78f, cy - 2f, innerWidth - 78f, 24f), sliderValue, 0f, 1f, 604, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh);
            cy += 32f;

            // ── 模拟复选框（CheckboxDraw → 滑动开关）──
            DrawRowLabel(innerX, cy, "ModernExpandMenu_MiscPreviewSwitch".Translate());
            switchValue = MD3Widgets.MD3ToggleSwitch(new Rect(innerX + 78f, cy - 2f, 110f, 24f), switchValue, 605, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh, MiscTheme.Surface);
            cy += 32f;

            // ── 模拟输入框（TextField MD3）──
            DrawRowLabel(innerX, cy, "ModernExpandMenu_MiscPreviewInput".Translate());
            inputText = MD3Widgets.MD3TextField(new Rect(innerX + 78f, cy - 2f, innerWidth - 78f, 26f), inputText, 606, true, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh, MiscTheme.OnSurface);
            cy += 34f;

            // ── 模拟滚动列表（BeginScrollView MD3 细滚动条）──
            if (cy + 36f < winRect.yMax - 10f)
            {
                float listHeight = winRect.yMax - 10f - cy;
                const float contentHeight = 160f;
                var viewRect = new Rect(innerX, cy, innerWidth, Mathf.Max(40f, listHeight));
                var contentRect = new Rect(0f, 0f, innerWidth - 18f, contentHeight);
                MD3Widgets.MD3BeginScrollView(viewRect, ref scrollPosition, contentRect);
                for (int i = 0; i < 6; i++)
                {
                    var itemRect = new Rect(6f, i * 26f, innerWidth - 30f, 20f);
                    MD3Widgets.DrawRoundedRect(itemRect, MiscTheme.SurfaceContainerHigh, 4f);
                    GUI.color = MiscTheme.OnSurface;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.WordWrap = false;
                    Widgets.Label(new Rect(itemRect.x + 8f, itemRect.y, itemRect.width - 16f, itemRect.height), "ModernExpandMenu_MiscPreviewScrollItem".Translate(i + 1));
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = true;
                    GUI.color = Color.white;
                }
                MD3Widgets.MD3EndScrollView(viewRect, ref scrollPosition, contentHeight, 607, MD3Theme.CardCornerRadius);
            }
        }

        /// <summary>绘制行标签（杂项主色，左侧固定宽度）。</summary>
        private static void DrawRowLabel(float x, float y, string label)
        {
            GUI.color = MiscTheme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(x, y, 74f, 20f), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
        }
    }
}
