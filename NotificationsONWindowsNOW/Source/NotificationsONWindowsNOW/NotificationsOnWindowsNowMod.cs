using System;
using HarmonyLib;
using NotificationsOnWindowsNow.Theme;
using NotificationsOnWindowsNow.UI;
using UnityEngine;
using Verse;

namespace NotificationsOnWindowsNow
{
    /// <summary>
    /// 模组主类：负责记录全局状态、应用 Harmony 补丁，并提供 MD3 风格设置页。
    /// </summary>
    public class NotificationsOnWindowsNowMod : Mod
    {
        /// <summary>当前模组实例，供全局访问。</summary>
        public static NotificationsOnWindowsNowMod Instance;

        /// <summary>模组设置实例。</summary>
        public static NotificationsOnWindowsNowSettings Settings;

        /// <summary>模组根目录（绝对路径），用于定位桥梁进程。</summary>
        public static string ModRootDirectory;

        /// <summary>设置页滚动位置。</summary>
        private static Vector2 settingsScrollPosition;

        public NotificationsOnWindowsNowMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<NotificationsOnWindowsNowSettings>();
            ModRootDirectory = content.RootDir;
            ApplyHarmonyPatches();
        }

        /// <summary>游戏内「选项 → Mod 设置」界面（MD3 风格：分组卡片 + 开关 + 合并档位，可滚动）。</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 整窗 MD3 表面背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            float contentX = inRect.x + 18f;
            float contentWidth = inRect.width - 36f;
            float y = inRect.y + 16f;
            const float rowHeight = 30f;

            // ── 标题（固定）──
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(contentX, y, contentWidth, 30f), "NOW.SettingsCategory".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            y += 38f;

            // ── 滚动内容区（卡片较多，MD3 细滚动条）──
            float cardWidth = contentWidth - 22f;
            var scrollRect = new Rect(contentX, y, contentWidth, inRect.yMax - y - 6f);
            var contentRect = new Rect(0f, 0f, cardWidth, 800f);
            MD3Widgets.MD3BeginScrollView(scrollRect, ref settingsScrollPosition, contentRect);

            float cy = 0f;

