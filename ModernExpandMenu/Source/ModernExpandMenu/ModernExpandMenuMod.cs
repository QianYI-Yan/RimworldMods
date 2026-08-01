using System;
using System.Reflection;
using HarmonyLib;
using ModernExpandMenu.Theme;
using ModernExpandMenu.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 模组入口 —— 安装 Harmony 补丁
    // 通过 PatchAll 自动注册本程序集内所有 HarmonyPatch
    // ═══════════════════════════════════════════════════
    public class ModernExpandMenuMod : Mod
    {
        // 全局设置实例（窗口 / 补丁各处读取）
        public static ModernExpandMenuSettings Settings;

        // 设置界面：当前正在编辑的数值滑块（点击数值框进入输入态）
        private static int editingSliderId = -1;
        private static string editingBuffer = "";
        private static Rect editingValueRect;

        // Harmony 唯一标识，与 About.xml 的 packageId 一致
        public const string HarmonyId = "yintx.deepseek.modernexpandmenu";

        public ModernExpandMenuMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ModernExpandMenuSettings>();
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>游戏内"选项 → Mod 设置"界面（MD3 风格：深色卡片分组 + 安卓滑动开关 + 单行紧凑滑块）。</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 点击编辑框外部时退出输入态（未回车视为取消）
            if (editingSliderId >= 0 && Event.current.type == EventType.MouseDown && !Mouse.IsOver(editingValueRect))
            {
                editingSliderId = -1;
            }

            // 整窗 MD3 表面背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            float contentX = inRect.x + 18f;
            float contentWidth = inRect.width - 36f;
            float y = inRect.y + 16f;

            // ===== 外观卡片 =====
            const float rowHeight = 30f;
            float appearanceHeight = 30f + rowHeight * 3f + 12f;
            var appearanceCard = new Rect(contentX, y, contentWidth, appearanceHeight);
            MD3Widgets.DrawCard(appearanceCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(appearanceCard, "ModernExpandMenu_SectionAppearance".Translate());
            float cy = appearanceCard.y + 34f;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowLoadingAnimation".Translate(), ref Settings.showLoadingAnimation, 0); cy += rowHeight;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowHoverHighlight".Translate(), ref Settings.showHoverHighlightAndArrow, 1); cy += rowHeight;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowItemCount".Translate(), ref Settings.showItemCount, 2); cy += rowHeight;
            y = appearanceCard.yMax + 12f;

            // ===== 性能卡片 =====
            float performanceHeight = 30f + rowHeight * 2f + 12f;
            var performanceCard = new Rect(contentX, y, contentWidth, performanceHeight);
            MD3Widgets.DrawCard(performanceCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(performanceCard, "ModernExpandMenu_SectionPerformance".Translate());
            cy = performanceCard.y + 34f;
            float maxHeightValue = Settings.maxMenuHeight;
            DrawSliderRow(performanceCard, cy, "ModernExpandMenu_MaxMenuHeight".Translate(), ref maxHeightValue, 300f, 9999f, 0, "0");
            Settings.maxMenuHeight = Mathf.RoundToInt(maxHeightValue);
            cy += rowHeight;
            float perFrameValue = Settings.maxProcessedPerFrame;
            DrawSliderRow(performanceCard, cy, "ModernExpandMenu_MaxProcessedPerFrame".Translate(), ref perFrameValue, 1f, 9999f, 1, "0");
            Settings.maxProcessedPerFrame = Mathf.RoundToInt(perFrameValue);
            cy += rowHeight;
            y = performanceCard.yMax + 12f;

            // ===== 加载动画卡片 =====
            float loadingHeight = 30f + rowHeight + 12f;
            var loadingCard = new Rect(contentX, y, contentWidth, loadingHeight);
            MD3Widgets.DrawCard(loadingCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(loadingCard, "ModernExpandMenu_SectionLoading".Translate());
            cy = loadingCard.y + 34f;
            float extraSecondsValue = Settings.extraLoadingBarSeconds;
            DrawSliderRow(loadingCard, cy, "ModernExpandMenu_ExtraLoadingBarSeconds".Translate(), ref extraSecondsValue, 0f, 60f, 2, "0.0");
            Settings.extraLoadingBarSeconds = extraSecondsValue;
            cy += rowHeight;
            y = loadingCard.yMax + 12f;

            // ===== 恢复默认设置 =====
            var resetRect = new Rect(contentX, y, contentWidth, 34f);
            if (MD3Widgets.MD3Button(resetRect, "ModernExpandMenu_ResetDefaults".Translate(), emphasized: false))
            {
                Settings.showLoadingAnimation = true;
                Settings.showHoverHighlightAndArrow = true;
                Settings.showItemCount = true;
                Settings.maxMenuHeight = 560;
                Settings.maxProcessedPerFrame = 6;
                Settings.extraLoadingBarSeconds = 0.5f;
                Settings.Write();
            }
            y = resetRect.yMax + 12f;

            Settings.Write();
        }

        /// <summary>绘制设置分组卡片标题（主色高亮）。</summary>
        private static void DrawSettingsTitle(Rect card, string title)
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

        /// <summary>绘制带右侧安卓滑动开关的一行设置。</summary>
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

        /// <summary>
        /// 绘制单行紧凑滑块设置：标签（左）+ 滑块（中）+ 数值按钮（右，可点击自由输入）。
        /// 数值输入提交时自动限制在安全范围（非负 / 不低于下限，防止产生 bug 的取值）。
        /// </summary>
        private static void DrawSliderRow(Rect card, float y, string label, ref float value, float min, float max, int sliderId, string valueFormat)
        {
            float rowHeight = 30f;
            float rowX = card.x + 14f;
            float rowWidth = card.width - 28f;

            // 数值按钮（右端 64px，可点击编辑）
            var valueRect = new Rect(rowX + rowWidth - 64f, y, 64f, rowHeight);
            // 标签（左侧固定宽度，过长截断）
            float labelWidth = Mathf.Min(160f, rowWidth * 0.42f);
            var labelRect = new Rect(rowX, y, labelWidth, rowHeight);
            // 滑块（标签与数值之间的弹性区域）
            var sliderRect = new Rect(labelRect.xMax + 8f, y, valueRect.x - labelRect.xMax - 16f, rowHeight);

            // 标签
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            // 数值区域：编辑态显示自绘 MD3 数字输入框（安卓 15 风格边框），否则显示可点击数值按钮
            if (editingSliderId == sliderId)
            {
                editingValueRect = valueRect;
                string edited = MD3Widgets.MD3NumberField(valueRect, editingBuffer, focused: true, out bool submitted, out bool cancelled);
                editingBuffer = edited;
                if (submitted)
                {
                    float parsed;
                    if (float.TryParse(editingBuffer.Replace(',', '.'), out parsed))
                    {
                        // 无上限自由输入，仅保下限（防止负数 / 0 等严重 bug 取值）
                        value = Mathf.Max(min, parsed);
                    }
                    editingSliderId = -1;
                }
                else if (cancelled)
                {
                    editingSliderId = -1;   // 取消编辑
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
                Widgets.Label(valueRect, value.ToString(valueFormat));
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

        public override string SettingsCategory()
        {
            return "Modern Expand Menu";
        }
    }
}
