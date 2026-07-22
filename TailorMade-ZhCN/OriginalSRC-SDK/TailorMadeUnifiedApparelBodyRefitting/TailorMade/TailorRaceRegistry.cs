using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x0200002D RID: 45
	public static class TailorRaceRegistry
	{
		// Token: 0x060000F9 RID: 249 RVA: 0x0000E894 File Offset: 0x0000CA94
		private static void Ensure()
		{
			bool flag = TailorRaceRegistry.byRace != null;
			if (!flag)
			{
				TailorRaceRegistry.byRace = new Dictionary<string, TailorRaceDef>();
				foreach (TailorRaceDef tailorRaceDef in DefDatabase<TailorRaceDef>.AllDefsListForReading)
				{
					bool flag2 = !GenText.NullOrEmpty(tailorRaceDef.raceName);
					if (flag2)
					{
						TailorRaceRegistry.byRace[tailorRaceDef.raceName] = tailorRaceDef;
					}
				}
			}
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000E920 File Offset: 0x0000CB20
		public static TailorRaceDef For(ThingDef race)
		{
			bool flag = race == null;
			TailorRaceDef tailorRaceDef;
			if (flag)
			{
				tailorRaceDef = null;
			}
			else
			{
				TailorRaceRegistry.Ensure();
				TailorRaceDef tailorRaceDef2;
				tailorRaceDef = (TailorRaceRegistry.byRace.TryGetValue(race.defName, out tailorRaceDef2) ? tailorRaceDef2 : null);
			}
			return tailorRaceDef;
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000E95C File Offset: 0x0000CB5C
		public static string ExplicitBodyPath(ThingDef race, BodyTypeDef bodyType)
		{
			TailorRaceDef tailorRaceDef = TailorRaceRegistry.For(race);
			bool flag = tailorRaceDef == null || bodyType == null;
			string text;
			if (flag)
			{
				text = null;
			}
			else
			{
				foreach (TailorRaceDef.BodyPathEntry bodyPathEntry in tailorRaceDef.bodyPaths)
				{
					bool flag2 = bodyPathEntry.bodyType == bodyType && !GenText.NullOrEmpty(bodyPathEntry.path);
					if (flag2)
					{
						return bodyPathEntry.path;
					}
				}
				text = null;
			}
			return text;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000E9F8 File Offset: 0x0000CBF8
		public static void Invalidate()
		{
			TailorRaceRegistry.byRace = null;
		}

		// Token: 0x040000FC RID: 252
		private static Dictionary<string, TailorRaceDef> byRace;
	}
}
