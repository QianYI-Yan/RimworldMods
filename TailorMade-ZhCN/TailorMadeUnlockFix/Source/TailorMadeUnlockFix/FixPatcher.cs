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
            var harmony = new Harmony("yintx.deepseek.astryl.tailormade.unlockfix");

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
                    string raceList = string.Join(", ", RecoveredRaceApparelLists.Keys);
                    Log.Message($"[TailorMadeUnlockFix] Recovered {RecoveredRaceApparelLists.Count} races [{raceList}], " +
                        $"{RecoveredRestrictedApparel.Count} apparel items. Fix active.");
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
                if (mods == null)
                {
                    Log.Warning("[TailorMadeUnlockFix] RunningMods is null.");
                    return;
                }

                int index = 0;
                int scanned = 0;
                var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "About", "Assemblies", "Languages", "Textures", "Sounds",
                    "Source", "lib", ".git", "bin", "obj", "MonoBleedingEdge"
                };

                foreach (var mod in mods)
                {
                    index++;
                    string root = null;
                    string modName = "(unknown)";

                    try { modName = mod.GetType().GetProperty("Name")?.GetValue(mod)?.ToString() ?? "(unknown)"; } catch { }

                    try
                    {
                        var rootProp = mod.GetType().GetProperty("RootDir");
                        if (rootProp != null)
                            root = rootProp.GetValue(mod)?.ToString();
                        else
                        {
                            var rootField = mod.GetType().GetField("rootDir");
                            if (rootField != null)
                                root = rootField.GetValue(mod)?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[TailorMadeUnlockFix]   mod[{index}] {modName}: cant get RootDir - {ex.Message}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(root))
                    {
                        Log.Warning($"[TailorMadeUnlockFix]   mod[{index}] {modName}: RootDir is empty");
                        continue;
                    }

                    if (!Directory.Exists(root))
                    {
                        Log.Warning($"[TailorMadeUnlockFix]   mod[{index}] {modName}: dir not found '{root}'");
                        continue;
                    }

                    Log.Message($"[TailorMadeUnlockFix]   Scanning mod[{index}]: {modName}");
                    scanned++;

                    try { SearchXmlRecursive(root, root, skipDirs); }
                    catch (Exception ex) { Log.Warning($"[TailorMadeUnlockFix]   Error scanning {modName}: {ex.Message}"); }
                }

                _recoverySucceeded = RecoveredRaceApparelLists.Count > 0;

                if (_recoverySucceeded)
                {
                    string raceList = string.Join(", ", RecoveredRaceApparelLists.Keys);
                    Log.Message($"[TailorMadeUnlockFix] Recovery OK: " +
                        $"{RecoveredRaceApparelLists.Count} races [{raceList}], " +
                        $"{RecoveredRestrictedApparel.Count} apparel items.");
                }
                else
                {
                    string raceNames = RecoveredRaceApparelLists.Count > 0
                        ? string.Join(", ", RecoveredRaceApparelLists.Keys)
                        : "(none)";
                    Log.Warning($"[TailorMadeUnlockFix] Recovery: {index} mods total, {scanned} scanned, " +
                        $"{RecoveredRaceApparelLists.Count} races found [{raceNames}]. " +
                        $"XML files parsed across all mods.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[TailorMadeUnlockFix] Recovery error: {ex}");
            }
        }

        private static void SearchXmlRecursive(string basePath, string currentDir,
            HashSet<string> skipDirs)
        {
            try
            {
                // Check for XML files in current directory
                foreach (var xmlFile in Directory.GetFiles(currentDir, "*.xml"))
                {
                    try { ParseHarRaceDefs(xmlFile); }
                    catch { }
                }

                // Recurse into subdirectories
                foreach (var subDir in Directory.GetDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (skipDirs.Contains(dirName))
                        continue;
                    SearchXmlRecursive(basePath, subDir, skipDirs);
                }
            }
            catch { }
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
        /// Strategy: PERMISSIVE — only use recovered data to ALLOW, never to BLOCK
        /// (because recovery data may be incomplete).
        ///
        /// When <c>unlockRestrictedApparel = true</c>:
        ///   Allow all apparel for all races.
        ///
        /// When <c>unlockRestrictedApparel = false</c>:
        ///   - Alien races with recovered data: allow items in their list (override HAR rejection)
        ///   - Non-alien races (Humans): block race-specific apparel using recovered data
        ///   - Otherwise: do nothing (let current state stand)
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
                // Allow everything
                if (!__result)
                    __result = true;
                return;
            }

            // --- unlockRestrictedApparel = false ---

            if (!_recoverySucceeded)
                return;

            if (RecoveredRaceApparelLists.TryGetValue(race.defName, out var allowed))
            {
                // Race has recovered data — only ALLOW confirmed items, never BLOCK
                // (incomplete recovery may miss items, so we don't reject unknowns)
                if (allowed.Contains(apparel.defName) && !__result)
                {
                    __result = true;
                }
            }
            else if (!HarSupport.IsAlienRace(race)
                && RecoveredRestrictedApparel.Contains(apparel.defName))
            {
                // Non-HAR race (eg Human) trying to wear race-specific apparel → BLOCK
                bool allowedByAny = RecoveredRaceApparelLists.Values
                    .Any(list => list.Contains(apparel.defName));
                if (allowedByAny)
                    __result = false;
            }
            // else: no recovered data, do nothing (let current state stand)
        }

        /// <summary>
        /// Postfix on <c>EquipmentUtility.CanEquip</c> (Priority.Last).
        ///
        /// Second line of defense, same permissive strategy as CanWearPostfix.
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
                // Allow everything
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
                // Only ALLOW confirmed items, never BLOCK
                if (allowed.Contains(apparelName) && !__result)
                {
                    __result = true;
                    cantReason = null;
                }
            }
            else if (!HarSupport.IsAlienRace(pawn.def)
                && RecoveredRestrictedApparel.Contains(apparelName))
            {
                // Non-HAR race wearing race-specific apparel → BLOCK
                bool allowedByAny = RecoveredRaceApparelLists.Values
                    .Any(list => list.Contains(apparelName));
                if (allowedByAny && __result)
                {
                    __result = false;
                    cantReason = "CannotEquip_RaceRestriction".Translate();
                }
            }
        }
    }
}
