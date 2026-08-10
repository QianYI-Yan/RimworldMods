using System.Reflection;
using AstrylsUIZhCN;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN.Circinus
{
    /// <summary>
    /// Circinus 硬编码翻译补丁（独立 DLL）。
    /// 共享字典与 Transpiler 来自主框架 AstrylsUIZhCN（PatchHelper / HardcodedStringReplacer）。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.astrylUImod.zhcn.hardcoded.circinus");
            const string circinus = "astryl.Circinus";
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.CircinusView", "DrawHeader");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.CircinusView", "DrawSweepButton");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.CircinusView", "DrawRail");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.CircinusView", "Shorten");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.CircinusView", "DrawSourceBanner");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.CircinusView", ".cctor"); // TabLabels 导航标签静态数组（transpiler 可能不生效，下方另有 Prefix 保底）
            // Circinus 补充：RunRecorder / Tab_Runs / Tab_Perf / Warmup
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.Session.RunRecorder", "Start");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Runs", "DrawDetail");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Perf", "ProfilingUnavailable");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Window_Warmup", "DoWindowContents");
            // Circinus 补充：Tab_Live 热点 / Tab_Cohorts 成本卡片 / Tab_Mods / CohortMath / ProfilerContribution
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Live", "DrawHotspots");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Cohorts", "DrawCardHead");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Cohorts", "DrawCardBody");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Cohorts", "CopyFor");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.UI.Tab_Mods", "Draw");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.Contract.CohortMath", "Placement");
            PatchHelper.PatchIfPresent(harmony, circinus, "Circinus.Profiling.ProfilerContribution", "Describe");
            // Circinus 导航标签 TabLabels：Prefix 运行时把静态数组改成中文（.cctor transpiler 在部分环境不生效）
            var cvDraw = AccessTools.Method(AccessTools.TypeByName("Circinus.UI.CircinusView"), "Draw");
            if (cvDraw != null)
            {
                harmony.Patch(cvDraw, prefix: new HarmonyMethod(typeof(Patcher).GetMethod(
                    nameof(LocalizeCircinusTabs), BindingFlags.Static | BindingFlags.NonPublic)));
            }
        }

        /// <summary>
        /// Prefix：把 Circinus 导航标签数组（TabLabels）运行时改成中文。
        /// 用 Prefix 而非只依赖 .cctor transpiler（部分环境对静态构造函数的 transpiler 不生效）。
        /// </summary>
        static void LocalizeCircinusTabs()
        {
            var type = AccessTools.TypeByName("Circinus.UI.CircinusView");
            if (type == null)
            {
                return;
            }
            var field = AccessTools.Field(type, "TabLabels");
            if (field == null)
            {
                return;
            }
            var labels = field.GetValue(null) as string[];
            if (labels == null || labels.Length == 0 || labels[0] == "实时")
            {
                return;
            }
            string[] zh = { "实时", "分析器", "压力", "运行", "发现", "错误", "性能", "补丁", "模组", "队列", "测试" };
            for (int i = 0; i < labels.Length && i < zh.Length; i++)
            {
                labels[i] = zh[i];
            }
        }
    }
}
