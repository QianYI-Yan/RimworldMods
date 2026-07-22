using System;
using System.Xml;
using HarmonyLib;
using RimWorld;
using TailorMade;
using Verse;

namespace TailorMadeUnlockFix
{
    // ═══════════════════════════════════════════════════════
    //  Mod 子类 —— 构造函数在 XML Patch 之前执行
    //  在这里安装 Harmony 补丁，拦截 TailorMade 的破坏性操作
    // ═══════════════════════════════════════════════════════
    public class TailorMadeUnlockFixMod : Mod
    {
        public TailorMadeUnlockFixMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("yintx.deepseek.astryl.tailormade.unlockfix");

            var remove = AccessTools.Method(typeof(PatchOperationRemove), "ApplyWorker");
            if (remove != null)
                harmony.Patch(remove, prefix: new HarmonyMethod(typeof(PatchBlocker), nameof(PatchBlocker.PrefixRemove)));

            var replace = AccessTools.Method(typeof(PatchOperationReplace), "ApplyWorker");
            if (replace != null)
                harmony.Patch(replace, prefix: new HarmonyMethod(typeof(PatchBlocker), nameof(PatchBlocker.PrefixReplace)));
        }
    }

    // ═══════════════════════════════════════════════════════
    //  XML 补丁拦截器 —— 只拦截 TailorMade 的补丁
    // ═══════════════════════════════════════════════════════
    public static class PatchBlocker
    {
        private static bool IsCalledByTailorMade()
        {
            var stack = new System.Diagnostics.StackTrace(false);
            for (int i = 2; i < stack.FrameCount; i++)
            {
                var asm = stack.GetFrame(i).GetMethod()?.DeclaringType?.Assembly;
                if (asm == null) continue;
                try
                {
                    var name = asm.GetName().Name;
                    if (name.IndexOf("TailorMade", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                catch { }
            }
            return false;
        }

        public static bool PrefixRemove(PatchOperationRemove __instance, ref bool __result, XmlDocument xml)
        {
            if (!IsCalledByTailorMade()) return true;

            var xpath = Traverse.Create(__instance).Field("xpath").GetValue<string>();
            if (!string.IsNullOrEmpty(xpath) && xpath.Contains("apparelList/li"))
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static bool PrefixReplace(PatchOperationReplace __instance, ref bool __result, XmlDocument xml)
        {
            if (!IsCalledByTailorMade()) return true;

            var xpath = Traverse.Create(__instance).Field("xpath").GetValue<string>();
            if (!string.IsNullOrEmpty(xpath) && xpath.Contains("onlyUseRaceRestrictedApparel"))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  运行时 —— 控制 unlockRestrictedApparel 开关
    // ═══════════════════════════════════════════════════════
    [StaticConstructorOnStartup]
    public static class FixRuntime
    {
        static FixRuntime()
        {
            var harmony = new Harmony("yintx.deepseek.astryl.tailormade.unlockfix.runtime");

            var rsType = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
            if (rsType != null)
            {
                var canWear = AccessTools.Method(rsType, "CanWear",
                    new[] { typeof(ThingDef), typeof(ThingDef) });
                if (canWear != null)
                    harmony.Patch(canWear, postfix: new HarmonyMethod(typeof(FixRuntime), nameof(CanWearPostfix)));
            }

            var canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) });
            if (canEquip != null)
            {
                harmony.Patch(canEquip, postfix: new HarmonyMethod(typeof(FixRuntime), nameof(CanEquipPostfix))
                { priority = Priority.Last });
            }

            Log.Message("[TailorMadeUnlockFix] Fix active. HAR restriction data preserved.");
        }

        public static void CanWearPostfix(ThingDef apparel, ThingDef race, ref bool __result)
        {
            var settings = TailorMadeMod.Settings;
            if (settings == null || !settings.enabled) return;
            if (race == null || apparel == null) return;

            if (settings.unlockRestrictedApparel && !__result)
                __result = true;
            // OFF: HAR's original restriction stands
        }

        public static void CanEquipPostfix(Thing thing, Pawn pawn, ref bool __result, ref string cantReason)
        {
            var settings = TailorMadeMod.Settings;
            if (settings == null || !settings.enabled) return;
            if (thing?.def == null || pawn?.def?.race == null || !pawn.def.race.Humanlike) return;

            if (settings.unlockRestrictedApparel && !__result)
            {
                __result = true;
                cantReason = null;
            }
            // OFF: HAR's original restriction stands
        }
    }
}
