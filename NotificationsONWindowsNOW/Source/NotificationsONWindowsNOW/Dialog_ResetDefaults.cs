using System;
using System.Collections.Generic;
using NotificationsOnWindowsNow.Theme;
using NotificationsOnWindowsNow.UI;
using UnityEngine;
using Verse;

namespace NotificationsOnWindowsNow
{
    // ═══════════════════════════════════════════════════
    // 恢复默认设置确认对话框（MD3 风格）：
    //   分组列出各设置项（常规 / 信件类型 / 消息类型 / 合并窗口），
    //   显示当前值与默认值的差异，可勾选是否恢复，支持全选 / 反选，
    //   确定后仅恢复勾选项。
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

        public override Vector2 InitialSize => new Vector2(580f, 620f);

        private void BuildSections()
        {
            NotificationsOnWindowsNowSettings s = NotificationsOnWindowsNowMod.Settings;

            // ── 常规 ──
            var general = new ResetSection("NOW.ResetSectionGeneral".Translate());
            general.items.Add(Item("NOW.ResetAllPush", s.enableAllPush, true, () => s.enableAllPush = true));
            general.items.Add(Item("NOW.ResetLetterPush", s.enableLetterPush, true, () => s.enableLetterPush = true));
            general.items.Add(Item("NOW.ResetMessagePush", s.enableMessagePush, true, () => s.enableMessagePush = true));
            general.items.Add(Item("NOW.ResetDedup", s.enableShortTimeMessageDedup, false, () => s.enableShortTimeMessageDedup = false));
            sections.Add(general);

            // ── 信件类型 ──
            var letters = new ResetSection("NOW.ResetSectionLetterTypes".Translate());
            letters.items.Add(Item("NOW.ResetLetterThreat", s.letterPushThreat, true, () => s.letterPushThreat = true));
            letters.items.Add(Item("NOW.ResetLetterQuest", s.letterPushQuest, true, () => s.letterPushQuest = true));
            letters.items.Add(Item("NOW.ResetLetterGrowth", s.letterPushGrowth, true, () => s.letterPushGrowth = true));
            letters.items.Add(Item("NOW.ResetLetterOther", s.letterPushOther, true, () => s.letterPushOther = true));
            sections.Add(letters);

            // ── 消息类型 ──
            var messages = new ResetSection("NOW.ResetSectionMessageTypes".Translate());
            messages.items.Add(Item("NOW.ResetMessageThreat", s.messagePushThreat, true, () => s.messagePushThreat = true));
            messages.items.Add(Item("NOW.ResetMessageNegative", s.messagePushNegative, true, () => s.messagePushNegative = true));
            messages.items.Add(Item("NOW.ResetMessagePositive", s.messagePushPositive, true, () => s.messagePushPositive = true));
            messages.items.Add(Item("NOW.ResetMessageNeutral", s.messagePushNeutral, true, () => s.messagePushNeutral = true));
            sections.Add(messages);

            // ── 合并窗口 ──
            var merge = new ResetSection("NOW.ResetSectionMerge".Translate());
            merge.items.Add(Item("NOW.ResetMergeWindow", MergeOptionText(s.mergeWindowOption), "NOW.MergeWindowOption2s".Translate(), () => s.mergeWindowOption = 2));
            sections.Add(merge);
        }

        /// <summary>合并窗口档位的显示名。</summary>
        private static string MergeOptionText(int option)
        {
            switch (option)
            {
                case 0: return "NOW.MergeWindowOptionOff".Translate();
                case 1: return "NOW.MergeWindowOption1s".Translate();
                case 2: return "NOW.MergeWindowOption2s".Translate();
                case 3: return "NOW.MergeWindowOption5s".Translate();
                default: return option.ToString();
            }
        }

        private static ResetItem Item(string key, object current, object defaultValue, Action reset)
        {
            return new ResetItem(key.Translate(), current.ToString(), defaultValue.ToString(), reset);
        }

        public override void DoWindowContents(Rect inRect)
        {
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 标题
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 12f, inRect.width - 40f, 30f), "NOW.ResetTitle".Translate());
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
            if (MD3Widgets.MD3Button(selectAllRect, "NOW.ResetSelectAll".Translate(), emphasized: false))
            {
                SetAll(selected: true);
            }
            if (MD3Widgets.MD3Button(invertRect, "NOW.ResetInvert".Translate(), emphasized: false))
            {
                InvertAll();
            }
            if (MD3Widgets.MD3Button(cancelRect, "Cancel".Translate(), emphasized: false))
            {
                Close();
            }
            if (MD3Widgets.MD3Button(applyRect, "NOW.ResetConfirm".Translate(), emphasized: true))
            {
                Apply();
            }
        }

        /// <summary>绘制树状分组标题行（主色高亮，不可勾选）。</summary>
        private void DrawSectionHeader(Rect viewRect, float y, ResetSection section)
        {
            var headerRect = new Rect(10f, y, viewRect.width - 20f, SectionHeaderHeight);
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, 22f, headerRect.height), "▾");
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(headerRect.x + 22f, headerRect.y, headerRect.width - 22f, headerRect.height), section.title);
            Text.WordWrap = true;
            GUI.color = Color.white;
            MD3Widgets.DrawRoundedRect(new Rect(headerRect.x + 22f, headerRect.yMax - 1f, headerRect.width - 22f, 1f), MD3Theme.SurfaceContainerHigh, 0.5f);
        }

        /// <summary>绘制子项行：勾选开关 + 名称 + 当前→默认差异。</summary>
        private void DrawItemRow(Rect viewRect, float y, ResetItem item)
        {
            var rowRect = new Rect(22f, y, viewRect.width - 32f, RowHeight);
            bool hasDiff = item.currentText != item.defaultText;

            // 勾选开关（无差异时灰色禁用）
            var boxRect = new Rect(rowRect.x, rowRect.y + 4f, 44f, 22f);
            if (hasDiff)
            {
                item.selected = MD3Widgets.MD3ToggleSwitch(boxRect, item.selected, GetSwitchIdForItem(item));
            }
            else
            {
                item.selected = false;
                MD3Widgets.DrawRoundedRect(new Rect(boxRect.x, boxRect.y + 3f, 38f, 16f), MD3Theme.DisabledText, 8f);
                MD3Widgets.DrawRoundedRect(new Rect(boxRect.x + 1f, boxRect.y + 4f, 36f, 14f), MD3Theme.Surface, 7f);
                MD3Widgets.DrawRoundedRect(new Rect(boxRect.x + 1f, boxRect.y + 4f, 14f, 14f), MD3Theme.DisabledText, 7f);
            }

            // 名称
            GUI.color = hasDiff ? MD3Theme.OnSurface : MD3Theme.DisabledText;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x + 50f, rowRect.y, rowRect.width - 50f - 240f, rowRect.height), item.label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // 当前 → 默认
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.WordWrap = false;
            GUI.color = hasDiff ? MD3Theme.OnSurfaceVariant : MD3Theme.DisabledText;
            string diffText = item.currentText + "  →  " + item.defaultText;
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
            NotificationsOnWindowsNowMod.Settings.Write();
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
