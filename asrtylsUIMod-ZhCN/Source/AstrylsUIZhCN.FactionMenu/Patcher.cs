using AstrylsUIZhCN;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN.FactionMenu
{
    /// <summary>
    /// Modern Faction Menu 硬编码翻译补丁（独立 DLL）。
    /// 共享字典与 Transpiler 来自主框架 AstrylsUIZhCN（PatchHelper / HardcodedStringReplacer）。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.astrylUImod.zhcn.hardcoded.faction");
            const string factionMenu = "astryl.ModernFactionMenu";
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.CapitalPreview", "Draw");
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.Window_ModernFactions", "DrawTerritorySection");
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.Window_ModernFactions", "DrawEmpireSection");
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.Window_ModernFactions", "DrawTrendsSection");
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.Window_ModernFactions", "DrawOverviewFixed");
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.Window_ModernFactions", "GoodwillBreakdown");
            PatchHelper.PatchIfPresent(harmony, factionMenu, "ModernFactionMenu.Window_ModernEmpireSettlements", "DrawRow");
        }
    }
}
