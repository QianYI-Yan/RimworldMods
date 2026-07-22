using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200001E RID: 30
	public static class HarSupport
	{
		// Token: 0x060000A2 RID: 162 RVA: 0x0000A9F8 File Offset: 0x00008BF8
		public static bool IsAlienRace(ThingDef def)
		{
			return HarSupport.Active && def != null && HarSupport.alienDefType.IsInstanceOfType(def);
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x0000AA24 File Offset: 0x00008C24
		public static string TryGetBodyPath(ThingDef race, BodyTypeDef bodyType)
		{
			bool flag = !HarSupport.IsAlienRace(race) || bodyType == null;
			string text;
			if (flag)
			{
				text = null;
			}
			else
			{
				ValueTuple<ushort, ushort> valueTuple = new ValueTuple<ushort, ushort>(race.shortHash, bodyType.index);
				string text2;
				bool flag2 = HarSupport.bodyPathCache.TryGetValue(valueTuple, out text2);
				if (flag2)
				{
					text = text2;
				}
				else
				{
					string text3 = null;
					string text4 = TailorRaceRegistry.ExplicitBodyPath(race, bodyType);
					bool flag3 = !GenText.NullOrEmpty(text4) && HarSupport.HasTex(text4);
					if (flag3)
					{
						HarSupport.bodyPathCache[valueTuple] = text4;
						text = text4;
					}
					else
					{
						try
						{
							object value = Traverse.Create(race).Field("alienRace").Field("graphicPaths")
								.Field("body")
								.GetValue();
							string text5 = null;
							string text6 = value as string;
							bool flag4 = text6 != null;
							if (flag4)
							{
								text5 = text6;
							}
							else
							{
								bool flag5 = value != null;
								if (flag5)
								{
									Traverse traverse = Traverse.Create(value).Field("path");
									bool flag6 = traverse.FieldExists();
									if (flag6)
									{
										text5 = traverse.GetValue<string>();
									}
								}
							}
							bool flag7 = !GenText.NullOrEmpty(text5);
							if (flag7)
							{
								string text7 = text5.TrimEnd(new char[] { '/' });
								List<string> list = new List<string>
								{
									text5 + "Naked_" + bodyType.defName,
									text7 + "/Naked_" + bodyType.defName,
									text5 + "_" + bodyType.defName,
									text7 + "_" + bodyType.defName,
									text7
								};
								TailorRaceDef tailorRaceDef = TailorRaceRegistry.For(race);
								bool flag8 = tailorRaceDef != null;
								if (flag8)
								{
									for (int i = tailorRaceDef.bodyPathTemplates.Count - 1; i >= 0; i--)
									{
										string text8 = tailorRaceDef.bodyPathTemplates[i];
										bool flag9 = !GenText.NullOrEmpty(text8);
										if (flag9)
										{
											list.Insert(0, text8.Replace("{0}", bodyType.defName));
										}
									}
								}
								foreach (string text9 in list)
								{
									bool flag10 = GenText.NullOrEmpty(text9);
									if (!flag10)
									{
										bool flag11 = HarSupport.HasTex(text9);
										if (flag11)
										{
											text3 = text9;
											break;
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							Log.WarningOnce("[TailorMade] Failed reading HAR body path for " + race.defName + ": " + ex.Message, (int)(race.shortHash ^ 31249));
						}
						HarSupport.bodyPathCache[valueTuple] = text3;
						text = text3;
					}
				}
			}
			return text;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x0000AD1C File Offset: 0x00008F1C
		private static bool HasTex(string path)
		{
			return ContentFinder<Texture2D>.Get(path + "_south", false) != null || ContentFinder<Texture2D>.Get(path + "_north", false) != null || ContentFinder<Texture2D>.Get(path + "_east", false) != null || ContentFinder<Texture2D>.Get(path + "_west", false) != null || ContentFinder<Texture2D>.Get(path, false) != null;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x0000ADA2 File Offset: 0x00008FA2
		public static void ClearCache()
		{
			HarSupport.bodyPathCache.Clear();
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x0000ADB0 File Offset: 0x00008FB0
		public static bool IsRaceRestrictedApparel(ThingDef def)
		{
			return HarSupport.harApparelRestricted != null && def != null && HarSupport.harApparelRestricted.Contains(def);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000ADDC File Offset: 0x00008FDC
		public static void TryPatchRestrictions(Harmony harmony)
		{
			bool flag = !HarSupport.Active;
			if (!flag)
			{
				try
				{
					Type type = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
					MethodInfo methodInfo = AccessTools.Method(type, "CanWear", new Type[]
					{
						typeof(ThingDef),
						typeof(ThingDef)
					}, null);
					bool flag2 = methodInfo == null;
					if (flag2)
					{
						Log.Error("[TailorMade] HAR RaceRestrictionSettings.CanWear not found; apparel-restriction bypass disabled.");
					}
					else
					{
						harmony.Patch(methodInfo, null, new HarmonyMethod(typeof(HarSupport), "CanWearPostfix", null), null, null);
						FieldInfo fieldInfo = AccessTools.Field(type, "apparelRestricted");
						HarSupport.harApparelRestricted = ((fieldInfo != null) ? fieldInfo.GetValue(null) : null) as HashSet<ThingDef>;
						MethodInfo methodInfo2 = AccessTools.Method(typeof(EquipmentUtility), "CanEquip", new Type[]
						{
							typeof(Thing),
							typeof(Pawn),
							typeof(string).MakeByRefType(),
							typeof(bool)
						}, null);
						harmony.Patch(methodInfo2, null, new HarmonyMethod(typeof(HarSupport), "CanEquipRecordPostfix", null)
						{
							priority = 800
						}, null, null);
						harmony.Patch(methodInfo2, null, new HarmonyMethod(typeof(HarSupport), "CanEquipRestorePostfix", null)
						{
							priority = 0
						}, null, null);
					}
				}
				catch (Exception ex)
				{
					string text = "[TailorMade] Failed to patch HAR apparel restrictions: ";
					Exception ex2 = ex;
					Log.Error(text + ((ex2 != null) ? ex2.ToString() : null));
				}
			}
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x0000AF80 File Offset: 0x00009180
		private static void CanEquipRecordPostfix(bool __result)
		{
			HarSupport.vanillaCanEquip = __result;
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x0000AF8C File Offset: 0x0000918C
		private static void CanEquipRestorePostfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
		{
			bool flag = __result || !HarSupport.vanillaCanEquip;
			if (!flag)
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				bool flag2 = settings == null || !settings.enabled || !settings.unlockRestrictedApparel;
				if (!flag2)
				{
					bool flag3 = ((thing != null) ? thing.def : null) == null || !thing.def.IsApparel;
					if (!flag3)
					{
						bool flag4;
						if (pawn == null)
						{
							flag4 = null != null;
						}
						else
						{
							ThingDef def = pawn.def;
							flag4 = ((def != null) ? def.race : null) != null;
						}
						bool flag5 = !flag4 || !pawn.def.race.Humanlike;
						if (!flag5)
						{
							bool flag6 = HarSupport.IsAlienRace(pawn.def);
							if (!flag6)
							{
								bool flag7 = HarSupport.harApparelRestricted != null && !HarSupport.IsRaceRestrictedApparel(thing.def);
								if (!flag7)
								{
									__result = true;
									cantReason = null;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060000AA RID: 170 RVA: 0x0000B070 File Offset: 0x00009270
		private static void CanWearPostfix(ThingDef apparel, ThingDef race, ref bool __result)
		{
			bool flag = __result;
			if (!flag)
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				bool flag2 = settings == null || !settings.enabled || !settings.unlockRestrictedApparel;
				if (!flag2)
				{
					bool flag3 = HarSupport.IsAlienRace(race);
					if (!flag3)
					{
						__result = true;
					}
				}
			}
		}

		// Token: 0x04000073 RID: 115
		public static readonly bool Active = HarSupport.alienDefType != null;

		// Token: 0x04000074 RID: 116
		private static readonly Type alienDefType = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");

		// Token: 0x04000075 RID: 117
		private static readonly Dictionary<ValueTuple<ushort, ushort>, string> bodyPathCache = new Dictionary<ValueTuple<ushort, ushort>, string>();

		// Token: 0x04000076 RID: 118
		private static HashSet<ThingDef> harApparelRestricted;

		// Token: 0x04000077 RID: 119
		[ThreadStatic]
		private static bool vanillaCanEquip;
	}
}
