using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace TailorMadeZhCN
{
    /// <summary>
    /// Transpiler 核心工具
    /// 遍历 IL 指令，将 ldstr（加载字符串常量）替换为中文字典中的对应翻译。
    /// </summary>
    internal static class StringReplacer
    {
        /// <summary>
        /// 通用的 Transpiler 方法。
        /// 替换目标方法中所有命中的 ldstr 指令的操作数。
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldstr && ins.operand is string text)
                {
                    if (Strings_Chinese.Map.TryGetValue(text, out var zh))
                    {
                        ins.operand = zh;
                    }
                }
                yield return ins;
            }
        }
    }
}
