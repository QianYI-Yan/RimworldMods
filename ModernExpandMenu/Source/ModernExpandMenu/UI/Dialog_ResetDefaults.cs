using System;
using System.Collections.Generic;
using ModernExpandMenu.Theme;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // 恢复默认设置确认对话框（MD3 风格）：
    //   树状结构列出（常规 / 动画 / 颜色 分组），每项显示当前值与默认值的差异，
    //   可勾选是否恢复，支持全选 / 反选，确定后仅恢复勾选项。
    // ═══════════════════════════════════════════════════
    public class Dialog_ResetDefaults : Window
    {
        private readonly List<ResetSection> sections = new List<ResetSection>();
        private Vector2 scrollPosition;
        private const float RowHeight = 30f;
        private const float SectionHeaderHeight = 28f;

        public Dialog_ResetDefaults()
        {
            doWindowBackground = false;
            drawShadow = false;
            layer = WindowLayer.Dialog;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnClickedOutside = false;

            BuildSections();
        }

        public override Vector2 InitialSize => new Vector2(580f, 680f);

        private void BuildSections()
        {
            ModernExpandMenuSettings s = ModernExpandMenuMod.Settings;

            // ── 常规 ──
            var general = new ResetSection("ModernExpandMenu_ResetSectionGeneral".Translate());
            general.items.Add(Item("ModernExpandMenu_ResetModEnabled", s.modEnabled, true, () => s.modEnabled = true));
            general.items.Add(Item("ModernExpandMenu_ResetDisablePseudoTranslation", s.disableDevPseudoTranslation, false, () => s.disableDevPseudoTranslation = false));
            general.items.Add(Item("ModernExpandMenu_ResetShowLoadingAnimation", s.showLoadingAnimation, true, () => s.showLoadingAnimation = true));
            general.items.Add(Item("ModernExpandMenu_ResetShowHoverHighlight", s.showHoverHighlightAndArrow, true, () => s.showHoverHighlightAndArrow = true));
            general.items.Add(Item("ModernExpandMenu_ResetShowItemCount", s.showItemCount, true, () => s.showItemCount = true));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleAllInputs", s.md3StyleAllInputs, false, () => s.md3StyleAllInputs = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleAllButtons", s.md3StyleAllButtons, false, () => s.md3StyleAllButtons = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleWindows", s.md3StyleWindows, false, () => s.md3StyleWindows = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleCommands", s.md3StyleCommands, false, () => s.md3StyleCommands = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleMenuSections", s.md3StyleMenuSections, false, () => s.md3StyleMenuSections = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleSchedule", s.md3StyleSchedule, false, () => s.md3StyleSchedule = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleInspectPane", s.md3StyleInspectPane, false, () => s.md3StyleInspectPane = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleStatistics", s.md3StyleStatistics, false, () => s.md3StyleStatistics = false));
            general.items.Add(Item("ModernExpandMenu_ResetMd3StyleIdeo", s.md3StyleIdeo, false, () => s.md3StyleIdeo = false));
            general.items.Add(Item("ModernExpandMenu_ResetSkipUploadWait", s.skipUploadWait, false, () => s.skipUploadWait = false));
            general.items.Add(Item("ModernExpandMenu_ResetSpaceOnlyPauses", s.spaceOnlyPauses, false, () => s.spaceOnlyPauses = false));
            general.items.Add(Item("ModernExpandMenu_ResetMaxMenuHeight", s.maxMenuHeight, 560, () => s.maxMenuHeight = 560));
            general.items.Add(Item("ModernExpandMenu_ResetMaxProcessedPerFrame", s.maxProcessedPerFrame, 6, () => s.maxProcessedPerFrame = 6));
            general.items.Add(Item("ModernExpandMenu_ResetExtraLoadingBarSeconds", s.extraLoadingBarSeconds.ToString("0.0"), "0.5", () => s.extraLoadingBarSeconds = 0.5f));
            sections.Add(general);

            // ── 动画 ──
            var animation = new ResetSection("ModernExpandMenu_ResetSectionAnimation".Translate());
            animation.items.Add(Item("ModernExpandMenu_ResetEnableAnimations", s.enableAnimations, true, () => s.enableAnimations = true));
            animation.items.Add(Item("ModernExpandMenu_ResetItemAppearDuration", s.itemAppearDuration.ToString("0.00"), "0.25", () => s.itemAppearDuration = 0.25f));
            animation.items.Add(Item("ModernExpandMenu_ResetItemAppearInterval", s.itemAppearInterval.ToString("0.00"), "0.03", () => s.itemAppearInterval = 0.03f));
            animation.items.Add(Item("ModernExpandMenu_ResetPopAnimationDuration", s.popAnimationDuration.ToString("0.00"), "0.18", () => s.popAnimationDuration = 0.18f));
            animation.items.Add(Item("ModernExpandMenu_ResetExpandAnimationSpeed", s.expandAnimationSpeed.ToString("0.0"), "10", () => s.expandAnimationSpeed = 10f));
            animation.items.Add(Item("ModernExpandMenu_ResetScrollFollowSpeed", s.scrollFollowSpeed.ToString("0"), "80", () => s.scrollFollowSpeed = 80f));
            animation.items.Add(Item("ModernExpandMenu_ResetScrollReturnDuration", s.scrollReturnDuration.ToString("0.00"), "0.60", () => s.scrollReturnDuration = 0.6f));
            animation.items.Add(Item("ModernExpandMenu_ResetWindowHeightAnimationSpeed", s.windowHeightAnimationSpeed.ToString("0"), "200", () => s.windowHeightAnimationSpeed = 200f));
            sections.Add(animation);

            // ── 颜色 ──
            var colors = new ResetSection("ModernExpandMenu_ResetSectionColors".Translate());
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorPrimary", s.colorPrimary, "#00A8FF", () => s.colorPrimary = "#00A8FF"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorOnPrimary", s.colorOnPrimary, "#001421", () => s.colorOnPrimary = "#001421"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorSurface", s.colorSurface, "#161821", () => s.colorSurface = "#161821"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorSurfaceContainer", s.colorSurfaceContainer, "#1E212D", () => s.colorSurfaceContainer = "#1E212D"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorSurfaceContainerHigh", s.colorSurfaceContainerHigh, "#262A3A", () => s.colorSurfaceContainerHigh = "#262A3A"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorOnSurface", s.colorOnSurface, "#E6E6EC", () => s.colorOnSurface = "#E6E6EC"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorOnSurfaceVariant", s.colorOnSurfaceVariant, "#9A9BA6", () => s.colorOnSurfaceVariant = "#9A9BA6"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorOutline", s.colorOutline, "#636676", () => s.colorOutline = "#636676"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorDisabledText", s.colorDisabledText, "#80808C", () => s.colorDisabledText = "#80808C"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorShadow", s.colorShadow, "#000000", () => s.colorShadow = "#000000"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorScrollbarTrack", s.colorScrollbarTrack, "#26262E", () => s.colorScrollbarTrack = "#26262E"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorScrollbarThumb", s.colorScrollbarThumb, "#525261", () => s.colorScrollbarThumb = "#525261"));
            colors.items.Add(AddColorItem("ModernExpandMenu_ColorScrollbarThumbDragging", s.colorScrollbarThumbDragging, "#737385", () => s.colorScrollbarThumbDragging = "#737385"));
            sections.Add(colors);

            // ── 杂项配色（全局 MD3 替换功能用，与扩展菜单配色分开）──
            var miscColors = new ResetSection("ModernExpandMenu_ResetSectionMiscColors".Translate());
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorPrimary", s.miscColorPrimary, "#00A8FF", () => s.miscColorPrimary = "#00A8FF"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorOnPrimary", s.miscColorOnPrimary, "#001421", () => s.miscColorOnPrimary = "#001421"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorSurface", s.miscColorSurface, "#161821", () => s.miscColorSurface = "#161821"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorSurfaceContainer", s.miscColorSurfaceContainer, "#1E212D", () => s.miscColorSurfaceContainer = "#1E212D"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorSurfaceContainerHigh", s.miscColorSurfaceContainerHigh, "#262A3A", () => s.miscColorSurfaceContainerHigh = "#262A3A"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorOnSurface", s.miscColorOnSurface, "#E6E6EC", () => s.miscColorOnSurface = "#E6E6EC"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorOnSurfaceVariant", s.miscColorOnSurfaceVariant, "#9A9BA6", () => s.miscColorOnSurfaceVariant = "#9A9BA6"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorOutline", s.miscColorOutline, "#636676", () => s.miscColorOutline = "#636676"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorDisabledText", s.miscColorDisabledText, "#80808C", () => s.miscColorDisabledText = "#80808C"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorShadow", s.miscColorShadow, "#000000", () => s.miscColorShadow = "#000000"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorScrollbarTrack", s.miscColorScrollbarTrack, "#26262E", () => s.miscColorScrollbarTrack = "#26262E"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorScrollbarThumb", s.miscColorScrollbarThumb, "#525261", () => s.miscColorScrollbarThumb = "#525261"));
            miscColors.items.Add(AddColorItem("ModernExpandMenu_ColorScrollbarThumbDragging", s.miscColorScrollbarThumbDragging, "#737385", () => s.miscColorScrollbarThumbDragging = "#737385"));
            sections.Add(miscColors);
        }

        private static ResetItem Item(string key, object current, object defaultValue, Action reset)
        {
            return new ResetItem(key.Translate(), current.ToString(), defaultValue.ToString(), reset);
        }

        private static ResetItem AddColorItem(string key, string current, string defaultValue, Action reset)
        {
            return new ResetItem(key.Translate(), current, defaultValue, reset);
        }

        public override void DoWindowContents(Rect inRect)
        {
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 标题
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 12f, inRect.width - 40f, 30f), "ModernExpandMenu_ResetTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 树状差异列表（滚动）：分组标题 + 缩进子项
            float listTop = inRect.y + 52f;
            float listHeight = inRect.height - 52f - 64f;
            float contentHeight = 0f;
            foreach (ResetSection section in sections)
            {
                contentHeight += SectionHeaderHeight;
                contentHeight += section.items.Count * RowHeight;
            }
            var viewRect = new Rect(0f, 0f, inRect.width - 30f, contentHeight + 8f);
            var listRect = new Rect(inRect.x + 14f, listTop, inRect.width - 28f, listHeight);
            MD3Widgets.MD3BeginScrollView(listRect, ref scrollPosition, viewRect);
            float y = 0f;
            foreach (ResetSection section in sections)
            {
                DrawSectionHeader(viewRect, y, section);
                y += SectionHeaderHeight;
                for (int i = 0; i < section.items.Count; i++)
                {
                    DrawItemRow(viewRect, y, section.items[i]);
                    y += RowHeight;
                }
            }
            MD3Widgets.MD3EndScrollView(listRect, ref scrollPosition, contentHeight + 8f, 2000, MD3Theme.CardCornerRadius);

            // 底部按钮：全选 / 反选 / 取消 / 确定
            float buttonY = inRect.yMax - 50f;
            var selectAllRect = new Rect(inRect.x + 20f, buttonY, 96f, 34f);
            var invertRect = new Rect(inRect.x + 124f, buttonY, 96f, 34f);
            var cancelRect = new Rect(inRect.xMax - 118f, buttonY, 96f, 34f);
            var applyRect = new Rect(inRect.xMax - 222f, buttonY, 96f, 34f);
            if (MD3Widgets.MD3Button(selectAllRect, "ModernExpandMenu_ResetSelectAll".Translate(), emphasized: false))
            {
                SetAll(selected: true);
            }
            if (MD3Widgets.MD3Button(invertRect, "ModernExpandMenu_ResetInvert".Translate(), emphasized: false))
            {
                InvertAll();
            }
            if (MD3Widgets.MD3Button(cancelRect, "Cancel".Translate(), emphasized: false))
            {
                Close();
            }
            if (MD3Widgets.MD3Button(applyRect, "ModernExpandMenu_ResetConfirm".Translate(), emphasized: true))
            {
                Apply();
            }
        }

        /// <summary>绘制树状分组标题行（主色高亮，不可勾选）。</summary>
        private void DrawSectionHeader(Rect viewRect, float y, ResetSection section)
        {
            var headerRect = new Rect(10f, y, viewRect.width - 20f, SectionHeaderHeight);
            // 左侧树状展开箭头符号（主色）
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, 22f, headerRect.height), "▾");
            Text.Anchor = TextAnchor.UpperLeft;
            // 分组标题
            GUI.color = MD3Theme.Primary;
            Widgets.Label(new Rect(headerRect.x + 22f, headerRect.y, headerRect.width - 22f, headerRect.height), section.title);
            Text.WordWrap = true;
            GUI.color = Color.white;
            // 分组下的分割线
            MD3Widgets.DrawRoundedRect(new Rect(headerRect.x + 22f, headerRect.yMax - 1f, headerRect.width - 22f, 1f), MD3Theme.SurfaceContainerHigh, 0.5f);
        }

        private void DrawItemRow(Rect viewRect, float y, ResetItem item)
        {
            // 子项缩进，体现树状层级
            var rowRect = new Rect(22f, y, viewRect.width - 32f, RowHeight);
            ResetItem currentItem = item;

            // 当前值与默认值是否有差异（无差异时灰显、开关禁用，避免无意义的重置项）
            bool hasDiff = currentItem.currentText != currentItem.defaultText;

            // 勾选框（安卓滑动开关，id 用行内唯一段；无差异时画灰色禁用开关，不响应点击）
            var boxRect = new Rect(rowRect.x, rowRect.y + 4f, 44f, 22f);
            if (hasDiff)
            {
                currentItem.selected = MD3Widgets.MD3ToggleSwitch(boxRect, currentItem.selected, GetSwitchIdForItem(currentItem));
            }
            else
            {
                currentItem.selected = false;
                // 灰色禁用开关：轨道（禁用色）+ 内缩表面 + 圆点靠左
                MD3Widgets.DrawRoundedRect(new Rect(boxRect.x, boxRect.y + 3f, 38f, 16f), MD3Theme.DisabledText, 8f);
                MD3Widgets.DrawRoundedRect(new Rect(boxRect.x + 1f, boxRect.y + 4f, 36f, 14f), MD3Theme.Surface, 7f);
                MD3Widgets.DrawRoundedRect(new Rect(boxRect.x + 1f, boxRect.y + 4f, 14f, 14f), MD3Theme.DisabledText, 7f);
            }

            // 名称（无差异灰色）
            GUI.color = hasDiff ? MD3Theme.OnSurface : MD3Theme.DisabledText;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x + 50f, rowRect.y, rowRect.width - 50f - 240f, rowRect.height), currentItem.label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // 当前 → 默认（差异对比；无差异灰色）
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.WordWrap = false;
            GUI.color = hasDiff ? MD3Theme.OnSurfaceVariant : MD3Theme.DisabledText;
            string diffText = currentItem.currentText + "  →  " + currentItem.defaultText;
            Widgets.Label(new Rect(rowRect.x + rowRect.width - 236f, rowRect.y, 236f, rowRect.height), diffText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
        }

        /// <summary>为重置项分配稳定的开关 id（1000 段，避免与设置界面开关冲突）。</summary>
        private int GetSwitchIdForItem(ResetItem item)
        {
            int index = 0;
            foreach (ResetSection section in sections)
            {
                int found = section.items.IndexOf(item);
                if (found >= 0)
                {
                    return 1000 + index + found;
                }
                index += section.items.Count;
            }
            return 1000 + index;
        }

        private void SetAll(bool selected)
        {
            foreach (ResetSection section in sections)
            {
                foreach (ResetItem item in section.items)
                {
                    item.selected = selected;
                }
            }
        }

        private void InvertAll()
        {
            foreach (ResetSection section in sections)
            {
                foreach (ResetItem item in section.items)
                {
                    item.selected = !item.selected;
                }
            }
        }

        private void Apply()
        {
            foreach (ResetSection section in sections)
            {
                foreach (ResetItem item in section.items)
                {
                    if (item.selected)
                    {
                        item.reset();
                    }
                }
            }
            ModernExpandMenuMod.Settings.Write();
            Close();
        }

        private class ResetItem
        {
            public readonly string label;
            public readonly string currentText;
            public readonly string defaultText;
            public readonly Action reset;
            public bool selected = true;

            public ResetItem(string label, string current, string defaultValue, Action reset)
            {
                this.label = label;
                currentText = current;
                defaultText = defaultValue;
                this.reset = reset;
            }
        }

        private class ResetSection
        {
            public readonly string title;
            public readonly List<ResetItem> items = new List<ResetItem>();

            public ResetSection(string title)
            {
                this.title = title;
            }
        }
    }
}
