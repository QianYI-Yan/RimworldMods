using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 设置窗口 MD3 化（仅针对本模组的 Dialog_ModSettings）：
    //   1) 覆盖底部"关闭"按钮为 MD3 风格（原版为棕色按钮）
    //   2) 关闭设置时若伪翻译开关被改动，强制刷新所有窗口 UI
    // ═══════════════════════════════════════════════════
    [HarmonyPatch]
    public static class Patch_WindowOnGUI_MD3
    {
        // 目标为 Window.WindowOnGUI（public virtual）——注意不是 OnGUI
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Window), "WindowOnGUI");
        }

        // 是否为当前正在绘制的"本模组设置窗口"
        private static bool isTargetWindow;

        private static void Prefix(Window __instance)
        {
            isTargetWindow = __instance is Dialog_ModSettings dms && GetTargetMod(dms) is ModernExpandMenuMod;
        }

        private static void Postfix(Window __instance)
        {
            if (!isTargetWindow)
            {
                return;
            }
            // 覆盖底部"关闭"按钮为 MD3 风格（只针对本模组设置窗口，不影响其他模组/窗口）
            // 内容区 = 窗口矩形收缩 margin（Window.Margin 为受保护属性，用 Traverse 读取）
            float margin = Traverse.Create(__instance).Property("Margin").GetValue<float>();
            Rect contentRect = __instance.windowRect.ContractedBy(margin);
            var closeRect = new Rect(
                contentRect.x + contentRect.width / 2f - Window.CloseButSize.x / 2f,
                contentRect.y + contentRect.height - 55f,
                Window.CloseButSize.x,
                Window.CloseButSize.y);
            if (UI.MD3Widgets.MD3Button(closeRect, "CloseButton".Translate(), emphasized: false))
            {
                __instance.Close();
            }
            isTargetWindow = false;
        }

        /// <summary>读取 Dialog_ModSettings 的私有 mod 字段。</summary>
        internal static Mod GetTargetMod(Dialog_ModSettings dialog)
        {
            return Traverse.Create(dialog).Field("mod").GetValue<Mod>();
        }
    }

    /// <summary>
    /// 关闭设置窗口时：若"禁用伪翻译"开关被改动，强制所有窗口重算布局刷新，
    /// 让伪翻译文本变化立刻反映到 UI。
    /// </summary>
    [HarmonyPatch(typeof(Dialog_ModSettings), "PreClose")]
    public static class Patch_ModSettingsPreClose
    {
        private static void Postfix(Dialog_ModSettings __instance)
        {
            if (Patch_WindowOnGUI_MD3.GetTargetMod(__instance) is not ModernExpandMenuMod)
            {
                return;
            }
            bool current = ModernExpandMenuMod.Settings.disableDevPseudoTranslation;
            if (current == ModernExpandMenuMod.lastPseudoTranslationSetting)
            {
                return;
            }
            ModernExpandMenuMod.lastPseudoTranslationSetting = current;
            // 伪翻译影响所有翻译文本显示，改动后强制刷新全部窗口
            foreach (Window window in Find.WindowStack.Windows)
            {
                window.Notify_ResolutionChanged();
            }
        }
    }
}
