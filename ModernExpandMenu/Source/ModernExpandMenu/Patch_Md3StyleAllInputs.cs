using HarmonyLib;
using ModernExpandMenu.UI;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 可选功能：把原版所有文本输入框统一改成 MD3 无边框样式。
    // 原版 Widgets.TextField（及 TextFieldNumeric 等所有重载）最终都经
    // Text.CurTextFieldStyle 绘制，patch 其 getter 即可全局替换外观，
    // 输入 / 焦点 / 粘贴 / 输入法等行为保持不变。由设置 md3StyleAllInputs 控制（默认关闭）。
    // ═══════════════════════════════════════════════════
    [HarmonyPatch(typeof(Text), "CurTextFieldStyle", MethodType.Getter)]
    public static class Patch_Md3StyleAllInputs
    {
        public static void Postfix(ref GUIStyle __result)
        {
            // 开关开启：把原版输入框样式换成 MD3 无边框透明样式（仅外观，输入行为不变）
            if (ModernExpandMenuMod.Settings.md3StyleAllInputs)
            {
                __result = MD3Widgets.ToMd3TextFieldStyle(__result);
            }
        }
    }
}
