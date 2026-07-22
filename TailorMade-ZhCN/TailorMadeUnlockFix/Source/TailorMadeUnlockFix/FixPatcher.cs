using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using RimWorld;
using TailorMade;
using Verse;

namespace TailorMadeUnlockFix
{
    /// <summary>
    /// Fixes TailorMade's unlockRestrictedApparel setting.
    ///
    /// Problem: TailorMade's XML patch (TailorMade_UnlockRaceRestrictedApparel.xml)
    /// unconditionally deletes HAR race apparelList data during XML loading.
    /// The unlockRestrictedApparel setting only controls TailorMade's Harmony backup patches,
    /// but those never fire because the XML data was already destroyed.
    ///
    /// Fix:
    /// 1. [XML] Restores onlyUseRaceRestrictedApparel to true for all HAR races
    ///    (see Patches/RestoreOnlyUseRaceRestricted.xml)
    /// 2. [C#] Reads the ORIGINAL XML files directly from disk (before TailorMade's patches)
    ///    to recover the deleted apparelList data, then re-enforces restrictions at runtime.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class FixPatcher
    {
        // Recovered data: race defName -> set of apparel defNames the race can wear
        private static readonly Dictionary<string, HashSet<string>> RecoveredRaceApparelLists
            = new Dictionary<string, HashSet<string>>();

        // All apparel defNames that are restricted by at least one HAR race
        private static readonly HashSet<string> RecoveredRestrictedApparel
            = new HashSet<string>();

        private static bool _recoverySucceeded;

        static FixPatcher()
        {
            var harmony = new Harmony("tailormade.unlockfix");

            try
            {
                // Step 1: recover restriction data from raw XML files on disk
                RecoverHarRestrictionsFromDisk();

                // Step 2: patch HAR's RaceRestrictionSettings.CanWear
                var rsType = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
                if (rsType != null)
                {
                    var canWear = AccessTools.Method(rsType, "CanWear",
                        new[] { typeof(ThingDef), typeof(ThingDef) });
                    if (canWear != null)
                    {
                        harmony.Patch(canWear,
                            postfix: new HarmonyMethod(typeof(FixPatcher), nameof(CanWearPostfix))
                            {
                                priority = Priority.Last
                            });
                    }
                }

                // Step 3: patch EquipmentUtility.CanEquip
                var canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                    new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) });
                if (canEquip != null)
                {
                    harmony.Patch(canEquip,
                        postfix: new HarmonyMethod(typeof(FixPatcher), nameof(CanEquipPostfix))
                        {
                            priority = Priority.Last
                        });
                }

