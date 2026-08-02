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

        // 设置界面：滑块数值编辑状态（点击数值框进入输入态）
        private static int editingSliderId = -1;
        private static string editingBuffer = "";
        private static Rect editingValueRect;

        public NotificationsOnWindowsNowMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<NotificationsOnWindowsNowSettings>();
            ModRootDirectory = content.RootDir;
            ApplyHarmonyPatches();
        }

        /// <summary>游戏内「选项 → Mod 设置」界面（MD3 风格：深色卡片 + 开关 + 滑块）。</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 点击编辑框外部时退出输入态（未回车视为取消）。
            if (editingSliderId >= 0 && Event.current.type == EventType.MouseDown && !Mouse.IsOver(editingValueRect))
            {
                editingSliderId = -1;
            }

            // 整窗 MD3 表面背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            float contentX = inRect.x + 18f;
            float contentWidth = inRect.width - 36f;
            float y = inRect.y + 16f;
            const float rowHeight = 30f;

            // ── 标题 ──
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(contentX, y, contentWidth, 30f), "NOW.SettingsCategory".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            y += 40f;

            // ── 卡片 1：短时间消息去重开关 ──
            float dedupHeight = 30f + rowHeight + 12f;
            var dedupCard = new Rect(contentX, y, contentWidth, dedupHeight);
            MD3Widgets.DrawCard(dedupCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(dedupCard, "NOW.EnableDedup".Translate());
            DrawCheckboxRow(dedupCard, dedupCard.y + 34f, "NOW.EnableDedupDesc".Translate(), ref Settings.enableShortTimeMessageDedup, 0);
            y = dedupCard.yMax + 12f;

            // ── 卡片 2：通知合并窗口滑块 ──
            float mergeHeight = 30f + rowHeight + 12f;
            var mergeCard = new Rect(contentX, y, contentWidth, mergeHeight);
            MD3Widgets.DrawCard(mergeCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawCardTitle(mergeCard, "NOW.MergeWindow".Translate());
            DrawSliderRow(mergeCard, mergeCard.y + 34f, "NOW.MergeWindow".Translate(), ref Settings.mergeWindowSeconds, 0f, 10f, 1, "0.0");
            y = mergeCard.yMax + 12f;

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

        /// <summary>绘制滑块设置行：标签（左）+ 滑块（中）+ 数值（右，可点击自由输入）。</summary>
        private static void DrawSliderRow(Rect card, float y, string label, ref float value, float min, float max, int sliderId, string valueFormat)
        {
            float rowX = card.x + 14f;
            float rowWidth = card.width - 28f;

            // 数值按钮（右端 64px，可点击编辑）
            var valueRect = new Rect(rowX + rowWidth - 64f, y, 64f, 30f);
            // 标签（左侧固定宽度）
            float labelWidth = Mathf.Min(170f, rowWidth * 0.45f);
            var labelRect = new Rect(rowX, y, labelWidth, 30f);
            // 滑块（标签与数值之间的弹性区域）
            var sliderRect = new Rect(labelRect.xMax + 8f, y, valueRect.x - labelRect.xMax - 16f, 30f);

            // 标签
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            // 数值区域：编辑态显示 MD3 数字输入框，否则显示可点击数值按钮
            if (editingSliderId == sliderId)
            {
                editingValueRect = valueRect;
                string controlName = "NOWNumericField" + sliderId;
                GUI.SetNextControlName(controlName);
                MD3Widgets.MD3NumberField(valueRect, ref value, ref editingBuffer, min, out bool submitted, out bool cancelled);
                if (submitted)
                {
                    value = Mathf.Max(min, value);
                    editingSliderId = -1;
                }
                else if (cancelled)
                {
                    editingSliderId = -1;
                }
            }
            else
            {
                MD3Widgets.DrawRoundedRect(valueRect, MD3Theme.SurfaceContainerHigh, 4f);
                if (Mouse.IsOver(valueRect))
                {
                    MD3Widgets.DrawHoverState(valueRect, 4f);
                }
                GUI.color = MD3Theme.Primary;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(valueRect, value.ToString(valueFormat) + "s");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(valueRect))
                {
                    editingSliderId = sliderId;
                    editingBuffer = value.ToString(valueFormat);
                    editingValueRect = valueRect;
                }
            }

            // 滑块（点击轨道跳转 / 按住拖动）
            value = MD3Widgets.MD3Slider(sliderRect, value, min, max, sliderId);
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
