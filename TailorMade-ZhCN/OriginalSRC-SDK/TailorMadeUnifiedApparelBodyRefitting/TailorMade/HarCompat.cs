using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000013 RID: 19
	public static class HarCompat
	{
		// Token: 0x0600007E RID: 126 RVA: 0x0000844C File Offset: 0x0000664C
		public static HarCompat.Profile For(ThingDef race)
		{
			HarCompat.EnsureScanned();
			bool flag = race == null;
			HarCompat.Profile profile;
			if (flag)
			{
				profile = null;
			}
			else
			{
				HarCompat.Profile profile2;
				profile = (HarCompat.profiles.TryGetValue(race.shortHash, out profile2) ? profile2 : null);
			}
			return profile;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00008488 File Offset: 0x00006688
		public static bool Engaged(ThingDef race)
		{
			HarCompat.Profile profile = HarCompat.For(race);
			return profile != null && profile.engaged;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000084AD File Offset: 0x000066AD
		public static void Invalidate()
		{
			HarCompat.profiles = null;
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000084B8 File Offset: 0x000066B8
		public static void EnsureScanned()
		{
			bool flag = HarCompat.profiles != null;
			if (!flag)
			{
				HarCompat.profiles = new Dictionary<ushort, HarCompat.Profile>();
				bool flag2 = !HarSupport.Active;
				if (!flag2)
				{
					int num = 0;
					int num2 = 0;
					int num3 = 0;
					foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
					{
						bool flag3 = !HarSupport.IsAlienRace(thingDef);
						if (!flag3)
						{
							HarCompat.Profile profile = new HarCompat.Profile
							{
								race = thingDef
							};
							HarCompat.Classify(profile);
							HarCompat.profiles[thingDef.shortHash] = profile;
							bool engaged = profile.engaged;
							if (engaged)
							{
								num++;
							}
							else
							{
								bool flag4 = profile.note.StartsWith("vanilla");
								if (flag4)
								{
									num2++;
								}
								else
								{
									num3++;
								}
							}
						}
					}
					Log.Message(string.Format("[TailorMade] HAR compatibility scan: {0} alien races — ", HarCompat.profiles.Count) + string.Format("{0} auto-fit, {1} vanilla-bodied (CB2 covers), {2} skipped (non-biped).", num, num2, num3));
				}
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000085F4 File Offset: 0x000067F4
		private static void Classify(HarCompat.Profile p)
		{
			ThingDef race = p.race;
			TailorRaceDef tailorRaceDef = TailorRaceRegistry.For(race);
			bool flag2;
			if (tailorRaceDef != null)
			{
				bool? engage = tailorRaceDef.engage;
				bool flag = false;
				flag2 = (engage.GetValueOrDefault() == flag) & (engage != null);
			}
			else
			{
				flag2 = false;
			}
			bool flag3 = flag2;
			if (flag3)
			{
				p.note = "skip: disabled by TailorRaceDef";
			}
			else
			{
				bool flag4 = tailorRaceDef != null && tailorRaceDef.engage.GetValueOrDefault();
				bool flag5 = !flag4 && (race.race == null || !race.race.Humanlike || !race.race.IsFlesh);
				if (flag5)
				{
					p.note = "skip: non-humanlike / non-flesh race";
				}
				else
				{
					try
					{
						Traverse traverse = Traverse.Create(race).Field("alienRace").Field("generalSettings")
							.Field("alienPartGenerator");
						IList list = traverse.Field("bodyTypes").GetValue() as IList;
						bool flag6 = list != null;
						if (flag6)
						{
							foreach (object obj in list)
							{
								bool flag7 = obj == BodyTypeDefOf.Female;
								if (flag7)
								{
									p.supportsFemale = true;
									break;
								}
							}
						}
						object value = traverse.Field("customDrawSize").GetValue();
						bool flag8;
						if (value is Vector2)
						{
							Vector2 vector = (Vector2)value;
							flag8 = Mathf.Abs(vector.x - 1f) > 0.01f || Mathf.Abs(vector.y - 1f) > 0.01f;
						}
						else
						{
							flag8 = false;
						}
						bool flag9 = flag8;
						if (flag9)
						{
							p.customDrawSize = true;
						}
					}
					catch
					{
					}
					foreach (BodyTypeDef bodyTypeDef in DefDatabase<BodyTypeDef>.AllDefsListForReading)
					{
						bool flag10 = GenText.NullOrEmpty(bodyTypeDef.bodyNakedGraphicPath);
						if (!flag10)
						{
							string text = HarSupport.TryGetBodyPath(race, bodyTypeDef);
							bool flag11 = !GenText.NullOrEmpty(text) && text != bodyTypeDef.bodyNakedGraphicPath;
							if (flag11)
							{
								p.resolvableBodyTypes.Add(bodyTypeDef.index);
							}
						}
					}
					bool flag12 = p.resolvableBodyTypes.Count > 0 || flag4;
					if (flag12)
					{
						p.engaged = true;
						p.note = ((flag4 && p.resolvableBodyTypes.Count == 0) ? "auto-fit (forced by TailorRaceDef)" : (string.Format("auto-fit ({0} body types", p.resolvableBodyTypes.Count) + (p.customDrawSize ? ", customDrawSize" : "") + (p.supportsFemale ? "" : ", no Female body") + ")"));
					}
					else
					{
						p.note = "vanilla-bodied (CB2 covers it)";
					}
				}
			}
		}

		// Token: 0x04000070 RID: 112
		private static Dictionary<ushort, HarCompat.Profile> profiles;

		// Token: 0x02000034 RID: 52
		public class Profile
		{
			// Token: 0x0600011C RID: 284 RVA: 0x00010A11 File Offset: 0x0000EC11
			public bool CanFit(BodyTypeDef bt)
			{
				return bt != null && this.resolvableBodyTypes.Contains(bt.index);
			}

			// Token: 0x0400011E RID: 286
			public ThingDef race;

			// Token: 0x0400011F RID: 287
			public bool engaged;

			// Token: 0x04000120 RID: 288
			public string note = "";

			// Token: 0x04000121 RID: 289
			public bool supportsFemale;

			// Token: 0x04000122 RID: 290
			public bool customDrawSize;

			// Token: 0x04000123 RID: 291
			public readonly HashSet<ushort> resolvableBodyTypes = new HashSet<ushort>();
		}
	}
}
