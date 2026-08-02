using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace NiceResearchTabZhCN
{
    /// <summary>
    /// Nice Research Tab 硬编码字符串翻译核心（直接字面量替换模式）。
    ///
    /// 设计（2026-08-02：直接替换，与 asrtylsUIMod-ZhCN 一致，最可靠）：
    /// - 中文文本直接写在本 DLL 中，不调用 Translator（其返回 TaggedString，
    ///   与 RadioButton 等 string 参数类型不匹配，会导致 InvalidProgramException）
    /// - Transpiler 把原模组的 ldstr 英文直接替换为 ldstr 中文
    /// - 修改翻译需重新编译 DLL（牺牲热更新换取可靠性）
    /// </summary>
    public static class HardcodedStringReplacer
    {
        /// <summary>原模组英文硬编码字符串 → 简体中文（直接替换）。</summary>
        public static readonly Dictionary<string, string> KeyForString = new Dictionary<string, string>
        {
            // ===== 性能预设（Settings.DrawPerformance） =====
            { "High", "高" },
            { "Medium", "中" },
            { "Low", "低" },
            { "Custom", "自定义" },

            // ===== DEV 调试选项（DebugOptionsWidget 构造函数） =====
            { "Finish now", "立即完成" },
            { "Apply techprint", "应用科技印" },
            { "Unhide", "取消隐藏" },

            // ===== 当前研究剩余时间（CurrentResearchWidget.Draw） =====
            { "Remaining time: N/A", "剩余时间：N/A" },
        };

        /// <summary>
        /// Transpiler：把命中映射表的 ldstr 直接替换为中文。
        /// 直接修改 operand，保留原指令（含 labels / 分支目标），避免 IL 结构问题。
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldstr
                    && code.operand is string str
                    && KeyForString.TryGetValue(str, out var localized))
                {
                    code.operand = localized;
                }
                yield return code;
            }
        }
    }
}
