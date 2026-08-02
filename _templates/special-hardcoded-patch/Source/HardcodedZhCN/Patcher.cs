using System.Reflection;
using HarmonyLib;
using Verse;

namespace HardcodedZhCN
{
    /// <summary>
    /// 补丁入口：游戏启动时选择性应用补丁。
    ///
    /// ⚠️ 聚合汉化多个模组时，必须做「模组存在性检查」：
    /// 目标模组未安装 / 未激活时跳过对应补丁，
    /// 否则 Harmony 会因目标类型不存在而抛异常，导致汉化模组整体报错。
    /// 使用方案一（手动注册）可完全避免该问题，也便于为不同模组分别配置。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("作者名.模组id.zhcn.hardcoded");

            // ===== 方案一（推荐）：逐目标注册，每个都做存在性检查 =====
            // 参数：模组 packageId、目标类型全名、目标方法名
            // PatchIfPresent(harmony, "目标模组packageId", "目标命名空间.目标类", "目标方法名");
            // PatchIfPresent(harmony, "astryl.ModernSocialTab", "ModernSocialTab.SocialTabDrawer", "FillTab");
            // PatchIfPresent(harmony, "astryl.ModernLearningMenu", "ModernLearningMenu.DashboardDrawer", "DrawDashboard");
            // 可对同一类多个方法重复调用，复用同一个 Transpiler。

            // ===== 方案二（属性驱动）：PatchAll 前用 ModsConfig 判断 =====
            // if (ModsConfig.IsActive("目标模组packageId"))
            // {
            //     harmony.PatchAll();
            // }
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
            // 3. 方法不存在则跳过
            var targetMethod = AccessTools.Method(targetType, methodName);
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