                if (_recoverySucceeded)
                {
                    Log.Message($"[TailorMadeUnlockFix] Recovered restriction data for " +
                        $"{RecoveredRaceApparelLists.Count} HAR races, " +
                        $"{RecoveredRestrictedApparel.Count} restricted apparel items. Fix active.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[TailorMadeUnlockFix] Initialization failed: {ex}");
            }
        }

        // ---------------------------------------------------------------
        // Data recovery: read raw XML from disk (pre-patch data)
        // ---------------------------------------------------------------

        private static void RecoverHarRestrictionsFromDisk()
        {
            try
            {
                var mods = LoadedModManager.RunningMods;
                if (mods == null || !mods.Any())
                {
                    Log.Warning("[TailorMadeUnlockFix] No running mods, cannot recover restriction data.");
                    return;
                }

                foreach (var mod in mods)
                {
                    var root = mod.RootDir?.ToString();
                    if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                        continue;

                    var defsDir = Path.Combine(root, "Defs");
                    if (!Directory.Exists(defsDir))
                        continue;

                    foreach (var xmlFile in Directory.GetFiles(defsDir, "*.xml", SearchOption.AllDirectories))
                    {
                        try { ParseHarRaceDefs(xmlFile); }
                        catch { /* skip unparseable files */ }
                    }
                }

                _recoverySucceeded = RecoveredRaceApparelLists.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Error($"[TailorMadeUnlockFix] Recovery error: {ex}");
            }
        }

        /// <summary>Parse one XML file for HAR race definitions with apparelList.</summary>
        private static void ParseHarRaceDefs(string filePath)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var defs = doc.DocumentElement;
            if (defs?.Name != "Defs")
                return;

            foreach (XmlNode node in defs.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                string defName = null;
                bool onlyUseRestricted = false;
                List<string> apparelList = null;

                // Format 1: <AlienRace.ThingDef_AlienRace>
                if (node.Name == "AlienRace.ThingDef_AlienRace")
                {
                    defName = node.SelectSingleNode("defName")?.InnerText?.Trim();
                    ExtractRestrictionData(node, ref onlyUseRestricted, ref apparelList);
                }
                // Format 2: <ThingDef Class="AlienRace.ThingDef_AlienRace">
                else if (node.Name == "ThingDef")
                {
                    var cls = node.Attributes?["Class"]?.Value;
                    if (cls != "AlienRace.ThingDef_AlienRace")
                        continue;
                    defName = node.SelectSingleNode("defName")?.InnerText?.Trim();
                    ExtractRestrictionData(node, ref onlyUseRestricted, ref apparelList);
                }

                if (string.IsNullOrEmpty(defName) || !onlyUseRestricted ||
                    apparelList == null || apparelList.Count == 0)
                    continue;

                var set = new HashSet<string>();
                foreach (var a in apparelList)
                    set.Add(a);

                RecoveredRaceApparelLists[defName] = set;
                foreach (var a in apparelList)
                    RecoveredRestrictedApparel.Add(a);
            }
        }

        private static void ExtractRestrictionData(
            XmlNode raceNode, ref bool onlyUseRestricted, ref List<string> apparelList)
        {
            var restricted = raceNode.SelectSingleNode(
                "alienRace/raceRestriction/onlyUseRaceRestrictedApparel");
            if (restricted?.InnerText.Trim() == "true")
                onlyUseRestricted = true;

            var liNodes = raceNode.SelectNodes(
                "alienRace/raceRestriction/apparelList/li");
            if (liNodes != null && liNodes.Count > 0)
            {
                apparelList = new List<string>();
                foreach (XmlNode li in liNodes)
                {
                    var val = li.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(val))
                        apparelList.Add(val);
                }
            }
        }

        // ---------------------------------------------------------------
        // Harmony postfixes
        // ---------------------------------------------------------------

        /// <summary>
        /// Postfix on <c>RaceRestrictionSettings.CanWear(ThingDef, ThingDef)</c>.
        ///
        /// When <c>unlockRestrictedApparel = true</c> (default):
        ///   Override HAR's rejection (caused by restored onlyUseRaceRestrictedApparel=true
        ///   with empty apparelList) — allow all apparel for all races.
        ///
        /// When <c>unlockRestrictedApparel = false</c>:
        ///   Use recovered data to determine the correct restriction outcome.
        /// </summary>
        public static void CanWearPostfix(ThingDef apparel, ThingDef race, ref bool __result)
        {
            var settings = TailorMadeMod.Settings;
            if (settings == null || !settings.enabled)
                return;
            if (race == null || apparel == null)
                return;

            if (settings.unlockRestrictedApparel)
            {
                // User wants everything unlocked — override any HAR rejection
                if (!__result)
                    __result = true;
                return;
            }

            // --- unlockRestrictedApparel = false ---

            if (!_recoverySucceeded)
                return;

            if (RecoveredRaceApparelLists.TryGetValue(race.defName, out var allowed))
            {
                if (allowed.Contains(apparel.defName))
                {
                    // Recovered data confirms this race can wear this apparel → allow
                    if (!__result)
                        __result = true;
                }
                // else: recovered data confirms restriction → let rejection stand
            }
            else if (!RecoveredRestrictedApparel.Contains(apparel.defName))
            {
                // No recovery data for this race AND the apparel is not known to be
                // restricted by any race → allow (conservative default)
                if (!__result)
                    __result = true;
            }
        }

        /// <summary>
        /// Postfix on <c>EquipmentUtility.CanEquip</c> (Priority.Last).
        ///
        /// Second line of defense: ensures the final CanEquip result is consistent
        /// with the recovered restriction data.
        /// </summary>
        public static void CanEquipPostfix(Thing thing, Pawn pawn,
            ref bool __result, ref string cantReason)
        {
            var settings = TailorMadeMod.Settings;
            if (settings == null || !settings.enabled)
                return;
            if (thing?.def == null || pawn?.def?.race == null || !pawn.def.race.Humanlike)
                return;

            if (settings.unlockRestrictedApparel)
            {
                // User wants everything unlocked
                if (!__result)
                {
                    __result = true;
                    cantReason = null;
                }
                return;
            }

            // --- unlockRestrictedApparel = false ---

            if (!_recoverySucceeded)
                return;

            var raceName = pawn.def.defName;
            var apparelName = thing.def.defName;

            if (RecoveredRaceApparelLists.TryGetValue(raceName, out var allowed))
            {
                if (allowed.Contains(apparelName))
                {
                    // Our data says allowed
                    if (!__result)
                    {
                        __result = true;
                        cantReason = null;
                    }
                }
                else
                {
                    // Our data says restricted — reject
                    if (__result)
                    {
                        __result = false;
                        cantReason = "CannotEquip_RaceRestriction".Translate();
                    }
                }
            }
            else if (RecoveredRestrictedApparel.Contains(apparelName))
            {
                // This apparel is restricted by some race, but we have no data for
                // this specific race → check if ANY race allows it
                bool allowedByAny = RecoveredRaceApparelLists.Values
                    .Any(list => list.Contains(apparelName));
                if (allowedByAny && __result)
                {
                    // Apparel is race-specific and this race isn't in our recovered data
                    __result = false;
                    cantReason = "CannotEquip_RaceRestriction".Translate();
                }
            }
        }
    }
}
