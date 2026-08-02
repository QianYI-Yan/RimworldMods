using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ModernExpandMenu.Theme;
using ModernExpandMenu.UI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 可选功能：原版 UI 全局 MD3 风格化（第二批扩展）。
    // 每个 patch 都由独立开关控制（默认关闭），只替换外观、不改交互：
    //   窗口边框 / 命令按钮（征召/解散/攻击）/ 菜单区块列表行 / 管制栏（时间表）/
    //   信息卡内容（含栏重叠修复）/ 统计界面分组块 / 文化菜单（文化行 + 模因大方块）
    // ═══════════════════════════════════════════════════
    public static class Patch_Md3StyleMore
    {
        /// <summary>
        /// 窗口边框 MD3：patch Widgets.DrawWindowBackground，
        /// 所有原版窗口背景换成 MD3 圆角表面卡片 + 主色细描边（开关 md3StyleWindows）。
        /// 本模组自己的 Dialog 窗口用自绘背景（不调用此方法），不受影响。
        /// </summary>
        [HarmonyPatch(typeof(Widgets), "DrawWindowBackground", new Type[] { typeof(Rect) })]
        private static class Patch_WindowBackground
        {
            private static bool Prefix(Rect rect)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleWindows)
                {
                    return true;
                }
                MD3Widgets.DrawRoundedRect(rect, MiscTheme.Surface, 8f);
                MD3Widgets.DrawRoundedRectOutline(rect, MiscTheme.Outline, 8f, 1f, MiscTheme.Surface);
                return false;
            }
        }

        // MD3 命令按钮背景纹理（9-slice 圆角，随配色缓存重建）
        private static Texture2D md3CommandBgTexture;
        private static Color md3CommandBgCacheFill;
        private static Color md3CommandBgCacheBorder;

        private static Texture2D GetMd3CommandBgTexture()
        {
            Color fill = MiscTheme.SurfaceContainerHigh;
            Color border = MiscTheme.Outline;
            if (md3CommandBgTexture == null || md3CommandBgCacheFill != fill || md3CommandBgCacheBorder != border)
            {
                if (md3CommandBgTexture != null)
                {
                    UnityEngine.Object.Destroy(md3CommandBgTexture);
                }
                md3CommandBgTexture = CreateRoundRectTexture(fill, border, 1f);
                md3CommandBgCacheFill = fill;
                md3CommandBgCacheBorder = border;
            }
            return md3CommandBgTexture;
        }

        /// <summary>生成 16x16 圆角矩形纹理（9-slice 拉伸用）：边框色 + 内部填充色，四角透明。</summary>
        private static Texture2D CreateRoundRectTexture(Color fill, Color border, float borderThickness)
        {
            const int size = 16;
            const float radius = 5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 圆角遮罩 alpha（四角透明）
                    float alpha = 1f;
                    float dx = 0f;
                    float dy = 0f;
                    bool corner = true;
                    if (x < radius && y < radius) { dx = radius - x; dy = radius - y; }
                    else if (x >= size - radius && y < radius) { dx = x - (size - radius); dy = radius - y; }
                    else if (x < radius && y >= size - radius) { dx = radius - x; dy = y - (size - radius); }
                    else if (x >= size - radius && y >= size - radius) { dx = x - (size - radius); dy = y - (size - radius); }
                    else { corner = false; }
                    if (corner)
                    {
                        alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    }
                    // 边框：距边缘小于 borderThickness 时用边框色
                    float distToEdge = Mathf.Min(x, Mathf.Min(y, Mathf.Min(size - 1 - x, size - 1 - y)));
                    Color c = distToEdge < borderThickness ? border : fill;
                    c.a = alpha;
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        /// <summary>命令按钮 MD3：patch Command.BGTexture / BGTextureShrunk getter，
        /// 所有原版命令按钮（征召/解散/攻击/建造等底部按钮条）背景换成 MD3 圆角纹理（开关 md3StyleCommands）。</summary>
        [HarmonyPatch(typeof(Command), "get_BGTexture")]
        private static class Patch_CommandBGTexture
        {
            private static void Postfix(ref Texture2D __result)
            {
                if (ModernExpandMenuMod.Settings.md3StyleCommands)
                {
                    __result = GetMd3CommandBgTexture();
                }
            }
        }

        /// <summary>命令按钮（缩小版，分组小按钮）同 MD3 圆角背景。</summary>
        [HarmonyPatch(typeof(Command), "get_BGTextureShrunk")]
        private static class Patch_CommandBGTextureShrunk
        {
            private static void Postfix(ref Texture2D __result)
            {
                if (ModernExpandMenuMod.Settings.md3StyleCommands)
                {
                    __result = GetMd3CommandBgTexture();
                }
            }
        }

        /// <summary>
        /// 菜单区块 / 列表行 MD3：patch Widgets.DrawMenuSection，
        /// 大量原版界面（药物/食物限制列表、手术清单、文化列表、各种分组区块）的
        /// 区块背景统一换成 MD3 圆角卡片（开关 md3StyleMenuSections）。
        /// </summary>
        [HarmonyPatch(typeof(Widgets), "DrawMenuSection")]
        private static class Patch_MenuSection
        {
            private static bool Prefix(Rect rect)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleMenuSections)
                {
                    return true;
                }
                MD3Widgets.DrawRoundedRect(rect, MiscTheme.SurfaceContainer, 6f);
                MD3Widgets.DrawRoundedRectOutline(rect, MiscTheme.Outline, 6f, 1f, MiscTheme.SurfaceContainer);
                return false;
            }
        }

        /// <summary>
        /// 管制栏（时间表 tab 左上角管制按钮）MD3：patch TimeAssignmentSelector.DrawTimeAssignmentSelectorFor，
        /// 选中主色胶囊 + 白色文字，未选表面高色 + hover 高亮（开关 md3StyleSchedule）。
        /// </summary>
        [HarmonyPatch]
        private static class Patch_ScheduleSelector
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(TimeAssignmentSelector), "DrawTimeAssignmentSelectorFor");
            }

            private static bool Prefix(Rect rect, TimeAssignmentDef ta)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleSchedule)
                {
                    return true;
                }
                rect = rect.ContractedBy(2f);
                bool selected = TimeAssignmentSelector.selectedAssignment == ta;

                MD3Widgets.DrawRoundedRect(rect, selected ? MiscTheme.Primary : MiscTheme.SurfaceContainerHigh, 6f);
                if (!selected && Mouse.IsOver(rect))
                {
                    MD3Widgets.DrawHoverState(rect, 6f, MiscTheme.HoverStateLayer);
                }
                GUI.color = selected ? MiscTheme.OnPrimary : MiscTheme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(rect, ta.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;

                if (Widgets.ButtonInvisible(rect, true))
                {
                    TimeAssignmentSelector.selectedAssignment = ta;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                return false;
            }
        }

        /// <summary>
        /// 信息卡内容 MD3：patch InspectPaneFiller.DoPaneContentsFor，
        /// 在内容区先画 MD3 圆角卡片背景（状态条与文本画在背景上）（开关 md3StyleInspectPane）。
        /// </summary>
        [HarmonyPatch(typeof(InspectPaneFiller), "DoPaneContentsFor")]
        private static class Patch_InspectPaneContents
        {
            private static void Prefix(ISelectable sel, Rect rect)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleInspectPane)
                {
                    return;
                }
                MD3Widgets.DrawRoundedRect(rect, MiscTheme.SurfaceContainer, 8f);
            }
        }

        /// <summary>
        /// 信息卡栏重叠修复：patch InspectPaneUtility.InspectPaneOnGUI，
        /// 把内容区起始 y 从 26f 改为 52f（标题区高 50f，原先内容与标题底部重叠约 24f）。
        /// </summary>
        [HarmonyPatch(typeof(InspectPaneUtility), "InspectPaneOnGUI")]
        private static class Patch_InspectPaneOverlapFix
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && Mathf.Approximately(value, 26f))
                    {
                        instruction.operand = 52f;
                    }
                    yield return instruction;
                }
            }
        }

        /// <summary>
        /// 统计界面分组块美化：patch MainTabWindow_History.DoStatisticsPage，
        /// 把纯文本统计改成分组 MD3 卡片块（基础 / 财富 / 袭击 / 伤亡 / 结局）（开关 md3StyleStatistics）。
        /// </summary>
        [HarmonyPatch]
        private static class Patch_StatisticsPage
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(MainTabWindow_History), "DoStatisticsPage");
            }

            private static bool Prefix(MainTabWindow_History __instance, Rect rect)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleStatistics)
                {
                    return true;
                }
                rect.yMin += 17f;
                float x = rect.x;
                float width = rect.width;
                float y = rect.y;

                // 卡片 1：基础（游玩时间 / 说书人 / 难度）
                TimeSpan playTime = new TimeSpan(0, 0, (int)Find.GameInfo.RealPlayTimeInteracting);
                string playtime = "Playtime".Translate() + ": " + playTime.Days + "LetterDay".Translate() + " " + playTime.Hours + "LetterHour".Translate() + " " + playTime.Minutes + "LetterMinute".Translate() + " " + playTime.Seconds + "LetterSecond".Translate();
                y = DrawStatsCard(x, y, width, "StatsGroupBasics".Translate(),
                    playtime,
                    "Storyteller".Translate() + ": " + Find.Storyteller.def.LabelCap,
                    "Difficulty".Translate() + ": " + Find.Storyteller.difficultyDef.LabelCap);

                // 卡片 2：财富（仅当前地图）
                if (Find.CurrentMap != null)
                {
                    y = DrawStatsCard(x, y, width, "StatsGroupWealth".Translate(),
                        "ThisMapColonyWealthTotal".Translate() + ": " + Find.CurrentMap.wealthWatcher.WealthTotal.ToString("F0"),
                        "ThisMapColonyWealthItems".Translate() + ": " + Find.CurrentMap.wealthWatcher.WealthItems.ToString("F0"),
                        "ThisMapColonyWealthBuildings".Translate() + ": " + Find.CurrentMap.wealthWatcher.WealthBuildings.ToString("F0"),
                        "ThisMapColonyWealthColonistsAndTameAnimals".Translate() + ": " + Find.CurrentMap.wealthWatcher.WealthPawns.ToString("F0"));
                }

                // 卡片 3：袭击
                y = DrawStatsCard(x, y, width, "StatsGroupRaids".Translate(),
                    "NumThreatBigs".Translate() + ": " + Find.StoryWatcher.statsRecord.numThreatBigs,
                    "NumEnemyRaids".Translate() + ": " + Find.StoryWatcher.statsRecord.numRaidsEnemy);

                // 卡片 4：伤亡
                string damage = Find.CurrentMap != null ? "ThisMapDamageTaken".Translate() + ": " + Find.CurrentMap.damageWatcher.DamageTakenEver.ToString() : null;
                List<string> casualties = new List<string>();
                if (damage != null)
                {
                    casualties.Add(damage);
                }
                casualties.Add("ColonistsKilled".Translate() + ": " + Find.StoryWatcher.statsRecord.colonistsKilled);
                y = DrawStatsCard(x, y, width, "StatsGroupCasualties".Translate(), casualties.ToArray());

                // 卡片 5：结局
                DrawStatsCard(x, y, width, "StatsGroupEnding".Translate(),
                    "ColonistsLaunched".Translate() + ": " + Find.StoryWatcher.statsRecord.colonistsLaunched);
                return false;
            }

            /// <summary>绘制一个 MD3 统计分组卡片（标题 + 文本行），返回下一张卡片 y。</summary>
            private static float DrawStatsCard(float x, float y, float width, string title, params string[] lines)
            {
                const float rowHeight = 22f;
                float height = 36f + lines.Length * rowHeight + 8f;
                var card = new Rect(x, y, width, height);
                MD3Widgets.DrawCard(card, MiscTheme.SurfaceContainer, 6f);

                GUI.color = MiscTheme.Primary;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = false;
                Widgets.Label(new Rect(card.x + 12f, card.y + 8f, card.width - 24f, 20f), title);
                Text.WordWrap = true;
                GUI.color = Color.white;

                float ly = card.y + 32f;
                for (int i = 0; i < lines.Length; i++)
                {
                    GUI.color = MiscTheme.OnSurface;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = false;
                    Widgets.Label(new Rect(card.x + 16f, ly, card.width - 32f, rowHeight), lines[i]);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = true;
                    GUI.color = Color.white;
                    ly += rowHeight;
                }
                return card.yMax + 10f;
            }
        }

        /// <summary>
        /// 文化菜单 → 模因大方块 MD3：重写 IdeoUIUtility.DoMeme（Prefix 接管），
        /// 把原版「浅色高亮 + 直角边框」的模因卡片换成 MD3 圆角卡片
        /// （表面高色填充 + 选中主色描边 / 未选轮廓色 + hover 主色层）（开关 md3StyleIdeo）。
        /// </summary>
        [HarmonyPatch(typeof(IdeoUIUtility), "DoMeme")]
        private static class Patch_MemeCard
        {
            private static bool Prefix(Rect memeBox, MemeDef meme, Ideo ideo, IdeoEditMode editMode, bool drawHighlight, Action selectedOverride)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleIdeo)
                {
                    return true;
                }
                bool selected = ideo != null && ideo.memes.Contains(meme);

                // MD3 模因卡片：表面高色圆角 + 选中主色描边 / 未选轮廓色
                MD3Widgets.DrawRoundedRect(memeBox, MiscTheme.SurfaceContainerHigh, 8f);
                MD3Widgets.DrawRoundedRectOutline(memeBox, selected ? MiscTheme.Primary : MiscTheme.Outline, 8f, 1.5f, MiscTheme.SurfaceContainerHigh);
                if (Mouse.IsOver(memeBox))
                {
                    MD3Widgets.DrawHoverState(memeBox, 8f, MiscTheme.HoverStateLayer);
                    string tip = meme.LabelCap + "\n\n" + meme.description;
                    if (editMode != IdeoEditMode.None)
                    {
                        tip += "\n\n" + "ModernExpandMenu_ClickToEditHint".Translate();
                    }
                    TooltipHandler.TipRegion(memeBox, tip);
                }

                // 模因图标（居中）
                GUI.DrawTexture(new Rect(memeBox.x + (memeBox.width - 80f) / 2f, memeBox.y + 8f, 80f, 80f), meme.Icon);
                // 影响等级图标（右上角）
                if (meme.impact > 0)
                {
                    Rect impactRect = memeBox.RightPartPixels(18f).TopPartPixels(18f);
                    impactRect.x -= 4f;
                    impactRect.y += 4f;
                    IdeoImpactUtility.DrawImpactIcon(impactRect, meme.impact);
                }
                // 模因名称（底部居中）
                Rect labelRect = new Rect(memeBox.x, memeBox.yMax - Text.LineHeight * 2f + 4f, memeBox.width, Text.LineHeight * 2f - 4f).ContractedBy(10f, 0f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(labelRect, meme.LabelCap);
                GenUI.ResetLabelAlign();

                // 编辑模式点击（与原文一致：选中回调或打开模因选择对话框）
                if (editMode != IdeoEditMode.None && Widgets.ButtonInvisible(memeBox, true) && IdeoUIUtility.TutorAllowsInteraction(editMode))
                {
                    if (selectedOverride != null)
                    {
                        selectedOverride();
                        return false;
                    }
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EditingMemes, KnowledgeAmount.Total);
                    Find.WindowStack.Add(new Dialog_ChooseMemes(ideo, meme.category, false, null, null, false));
                }
                return false;
            }
        }

        /// <summary>
        /// 文化菜单 → 戒律块 MD3：patch Precept.DrawPreceptBox（Postfix），
        /// 在戒律卡片上叠加 MD3 圆角描边 + hover 主色层（开关 md3StyleIdeo）。
        /// </summary>
        [HarmonyPatch]
        private static class Patch_PreceptBox
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Precept), "DrawPreceptBox");
            }

            private static void Postfix(Rect preceptBox, bool forceHighlight)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleIdeo)
                {
                    return;
                }
                if (Mouse.IsOver(preceptBox) || forceHighlight)
                {
                    MD3Widgets.DrawHoverState(preceptBox, 6f, MiscTheme.HoverStateLayer);
                }
                MD3Widgets.DrawRoundedRectOutline(preceptBox, MiscTheme.Outline, 6f, 1f, new Color(0f, 0f, 0f, 0f));
            }
        }

        /// <summary>
        /// 文化菜单 → 文化列表行 MD3：patch IdeoUIUtility.DrawIdeoRow，
        /// 在文化行卡片上叠加 MD3 圆角描边（选中主色 / 未选轮廓色）（开关 md3StyleIdeo）。
        /// </summary>
        [HarmonyPatch]
        private static class Patch_IdeoRow
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(IdeoUIUtility), "DrawIdeoRow");
            }

            private static void Postfix(Rect fillRect, int row, ref bool mouseover, Ideo ideo)
            {
                if (!ModernExpandMenuMod.Settings.md3StyleIdeo)
                {
                    return;
                }
                // 文化行卡片：hover 时画浅主色层，再叠加 MD3 圆角描边（选中主色 / 未选轮廓色）
                if (mouseover)
                {
                    MD3Widgets.DrawHoverState(fillRect, 8f, MiscTheme.HoverStateLayer);
                }
                bool selected = IdeoUIUtility.selected == ideo;
                MD3Widgets.DrawRoundedRectOutline(fillRect, selected ? MiscTheme.Primary : MiscTheme.Outline, 8f, 1f, new Color(0f, 0f, 0f, 0f));
            }
        }
    }
}
