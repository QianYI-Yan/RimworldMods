using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AstrylsUIZhCN
{
    /// <summary>
    /// 共享补丁注册助手：主框架 DLL 与各模组独立 DLL 共用。
    /// 目标模组未激活 / 类型不存在 / 方法不存在时静默跳过，保证缺模组不报错。
    /// 单个方法 patch 失败（如方法重载歧义、反射异常）只跳过该 patch，不拖垮整个 DLL。
    /// </summary>
    public static class PatchHelper
    {
        /// <summary>
        /// 目标类型与方法都存在时才给目标方法打 Transpiler 补丁。
        /// </summary>
        public static void PatchIfPresent(Harmony harmony, string modPackageId, string typeName, string methodName)
        {
            try
            {
                // 1. 模组未激活则跳过
                if (!ModsConfig.IsActive(modPackageId))
                {
                    return;
                }
                // 2. 类型不存在则跳过
                var targetType = AccessTools.TypeByName(typeName);
                if (targetType == null)
                {
                    return;
                }
                // 3. 构造函数 / 静态构造函数 / 方法 / 属性 getter 不存在则跳过
                MethodBase targetMethod = FindTargetMethod(targetType, methodName);
                if (targetMethod == null)
                {
                    return;
                }
                // 4. 应用 Transpiler（替换硬编码字符串）
                harmony.Patch(targetMethod, transpiler: new HarmonyMethod(
                    typeof(HardcodedStringReplacer).GetMethod(nameof(HardcodedStringReplacer.Transpiler),
                        BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception ex)
            {
                // 单点失败不拖垮整个 DLL：记录警告并跳过该补丁
                Log.Warning("[AstrylsUIZhCN] 跳过补丁 " + typeName + "." + methodName + "：" + ex.Message);
            }
        }

        /// <summary>
        /// 查找目标方法：构造函数 / 静态构造函数 / 方法 / 属性 getter。
        /// 注意：AccessTools.Method 遇多个同名重载会抛 AmbiguousMatchException，
        /// 降级为取第一个声明的同名方法（Transpiler 按字典匹配 ldstr，重载间互不影响）。
        /// </summary>
        private static MethodBase FindTargetMethod(Type targetType, string methodName)
        {
            if (methodName == ".ctor")
            {
                // 注意：不能用 AccessTools.Constructor(type, null, ...)——null 参数会被当作无参，
                // 有参构造函数（如 FloatMenuOptionSub(string, Func<...>)）会找不到。
                return targetType.GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
            }
            if (methodName == ".cctor")
            {
                return targetType.GetConstructor(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, System.Type.EmptyTypes, null);
            }
            try
            {
                return AccessTools.Method(targetType, methodName)
                    ?? AccessTools.PropertyGetter(targetType, methodName);
            }
            catch (AmbiguousMatchException)
            {
                var method = targetType.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == methodName);
                return method ?? AccessTools.PropertyGetter(targetType, methodName);
            }
        }
    }
}
