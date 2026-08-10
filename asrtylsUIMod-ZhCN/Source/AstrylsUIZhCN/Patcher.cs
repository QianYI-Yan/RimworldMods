using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AstrylsUIZhCN
{
    /// <summary>
    /// 补丁入口：游戏启动时选择性应用补丁。
    /// 每个目标方法都做「模组存在性检查」——目标模组未安装 / 未激活、
    /// 类型或方法不存在时静默跳过，保证缺少任意一个 UI 模组都不报错。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.astrylUImod.zhcn.hardcoded");

            // ============ Modern Social Tab（无 Keyed，硬编码最多） ============
            const string socialTab = "astryl.ModernSocialTab";
            const string stMod = "ModernSocialTab.ModernSocialTabMod";
            const string stDrawer = "ModernSocialTab.SocialTabDrawer";
            const string stCache = "ModernSocialTab.SocialTabCache";
            const string stGraph = "ModernSocialTab.OpinionGraph";
            const string stFillTab = "ModernSocialTab.Patch_ITab_Pawn_Social_FillTab";

            // 设置界面
            PatchHelper.PatchIfPresent(harmony, socialTab, stMod, "DoSettingsWindowContents");
            // 标签页绘制
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "SortModeLabel");          // 属性 getter（Opinion/Relation）
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawRelationList");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawVanillaToggle");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawCertBarElem");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawGraphPane");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawThoughtsList");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawInteractionsList");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawBodyThoughtsList");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawSocialStatsStrip");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "BuildStatBlocksInto");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "DrawPawnCard");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "GetCardButtons");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "BuildCardButtons");
            PatchHelper.PatchIfPresent(harmony, socialTab, stDrawer, "WhereIs");
            // 其他类
            PatchHelper.PatchIfPresent(harmony, socialTab, stCache, "BuildRomanceTooltip");
            PatchHelper.PatchIfPresent(harmony, socialTab, stGraph, "Draw");
            PatchHelper.PatchIfPresent(harmony, socialTab, stFillTab, "Postfix");

            // ============ 独立模组 DLL（Learning/ColonistBar/Circinus/Faction/ModernCC）============
            // 硬编码翻译补丁已按模组拆分为独立 DLL（AstrylsUIZhCN.*.dll），由各自 Patcher 注册。

            // ============ 隐藏 Mod 设置列表中的聚合 UI 模组条目 ============
            // 原版「选项 → Mod 设置」只显示汉化模组一个条目，
            // 各 UI 模组设置改为从汉化模组的聚合界面进入。
            var dialogOptionsPostOpen = AccessTools.Method(typeof(Dialog_Options), "PostOpen");
            if (dialogOptionsPostOpen != null)
            {
                harmony.Patch(dialogOptionsPostOpen,
                    postfix: new HarmonyMethod(typeof(Patcher).GetMethod(
                        nameof(HideAggregatedModsFromSettings), BindingFlags.Static | BindingFlags.NonPublic)));
            }
        }

        /// <summary>
        /// Postfix：把「选项 → Mod 设置」列表中的 18 个聚合 UI 模组过滤掉。
        /// </summary>
        static void HideAggregatedModsFromSettings(Dialog_Options __instance)
        {
            var field = AccessTools.Field(typeof(Dialog_Options), "cachedModsWithSettings");
            if (field == null)
            {
                return;
            }
            var list = field.GetValue(__instance) as IEnumerable<Mod>;
            if (list == null)
            {
                return;
            }
            field.SetValue(__instance, list.Where(mod =>
                !AstrylsUIZhCNMod.IsAggregated(mod.Content?.PackageId)));
        }

    }
}
