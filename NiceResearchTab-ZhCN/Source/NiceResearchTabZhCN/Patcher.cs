using System.Reflection;
using HarmonyLib;
using Verse;

namespace NiceResearchTabZhCN
{
    /// <summary>
    /// 补丁入口：游戏启动时对 Nice Research Tab 的硬编码字符串方法应用 Transpiler。
    /// 每个目标方法都做「模组存在性检查」——目标模组未安装 / 未激活、
    /// 类型或方法不存在时静默跳过，保证缺模组时不报错。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("yintx.deepseek.niceresearchtab.zhcn.hardcoded");

            // ============ Nice Research Tab ============
            const string modId = "Andromeda.NiceResearchTab";

            // 设置界面：性能预设单选按钮 High/Medium/Low/Custom
            PatchIfPresent(harmony, modId, "NiceResearchTab.Settings", "DrawPerformance");
            // DEV 调试选项：Finish now / Apply techprint / Unhide
            PatchIfPresent(harmony, modId, "NiceResearchTab.DebugOptionsWidget", ".ctor");
            // 当前研究剩余时间：Remaining time: N/A
            PatchIfPresent(harmony, modId, "NiceResearchTab.CurrentResearchWidget", "Draw");
        }

        /// <summary>
        /// 目标类型与方法都存在时才给目标方法打 Transpiler 补丁。
        /// 任一步骤不满足都静默跳过，保证缺模组时不报错。
        /// </summary>
        static void PatchIfPresent(Harmony harmony, string modPackageId, string typeName, string methodName)
        {
            // 1. 模组未激活则跳过
            if (!ModsConfig.IsActive(modPackageId))
            {
                return;
            }
            // 2. 类型不存在则跳过（防御：即使模组激活，类型名也可能变化）
            var targetType = AccessTools.TypeByName(typeName);
            if (targetType == null)
            {
                return;
            }
            // 3. 方法不存在则跳过（构造函数用 .ctor 标识）
            MethodBase targetMethod = methodName == ".ctor"
                ? AccessTools.Constructor(targetType)
                : AccessTools.Method(targetType, methodName);
            if (targetMethod == null)
            {
                return;
            }
            // 4. 应用 Transpiler（替换硬编码字符串）
            harmony.Patch(targetMethod, transpiler: new HarmonyMethod(
                typeof(HardcodedStringReplacer).GetMethod(nameof(HardcodedStringReplacer.Transpiler),
                    BindingFlags.Static | BindingFlags.Public)));
        }
    }
}
