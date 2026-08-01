using HarmonyLib;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 禁用 RimWorld 开发者模式的"伪本地化"装饰：
    // DevMode 开启时，翻译缺失的文本会被替换成带重音/装饰的字符
    // （a→à、b→þ、c→ç…，见 Verse.Translator.PseudoTranslated），
    // 用于提醒开发者哪些文本未翻译，但严重干扰阅读。
    // 此补丁让翻译缺失文本直接显示原文（默认不干预，由设置开关控制）。
    // ═══════════════════════════════════════════════════
    [HarmonyPatch]
    public static class Patch_DisablePseudoTranslation
    {
        // PseudoTranslated 为 private static 方法，Harmony 属性写法找不到，
        // 必须用 TargetMethod 精确定位
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Translator), "PseudoTranslated");
        }

        private static bool Prefix(ref string __result, string original)
        {
            // 默认不干预（设置中开关开启时才禁用伪翻译）
            if (!ModernExpandMenuMod.Settings.disableDevPseudoTranslation)
            {
                return true;
            }
            __result = original;   // 直接返回原文，跳过重音装饰
            return false;          // 跳过原方法
        }
    }
}
