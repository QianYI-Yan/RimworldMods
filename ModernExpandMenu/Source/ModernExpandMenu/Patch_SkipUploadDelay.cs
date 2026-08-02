using HarmonyLib;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 跳过创意工坊上传确认的 6 秒等待倒计时：
    // 原版上传确认框（Page_ModsConfig → Dialog_ConfirmModUpload →
    // Dialog_MessageBox.CreateConfirmation）设置 interactionDelay = 6f，
    // 按钮需等倒计时结束才能点击。开关开启时，任何设置了交互延迟的
    // Dialog_MessageBox 立即可交互（只影响带延迟的框，普通消息框不受影响）。
    // ═══════════════════════════════════════════════════
    [HarmonyPatch]
    public static class Patch_SkipUploadDelay
    {
        // InteractionDelayExpired 为 private 属性，需 TargetMethod 精确定位
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Dialog_MessageBox), "InteractionDelayExpired");
        }

        private static bool Prefix(Dialog_MessageBox __instance, ref bool __result)
        {
            if (ModernExpandMenuMod.Settings.skipUploadWait && __instance.interactionDelay > 0f)
            {
                __result = true;   // 跳过倒计时，立即可交互
                return false;
            }
            return true;
        }
    }
}
