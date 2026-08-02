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
            PatchIfPresent(harmony, socialTab, stMod, "DoSettingsWindowContents");
            // 标签页绘制
            PatchIfPresent(harmony, socialTab, stDrawer, "SortModeLabel");          // 属性 getter（Opinion/Relation）
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawRelationList");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawVanillaToggle");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawCertBarElem");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawGraphPane");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawThoughtsList");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawInteractionsList");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawBodyThoughtsList");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawSocialStatsStrip");
            PatchIfPresent(harmony, socialTab, stDrawer, "BuildStatBlocksInto");
            PatchIfPresent(harmony, socialTab, stDrawer, "DrawPawnCard");
            PatchIfPresent(harmony, socialTab, stDrawer, "GetCardButtons");
            PatchIfPresent(harmony, socialTab, stDrawer, "BuildCardButtons");
            PatchIfPresent(harmony, socialTab, stDrawer, "WhereIs");
            // 其他类
            PatchIfPresent(harmony, socialTab, stCache, "BuildRomanceTooltip");
            PatchIfPresent(harmony, socialTab, stGraph, "Draw");
            PatchIfPresent(harmony, socialTab, stFillTab, "Postfix");

            // ============ Modern Learning Menu（无 Keyed） ============
            const string learningMenu = "astryl.ModernLearningMenu";
            const string lmDrawer = "ModernLearningMenu.DashboardDrawer";
            const string lmMod = "ModernLearningMenu.ModernLearningMenuMod";

            // 面板标题/说明（属性 getter）
            PatchIfPresent(harmony, learningMenu, lmDrawer, "PanelTitle");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "PanelDescription");
            // 绘制与提示
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawTopBar");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Skills");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Children");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawChildCard");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Growth");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Education");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawStudyGroupCard");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Training");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Readout");
            PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Expertise");
            // 设置界面
            PatchIfPresent(harmony, learningMenu, lmMod, "DoSettingsWindowContents");

            // ============ Modern Colonist Bar（命令中心菜单、悬停按钮等硬编码） ============
            const string colonistBar = "astryl.ModernColonistBar";
            const string cbNS = "ModernColonistBar";
            PatchIfPresent(harmony, colonistBar, cbNS + ".BarControls", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".BarControls", "OpenMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".BarControls", "BuildViewMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "OpenInteractionMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildViewMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".FloatMenuOptionSub", ".ctor");
            PatchIfPresent(harmony, colonistBar, cbNS + ".HoverPopout", "BuildActions");
            PatchIfPresent(harmony, colonistBar, cbNS + ".BarSquads", "ViewLabel");
            PatchIfPresent(harmony, colonistBar, cbNS + ".WarbandHotbar", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "Label"); // 属性 getter
            // 状态显示 / 菜单 / 对话框
            PatchIfPresent(harmony, colonistBar, cbNS + ".AwayIndicator", "LocationTip");
            PatchIfPresent(harmony, colonistBar, cbNS + ".AggroRadar", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".PawnStatusUtil", "Gather");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_DrawColonist_Overlay", "DrawWeaponIcon");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_DrawColonist_Overlay", "DrawBpDevice");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_DrawColonist_Overlay", "MedicalTooltip");
            PatchIfPresent(harmony, colonistBar, cbNS + ".FollowCam", "OnGUI");
            PatchIfPresent(harmony, colonistBar, cbNS + ".FollowCam", "Toggle");
            PatchIfPresent(harmony, colonistBar, cbNS + ".FollowCam", "Stop");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_ManageSquads", "DoWindowContents");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_RenameSquad", "DoWindowContents");
            PatchIfPresent(harmony, colonistBar, cbNS + ".BarSquads", "NameOfHidden");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildModMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildPoliciesMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildBarManageMenu");
            // 指挥中心
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DoWindowContents");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawRoster");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawSquadHeader");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "SubLine");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawDetail");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawPillar");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DpsChip");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawActionBar");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "PolicyBtn");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "OpenAssignMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "OpenSortMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "OpenSquadMenu");
            // 装备模块
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawGearPanel");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawMapList");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "OpenMapRowMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "OrderGear");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawSlot");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawManage");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawReqRow");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "KitToOutfit");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "QueueBills");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "Snapshot");
            // 军械库
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Armory", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Armory", "OpenRowMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Armory", "WhereLabel");
            // 概览模块
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Overview", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Overview", "DrawRows");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Overview", "DrawGraph");
            // 统计模块
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Stats", "Draw");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Stats", "OpenColumnsMenu");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Stats", "BuildAddMenu");
            // 战斗统计
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "ReadinessTip");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "TechName");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "EffectiveDps");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "RangedEffDps");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "Resolve");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "RangedDpsTip");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "KeyLabel");
            PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "StatMenu");
            // 战斗条
            PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "DrawCell");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "DrawBelt");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "BeltLabel");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "UseBeltItem");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "DrawPips");
            PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "IsChampion");
            // 订单禁用原因
            PatchIfPresent(harmony, colonistBar, cbNS + ".CCOrders", "Blocker");

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
        /// Postfix：把「选项 → Mod 设置」列表中的 16 个聚合 UI 模组过滤掉。
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

        /// <summary>
        /// 目标类型与方法都存在时才给目标方法打 Transpiler 补丁。
        /// 任一步骤不满足都静默跳过，保证缺模组时不报错。
        /// </summary>
        static void PatchIfPresent(Harmony harmony, string modPackageId, string typeName, string methodName)
        {
            // 1. 模组未激活则跳过
            if (!ModsConfig.IsActive(modPackageId))
            {
                return;
            }
            // 2. 类型不存在则跳过
            var targetType = AccessTools.TypeByName(typeName);
            if (targetType == null)
            {
                return;
            }
            // 3. 构造函数 / 方法 / 属性 getter 不存在则跳过
            MethodBase targetMethod;
            if (methodName == ".ctor")
            {
                targetMethod = AccessTools.Constructor(targetType);
            }
            else
            {
                targetMethod = AccessTools.Method(targetType, methodName)
                    ?? AccessTools.PropertyGetter(targetType, methodName);
            }
            if (targetMethod == null)
            {
                return;
            }
            // 4. 应用 Transpiler（替换硬编码字符串）
            harmony.Patch(targetMethod, transpiler: new HarmonyMethod(
                typeof(HardcodedStringReplacer).GetMethod(nameof(HardcodedStringReplacer.Transpiler),
                    BindingFlags.Static | BindingFlags.Public)));
        }
    }
}
