using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 可选功能：强制显示游戏内"上传 / 更新到创意工坊"按钮并可强制上传更新。
    // 原版上传按钮藏在模组行「More Actions」右键菜单里，且需同时满足：
    //   开发者模式（Prefs.DevMode）+ Steam 已初始化 + mod.CanToUploadToWorkshop()
    // 其中 CanToUploadToWorkshop 会校验"工坊作者是当前用户"（MayHaveAuthorNotCurrentUser），
    // 本地 Mods 目录副本未订阅时会返回 false → 按钮消失（"更新选项消失"的根因）。
    // 本补丁（由设置 forceWorkshopUpload 控制，默认关）：
    //   ① CanToUploadToWorkshop 强制返回 true（跳过作者校验，未订阅也能上传更新）；
    //   ② Page_ModsConfig.DoModInfo 上传按钮不再要求开发者模式。
    // 点击后走原版 Workshop.Upload：有 PublishedFileId 则更新，无则新建，交互不变。
    // ═══════════════════════════════════════════════════
    public static class Patch_ForceWorkshopUpload
    {
        /// <summary>运行时标志：DoModInfo 每帧按设置刷新，Transpiler 用它替换 Prefs.DevMode（避免全局改 DevMode）。</summary>
        private static bool forceDevMode;

        /// <summary>CanToUploadToWorkshop 强制允许：本地 Mods 目录的非官方模组即可上传/更新（跳过作者校验）。</summary>
        [HarmonyPatch(typeof(ModMetaData), "CanToUploadToWorkshop")]
        private static class Patch_CanUpload
        {
            private static void Postfix(ref bool __result)
            {
                if (ModernExpandMenuMod.Settings.forceWorkshopUpload)
                {
                    __result = true;
                }
            }
        }

        /// <summary>上传按钮不再要求开发者模式：把 DoModInfo 里唯一的 Prefs.DevMode 读取替换为运行时标志。</summary>
        [HarmonyPatch(typeof(Page_ModsConfig), "DoModInfo")]
        private static class Patch_UploadButtonDevMode
        {
            private static void Prefix()
            {
                forceDevMode = ModernExpandMenuMod.Settings.forceWorkshopUpload;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo forceDevModeField = typeof(Patch_ForceWorkshopUpload).GetField(
                    nameof(forceDevMode), BindingFlags.Static | BindingFlags.NonPublic);
                foreach (CodeInstruction instruction in instructions)
                {
                    // 把 Prefs.DevMode 的 getter 调用替换为读取运行时标志 forceDevMode
                    // 注意：必须把原指令附着的 labels 转移到新指令上，否则跳转分支会引用不存在的 label（模组加载失败）
                    if (instruction.opcode == OpCodes.Call
                        && instruction.operand is MethodInfo method
                        && method.Name == "get_DevMode"
                        && method.DeclaringType == typeof(Prefs))
                    {
                        var replacement = new CodeInstruction(OpCodes.Ldsfld, forceDevModeField);
                        replacement.labels.AddRange(instruction.labels);
                        yield return replacement;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }
    }
}
