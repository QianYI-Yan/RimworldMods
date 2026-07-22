using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TailorMade;
using Verse;

namespace TailorMadeZhCN
{
    /// <summary>
    /// 模组入口 —— 应用所有 Harmony 补丁
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class TailorMadeZhCNMod
    {
        static TailorMadeZhCNMod()
        {
            var harmony = new Harmony("tailormade.zhcn");
            harmony.PatchAll();

            // 手动补丁：Patch_PlaySettings_TailorToggle 是 internal 类，
            // 无法直接用 typeof() 引用，需要用 AccessTools 通过名称查找。
            var toggleType = AccessTools.TypeByName("TailorMade.Patch_PlaySettings_TailorToggle");
            if (toggleType != null)
            {
                var toggleMethod = AccessTools.Method(toggleType, "Postfix");
                if (toggleMethod != null)
                {
                    harmony.Patch(toggleMethod, transpiler: new HarmonyMethod(
                        AccessTools.Method(typeof(Patch_TailorToggleTip), "Transpiler")));
                }
            }

            Log.Message("[TailorMadeZhCN] 汉化补丁已加载。");
        }
    }

    // ═══════════════════════════════════════════════════════
    // Postfix 补丁 —— 用于返回值简单的方法
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// TailorMadeMod.SettingsCategory()
    /// 将返回值 "TailorMade" 替换为中文
    /// </summary>
    [HarmonyPatch(typeof(TailorMadeMod), "SettingsCategory")]
    internal static class Patch_SettingsCategory
    {
        static void Postfix(ref string __result)
        {
            if (Strings_Chinese.Map.TryGetValue(__result, out var zh))
                __result = zh;
        }
    }

    /// <summary>
    /// TailorMadeMod.MapTokenLabel(string)
    /// 将返回值 "Default"/"Off"/"Auto-resize"/"Force: Auto"/"Force: ..."/" (missing)" 替换为中文
    /// </summary>
    [HarmonyPatch(typeof(TailorMadeMod), "MapTokenLabel")]
    internal static class Patch_MapTokenLabel
    {
        static void Postfix(ref string __result)
        {
            if (__result != null && Strings_Chinese.Map.TryGetValue(__result, out var zh))
                __result = zh;
        }
    }

    // ═══════════════════════════════════════════════════════
    // Transpiler 补丁 —— 用于包含大量字符串常量的大方法
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// TailorMadeMod.DoSettingsWindowContents(Rect)
    /// 替换设置界面中的所有英文字符串
    /// </summary>
    [HarmonyPatch(typeof(TailorMadeMod), "DoSettingsWindowContents")]
    internal static class Patch_DoSettingsWindowContents
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Window_Tailor.DoWindowContents(Rect)
    /// 替换编辑窗口主方法中的字符串
    /// </summary>
    [HarmonyPatch(typeof(Window_Tailor), "DoWindowContents")]
    internal static class Patch_DoWindowContents
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Window_Tailor.DrawList(Rect, Pawn)
    /// 替换穿戴列表中的字符串
    /// </summary>
    [HarmonyPatch(typeof(Window_Tailor), "DrawList")]
    internal static class Patch_DrawList
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Window_Tailor.DrawRight(Rect, Pawn)
    /// 替换右侧编辑面板中的字符串
    /// </summary>
    [HarmonyPatch(typeof(Window_Tailor), "DrawRight")]
    internal static class Patch_DrawRight
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Window_Tailor.IconBtn(Rect, Texture2D, string)
    /// 替换图标按钮的 fallback 字符 "?"
    /// </summary>
    [HarmonyPatch(typeof(Window_Tailor), "IconBtn")]
    internal static class Patch_IconBtn
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Window_Tailor.IconToggle(Rect, Texture2D, bool, string)
    /// 替换图标切换按钮的 fallback 字符 "H"
    /// </summary>
    [HarmonyPatch(typeof(Window_Tailor), "IconToggle")]
    internal static class Patch_IconToggle
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Window_Tailor.BuildXml(Pawn, string)
    /// 替换 "Human" fallback 种族名
    /// </summary>
    [HarmonyPatch(typeof(Window_Tailor), "BuildXml")]
    internal static class Patch_BuildXml
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }

    /// <summary>
    /// Patch_PlaySettings_TailorToggle.Postfix(WidgetRow, bool)
    /// 替换工具栏按钮的提示文本。
    /// 通过 AccessTools 在 TailorMadeZhCNMod 静态构造器中手动注册。
    /// </summary>
    internal static class Patch_TailorToggleTip
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => StringReplacer.Transpile(instructions);
    }
}