            // 卡片 1：总开关
            var masterCard = new Rect(0f, cy, cardWidth, 72f);
            MD3Widgets.DrawCard(masterCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(masterCard, "NOW.EnableAllPush".Translate());
            DrawCheckboxRow(masterCard, masterCard.y + 34f, "NOW.EnableAllPushExplain".Translate(), ref Settings.enableAllPush, 0);
            cy = masterCard.yMax + 12f;

            // 卡片 2：推送范围
            var scopeCard = new Rect(0f, cy, cardWidth, 102f);
            MD3Widgets.DrawCard(scopeCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(scopeCard, "NOW.PushScope".Translate());
            DrawCheckboxRow(scopeCard, scopeCard.y + 34f, "NOW.EnableLetterPush".Translate(), ref Settings.enableLetterPush, 1);
            DrawCheckboxRow(scopeCard, scopeCard.y + 64f, "NOW.EnableMessagePush".Translate(), ref Settings.enableMessagePush, 2);
            cy = scopeCard.yMax + 12f;

            // 卡片 3：信件类型细分
            var letterCard = new Rect(0f, cy, cardWidth, 162f);
            MD3Widgets.DrawCard(letterCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(letterCard, "NOW.LetterTypes".Translate());
            DrawCheckboxRow(letterCard, letterCard.y + 34f, "NOW.LetterTypeThreat".Translate(), ref Settings.letterPushThreat, 3);
            DrawCheckboxRow(letterCard, letterCard.y + 64f, "NOW.LetterTypeQuest".Translate(), ref Settings.letterPushQuest, 4);
            DrawCheckboxRow(letterCard, letterCard.y + 94f, "NOW.LetterTypeGrowth".Translate(), ref Settings.letterPushGrowth, 5);
            DrawCheckboxRow(letterCard, letterCard.y + 124f, "NOW.LetterTypeOther".Translate(), ref Settings.letterPushOther, 6);
            cy = letterCard.yMax + 12f;

            // 卡片 4：消息类型细分
            var messageCard = new Rect(0f, cy, cardWidth, 162f);
            MD3Widgets.DrawCard(messageCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(messageCard, "NOW.MessageTypes".Translate());
            DrawCheckboxRow(messageCard, messageCard.y + 34f, "NOW.MessageTypeThreat".Translate(), ref Settings.messagePushThreat, 7);
            DrawCheckboxRow(messageCard, messageCard.y + 64f, "NOW.MessageTypeNegative".Translate(), ref Settings.messagePushNegative, 8);
            DrawCheckboxRow(messageCard, messageCard.y + 94f, "NOW.MessageTypePositive".Translate(), ref Settings.messagePushPositive, 9);
            DrawCheckboxRow(messageCard, messageCard.y + 124f, "NOW.MessageTypeNeutral".Translate(), ref Settings.messagePushNeutral, 10);
            cy = messageCard.yMax + 12f;

            // 卡片 5：通知合并窗口档位（不合并 / 1 秒 / 2 秒 / 5 秒）
            var mergeCard = new Rect(0f, cy, cardWidth, 82f);
            MD3Widgets.DrawCard(mergeCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(mergeCard, "NOW.MergeWindow".Translate());
            DrawMergeSegmentRow(mergeCard, mergeCard.y + 34f);
            cy = mergeCard.yMax + 12f;

            // 卡片 6：短时间消息去重
            var dedupCard = new Rect(0f, cy, cardWidth, 72f);
            MD3Widgets.DrawCard(dedupCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(dedupCard, "NOW.EnableDedup".Translate());
            DrawCheckboxRow(dedupCard, dedupCard.y + 34f, "NOW.EnableDedupExplain".Translate(), ref Settings.enableShortTimeMessageDedup, 11);
            cy = dedupCard.yMax + 12f;

            // 恢复默认设置按钮
            var resetButtonRect = new Rect(0f, cy, cardWidth, 34f);
            if (MD3Widgets.MD3Button(resetButtonRect, "NOW.ResetButton".Translate(), emphasized: false))
            {
                Find.WindowStack.Add(new Dialog_ResetDefaults());
            }
            cy = resetButtonRect.yMax + 12f;

            MD3Widgets.MD3EndScrollView(scrollRect, ref settingsScrollPosition, cy, 99, 0f);

            Settings.Write();
        }

        /// <summary>绘制设置卡片标题（主色高亮）。</summary>
        private static void DrawCardTitle(Rect card, string title)
        {
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(card.x + 14f, card.y + 8f, card.width - 28f, 20f), title);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
        }

        /// <summary>绘制带右侧 MD3 开关的一行设置。</summary>
        private static void DrawCheckboxRow(Rect card, float y, string label, ref bool value, int switchId)
        {
            var rowRect = new Rect(card.x + 14f, y, card.width - 28f, 30f);
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - 50f, rowRect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            var switchRect = new Rect(rowRect.xMax - 44f, rowRect.y + (rowRect.height - 24f) / 2f, 44f, 24f);
            value = MD3Widgets.MD3ToggleSwitch(switchRect, value, switchId);
        }

        /// <summary>绘制合并窗口档位行：当前档位名 + 多段滑块（不合并/1秒/2秒/5秒）。</summary>
        private static void DrawMergeSegmentRow(Rect card, float y)
        {
            var rowRect = new Rect(card.x + 14f, y, card.width - 28f, 30f);

            // 当前档位名（左侧）
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, 90f, rowRect.height), MergeOptionLabel());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 多段滑块（4 档离散档位）
            var segRect = new Rect(rowRect.x + 98f, rowRect.y, rowRect.width - 98f, rowRect.height);
            float segValue = MD3Widgets.MD3SegmentSlider(segRect, Settings.mergeWindowOption, 4, 2);
            Settings.mergeWindowOption = Mathf.RoundToInt(Mathf.Clamp(segValue, 0f, 3f));
        }

        /// <summary>当前合并窗口档位的显示名。</summary>
        private static string MergeOptionLabel()
        {
            switch (Settings.mergeWindowOption)
            {
                case 0: return "NOW.MergeWindowOptionOff".Translate();
                case 1: return "NOW.MergeWindowOption1s".Translate();
                case 2: return "NOW.MergeWindowOption2s".Translate();
                case 3: return "NOW.MergeWindowOption5s".Translate();
                default: return "NOW.MergeWindowOption2s".Translate();
            }
        }

        /// <summary>设置页在选项列表中的分类名。</summary>
        public override string SettingsCategory()
        {
            return "NOW.SettingsCategory".Translate();
        }

        /// <summary>应用本程序集内所有 Harmony 补丁。</summary>
        private static void ApplyHarmonyPatches()
        {
            try
            {
                var harmony = new Harmony("yintx.deepseek.NotificationsONWindowsNOW");
                harmony.PatchAll();
            }
            catch (Exception exception)
            {
                Log.Error("[NotificationsONWindowsNOW] Harmony 补丁应用失败: " + exception);
            }
        }
    }
}
