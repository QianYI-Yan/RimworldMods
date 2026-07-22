using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using TailorMade;

namespace TailorMadeFix
{
    /// <summary>
    /// 模组入口 —— 应用所有修复补丁
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class FixPatcher
    {
        static FixPatcher()
        {
            var harmony = new Harmony("astryl.tailormade.racefix");

            // ── 补丁 1：HAR 的 CanWear ──
            // 在 TailorMade 的 CanWearPostfix 之后执行（Priority.Last），
            // 当 unlockRestrictedApparel = false 时检查是否需要禁止跨种族穿衣。
            TryPatchCanWear(harmony);

            // ── 补丁 2：EquipmentUtility.CanEquip ──
            // 在 TailorMade 的 CanEquipRestorePostfix 之后执行（Priority.LowerThanNormal），
            // 同上逻辑。
            TryPatchCanEquip(harmony);

            Log.Message("[TailorMadeFix] Race restriction fix loaded. unlockRestrictedApparel will now work correctly.");
        }

        /// <summary>
        /// 补丁 AlienRace.RaceRestrictionSettings.CanWear(ThingDef, ThingDef)
        /// </summary>
        private static void TryPatchCanWear(Harmony harmony)
        {
            try
            {
                var type = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
                if (type == null)
                {
                    Log.Message("[TailorMadeFix] HAR not detected — skipping CanWear patch.");
                    return;
                }

                var method = AccessTools.Method(type, "CanWear", new Type[]
                {
                    typeof(ThingDef),
                    typeof(ThingDef)
                });
                if (method == null)
                {
                    Log.Warning("[TailorMadeFix] HAR RaceRestrictionSettings.CanWear not found.");
                    return;
                }

                harmony.Patch(method, postfix: new HarmonyMethod(typeof(FixPatcher), nameof(CanWearFinalizer))
                {
                    priority = Priority.Last
                });

                Log.Message("[TailorMadeFix] Patched HAR CanWear ✓");
            }
            catch (Exception ex)
            {
                Log.Error($"[TailorMadeFix] Failed to patch HAR CanWear: {ex.Message}");
            }
        }

        /// <summary>
        /// 补丁 EquipmentUtility.CanEquip(Thing, Pawn, ref string, bool)
        /// </summary>
        private static void TryPatchCanEquip(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(typeof(EquipmentUtility), "CanEquip", new Type[]
                {
                    typeof(Thing),
                    typeof(Pawn),
                    typeof(string).MakeByRefType(),
                    typeof(bool)
                });
                if (method == null)
                {
                    Log.Warning("[TailorMadeFix] EquipmentUtility.CanEquip not found.");
                    return;
                }

                harmony.Patch(method, postfix: new HarmonyMethod(typeof(FixPatcher), nameof(CanEquipFinalizer))
                {
                    priority = Priority.Last
                });

                Log.Message("[TailorMadeFix] Patched EquipmentUtility.CanEquip ✓");
            }
            catch (Exception ex)
            {
                Log.Error($"[TailorMadeFix] Failed to patch CanEquip: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════
        //  CanWear 补丁
        // ════════════════════════════════════════════════════

        /// <summary>
        /// 在 TailorMade 的 CanWearPostfix 之后运行。
        /// 当 unlockRestrictedApparel = false 时，如果 HAR 允许了跨种族穿衣，
        /// 我们检查是否需要禁止。
        /// </summary>
        internal static void CanWearFinalizer(ThingDef apparel, ThingDef race, ref bool __result)
        {
            // HAR 已经拒绝 → 无需处理
            if (__result == false) return;

            // 获取 TailorMade 设置
            var settings = TailorMadeMod.Settings;
            if (settings == null) return;

            // 设置开启 → TailorMade 负责放行，不动
            if (settings.unlockRestrictedApparel) return;

            // 设置关闭 → 我们需要检查是否应该限制
            if (apparel == null || race == null) return;

            // 只对外星种族做检查
            if (!HarSupport.IsAlienRace(race)) return;

            if (ShouldRestrict(apparel, race))
            {
                __result = false;
            }
        }

        // ════════════════════════════════════════════════════
        //  CanEquip 补丁
        // ════════════════════════════════════════════════════

        /// <summary>
        /// 在 TailorMade 的 CanEquipRestorePostfix 之后运行。
        /// 当 unlockRestrictedApparel = false 时，确保跨种族服装被拒绝。
        /// </summary>
        internal static void CanEquipFinalizer(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            // 已经被拒绝 → 不动
            if (__result == false) return;

            var settings = TailorMadeMod.Settings;
            if (settings == null) return;

            // 设置开启 → TailorMade 负责放行
            if (settings.unlockRestrictedApparel) return;

            if (thing == null || pawn == null) return;

            var pawnRace = pawn.def;
            if (pawnRace == null) return;

            // 只对外星种族做检查
            if (!HarSupport.IsAlienRace(pawnRace)) return;

            var apparelDef = thing.def;
            if (apparelDef == null) return;

            if (ShouldRestrict(apparelDef, pawnRace))
            {
                __result = false;
                cantReason = "RaceRestrictedApparel".Translate() ?? "Race-restricted apparel.";
            }
        }

        // ════════════════════════════════════════════════════
        //  限制检查逻辑
        // ════════════════════════════════════════════════════

        /// <summary>
        /// 缓存：记录哪些 Mod 包含外星种族定义。
        /// </summary>
        private static readonly Dictionary<ModContentPack, bool> _modHasAlienRaces = new Dictionary<ModContentPack, bool>();

        /// <summary>
        /// 判断某个服装是否应该被某个种族限制。
        /// 
        /// 逻辑（启发式，因为原始数据已被 TailorMade 的 XML Patch 删除）：
        /// 1. 如果服装和种族来自同一个 Mod → 允许（该 Mod 的服装为该种族设计）
        /// 2. 如果服装来自原版或官方 DLC → 允许（通用服装）
        /// 3. 如果服装来自的 Mod 包含外星种族定义 → 限制（该服装是某外星种族的专属装备）
        /// 4. 否则 → 允许
        /// </summary>
        private static bool ShouldRestrict(ThingDef apparel, ThingDef race)
        {
            try
            {
                var apparelMod = apparel.modContentPack;
                var raceMod = race.modContentPack;

                // 来自同一 Mod → 不限制
                if (apparelMod != null && raceMod != null && apparelMod == raceMod)
                    return false;

                // 来自原版或官方 DLC → 不限制
                if (apparelMod == null || IsOfficialMod(apparelMod))
                    return false;

                // 检查服装来源的 Mod 是否包含外星种族
                // 如果是，该服装很可能是该种族的专属装备
                if (ModContainsAlienRaces(apparelMod))
                    return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 判断是否是官方 Mod。
        /// </summary>
        private static bool IsOfficialMod(ModContentPack mod)
        {
            var name = mod.Name;
            return name == "Core"
                || name == "Royalty"
                || name == "Ideology"
                || name == "Biotech"
                || name == "Anomaly"
                || name.StartsWith("Official");
        }

        /// <summary>
        /// 判断某个 Mod 是否包含外星种族（HAR ThingDef_AlienRace）。
        /// 结果会被缓存。
        /// </summary>
        private static bool ModContainsAlienRaces(ModContentPack mod)
        {
            // 先查缓存
            if (_modHasAlienRaces.TryGetValue(mod, out var result))
                return result;

            // 扫描所有 ThingDef，看该 Mod 是否有 HAR 外星种族
            result = false;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.modContentPack == mod && HarSupport.IsAlienRace(def))
                {
                    result = true;
                    break;
                }
            }

            _modHasAlienRaces[mod] = result;
            return result;
        }
    }
}
