using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace NiceResearchTabZhCN
{
    /// <summary>
    /// Nice Research Tab 硬编码字符串翻译核心（Keyed + Translator 维护模式）。
    ///
    /// 设计：
    /// - 翻译文本存放在 Languages/ChineseSimplified/Keyed/HardcodedZhCN.xml
    /// - 本类只维护「英文原文 → Keyed 键名」的稳定映射
    /// - Transpiler 把 ldstr 指令替换为 Translator.Translate(键名) 调用
    ///
    /// 后续维护：只改 Keyed XML 即可更新翻译，无需重新编译 DLL。
    /// </summary>
    public static class HardcodedStringReplacer
    {
        /// <summary>
        /// 稳定映射：原模组英文硬编码字符串 → 本汉化的 Keyed 键名。
        /// 字符串必须与反编译源码中的字面量逐字符一致（含大小写、标点）。
        /// </summary>
        public static readonly Dictionary<string, string> KeyForString = new Dictionary<string, string>
        {
            // ===== 性能预设（Settings.DrawPerformance） =====
            { "High", "NiceResearchTabZhCN.Performance.High" },
            { "Medium", "NiceResearchTabZhCN.Performance.Medium" },
            { "Low", "NiceResearchTabZhCN.Performance.Low" },
            { "Custom", "NiceResearchTabZhCN.Performance.Custom" },

            // ===== DEV 调试选项（DebugOptionsWidget 构造函数） =====
            { "Finish now", "NiceResearchTabZhCN.Debug.FinishNow" },
            { "Apply techprint", "NiceResearchTabZhCN.Debug.ApplyTechprint" },
            { "Unhide", "NiceResearchTabZhCN.Debug.Unhide" },

            // ===== 当前研究剩余时间（CurrentResearchWidget.Draw） =====
            { "Remaining time: N/A", "NiceResearchTabZhCN.CurrentResearch.RemainingTime" },
        };

        /// <summary>Translator.Translate(string) 的反射句柄。</summary>
        static readonly MethodInfo TranslateMethod =
            AccessTools.Method(typeof(Translator), nameof(Translator.Translate), new[] { typeof(string) });

        /// <summary>
        /// 通用 Transpiler：把命中映射表的 ldstr 替换为翻译。
        /// 值为键名时走 Translator.Translate；否则直接替换 operand。
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldstr
                    && code.operand is string str
                    && KeyForString.TryGetValue(str, out var replacement))
                {
                    if (replacement.StartsWith("NiceResearchTabZhCN."))
                    {
                        // ldstr "键名"; call Verse.Translator::Translate(string)
                        yield return new CodeInstruction(OpCodes.Ldstr, replacement);
                        yield return new CodeInstruction(OpCodes.Call, TranslateMethod);
                    }
                    else
                    {
                        // 直接替换字符串字面量
                        code.operand = replacement;
                        yield return code;
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
