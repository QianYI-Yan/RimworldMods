using HarmonyLib;
using RimWorld;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 空格键仅暂停、不解除暂停：
    // 开关开启时，若当前已暂停且本次 TogglePaused 是"空格键触发"（TogglePause 绑定键按下），
    // 则不执行切换（保持暂停）；其他解除方式（暂停按钮 / 菜单继续）不受影响。
    // ═══════════════════════════════════════════════════
    [HarmonyPatch]
    public static class Patch_SpaceOnlyPause
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            // 1.6 方法名为 TogglePaused（TogglePause 已不存在）
            return AccessTools.Method(typeof(TickManager), "TogglePaused");
        }

        private static bool Prefix(TickManager __instance)
        {
            if (!ModernExpandMenuMod.Settings.spaceOnlyPauses)
            {
                return true;
            }
            // 已暂停且此刻正由空格键（TogglePause 绑定键）触发：不解除暂停
            if (__instance.Paused && KeyBindingDefOf.TogglePause.KeyDownEvent)
            {
                return false;
            }
            return true;
        }
    }
}
