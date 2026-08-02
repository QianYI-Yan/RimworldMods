using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace HardcodedZhCN
{
    /// <summary>
    /// 硬编码字符串翻译核心（推荐：Keyed + Translator 维护模式）。
    ///
    /// 设计（方便后续维护）：
    /// - 翻译文本存放在 RimWorld 标准语言文件 Languages/ChineseSimplified/Keyed/HardcodedZhCN.xml
    /// - 本类只维护「英文原文 → Keyed 键名」的稳定映射（只有原模组字符串变化时才需更新）
    /// - Transpiler 把 ldstr 指令替换为 Translator.Translate(键名) 调用，运行时查表
    ///
    /// 后续维护：只改 Keyed XML 即可更新翻译，无需重新编译 DLL。
    /// 缺失翻译键时 RimWorld 会回退显示键名（配合存在性检查，绝不报错）。
    ///
    /// 备选方案（简单直接）：把「英文→中文」直接写进下方 KeyForString 的值，
    /// Transpiler 改为替换 operand（旧模式，改翻译需重编译）。
    /// </summary>
    public static class HardcodedStringReplacer
    {
        /// <summary>
        /// 稳定映射：原模组英文硬编码字符串 → 本汉化的 Keyed 键名。
        /// 值可直接填中文（旧模式）或键名（推荐模式）。
        /// </summary>
        public static readonly Dictionary<string, string> KeyForString = new Dictionary<string, string>
        {
            // ===== 示例（替换为真实字符串） =====
            // 推荐：值 = Keyed 键名，翻译写在 Keyed/HardcodedZhCN.xml
            { "Appearance", "HardcodedZhCN.SocialTab.Appearance" },
            // 旧模式：值 = 中文，改翻译需重编译
            // { "Relation list", "关系列表" },
            // 含 {0} 占位符的字符串可整体翻译，占位符保留
            { "Targeted by {0} hostile", "HardcodedZhCN.ColonistBar.TargetedBy" },
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
                    if (replacement.StartsWith("HardcodedZhCN."))
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
