using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AstrylsUIZhCN;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN.ModernCC
{
    /// <summary>
    /// Modern CC 硬编码翻译补丁（独立 DLL）。
    /// 体型按钮：DrawUpper 用 GenText.CapitalizeFirst(bodyTypeDef.defName) 渲染按钮文本，
    /// defName 不可翻译，用 Transpiler 把该方法内的 CapitalizeFirst 调用换成主框架的
    /// HardcodedStringReplacer.BodyTypeDisplayName（defName → 中文）。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.astrylUImod.zhcn.hardcoded.moderncc");
            const string modernCC = "astryl.ModernCC";

            // 体型按钮：仅在 Modern CC 激活时打补丁
            if (ModsConfig.IsActive(modernCC))
            {
                var bodyTypeDraw = AccessTools.Method(AccessTools.TypeByName("MCE.Module_Appearance"), "DrawUpper");
                if (bodyTypeDraw != null)
                {
                    harmony.Patch(bodyTypeDraw, transpiler: new HarmonyMethod(typeof(Patcher).GetMethod(
                        nameof(ReplaceBodyTypeNameWithChinese), BindingFlags.Static | BindingFlags.NonPublic)));
                }
            }
        }

        /// <summary>
        /// Transpiler：Modern CC 体型按钮——把 GenText.CapitalizeFirst(bodyTypeDef.defName)
        /// 替换为 HardcodedStringReplacer.BodyTypeDisplayName(defName)，实现 defName → 中文。
        /// 仅在目标方法内替换该调用点，不影响全局。
        /// </summary>
        static IEnumerable<CodeInstruction> ReplaceBodyTypeNameWithChinese(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var capitalize = AccessTools.Method(typeof(GenText), "CapitalizeFirst", new[] { typeof(string) });
            var replacement = AccessTools.Method(typeof(HardcodedStringReplacer), nameof(HardcodedStringReplacer.BodyTypeDisplayName));
            if (capitalize == null || replacement == null)
            {
                return code;
            }
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Call && code[i].operand is MethodInfo mi && mi == capitalize)
                {
                    code[i].operand = replacement;
                }
            }
            return code;
        }
    }
}
