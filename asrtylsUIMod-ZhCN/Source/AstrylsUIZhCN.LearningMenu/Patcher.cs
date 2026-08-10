using AstrylsUIZhCN;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN.LearningMenu
{
    /// <summary>
    /// Modern Learning Menu 硬编码翻译补丁（独立 DLL）。
    /// 共享字典与 Transpiler 来自主框架 AstrylsUIZhCN（PatchHelper / HardcodedStringReplacer）。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.astrylUImod.zhcn.hardcoded.learning");
            const string learningMenu = "astryl.ModernLearningMenu";
            const string lmDrawer = "ModernLearningMenu.DashboardDrawer";
            const string lmMod = "ModernLearningMenu.ModernLearningMenuMod";

            // 面板标题/说明（属性 getter）
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "PanelTitle");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "PanelDescription");
            // 绘制与提示
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawTopBar");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Skills");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Children");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawChildCard");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Growth");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Education");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawStudyGroupCard");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Training");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Readout");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawPanel_Expertise");
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmDrawer, "DrawModIconBadge"); // VSE 等模组缺失/已激活提示 tooltip
            // 设置界面
            PatchHelper.PatchIfPresent(harmony, learningMenu, lmMod, "DoSettingsWindowContents");
        }
    }
}
