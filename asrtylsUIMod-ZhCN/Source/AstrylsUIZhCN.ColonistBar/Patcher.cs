using AstrylsUIZhCN;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN.ColonistBar
{
    /// <summary>
    /// Modern Colonist Bar 硬编码翻译补丁（独立 DLL，硬编码量最大的模组）。
    /// 共享字典与 Transpiler 来自主框架 AstrylsUIZhCN（PatchHelper / HardcodedStringReplacer）。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.astrylUImod.zhcn.hardcoded.colonistbar");
            const string colonistBar = "astryl.ModernColonistBar";
            const string cbNS = "ModernColonistBar";
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".BarControls", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".BarControls", "OpenMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".BarControls", "BuildViewMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "OpenInteractionMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildViewMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".FloatMenuOptionSub", ".ctor");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".HoverPopout", "BuildActions");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".BarSquads", "ViewLabel");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".WarbandHotbar", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "Label"); // 属性 getter
            // 状态显示 / 菜单 / 对话框
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".AwayIndicator", "LocationTip");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".AggroRadar", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".PawnStatusUtil", "Gather");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_DrawColonist_Overlay", "DrawWeaponIcon");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_DrawColonist_Overlay", "DrawBpDevice");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_DrawColonist_Overlay", "MedicalTooltip");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".FollowCam", "OnGUI");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".FollowCam", "Toggle");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".FollowCam", "Stop");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_ManageSquads", "DoWindowContents");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_RenameSquad", "DoWindowContents");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".BarSquads", "NameOfHidden");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildModMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildPoliciesMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Patch_HandleClicks_RightClickMenu", "BuildBarManageMenu");
            // 指挥中心
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DoWindowContents");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawRoster");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawSquadHeader");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "SubLine");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawDetail");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawPillar");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DpsChip");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "DrawActionBar");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "PolicyBtn");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "OpenAssignMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "OpenSortMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Dialog_CommandCenter", "OpenSquadMenu");
            // 装备模块
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawGearPanel");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawMapList");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "OpenMapRowMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "OrderGear");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawSlot");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawManage");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "DrawReqRow");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "KitToOutfit");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "QueueBills");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Loadouts", "Snapshot");
            // 军械库
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Armory", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Armory", "OpenRowMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Armory", "WhereLabel");
            // 概览模块
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Overview", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Overview", "DrawRows");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Overview", "DrawGraph");
            // 统计模块
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Stats", "Draw");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Stats", "OpenColumnsMenu");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Module_Stats", "BuildAddMenu");
            // 战斗统计
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "ReadinessTip");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "TechName");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "EffectiveDps");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "RangedEffDps");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "Resolve");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "RangedDpsTip");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "KeyLabel");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".MCBStats", "StatMenu");
            // 战斗条
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "DrawCell");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "DrawBelt");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "BeltLabel");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "UseBeltItem");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "DrawPips");
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".Warband", "IsChampion");
            // 订单禁用原因
            PatchHelper.PatchIfPresent(harmony, colonistBar, cbNS + ".CCOrders", "Blocker");
        }
    }
}
