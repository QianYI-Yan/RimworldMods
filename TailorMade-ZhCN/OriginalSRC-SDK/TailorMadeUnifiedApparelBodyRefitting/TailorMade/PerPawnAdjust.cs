using System;
using Verse;

namespace TailorMade
{
	// Token: 0x02000029 RID: 41
	public static class PerPawnAdjust
	{
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000DE RID: 222 RVA: 0x0000DF34 File Offset: 0x0000C134
		public static PerPawnData Data
		{
			get
			{
				Game game = Current.Game;
				bool flag = game == null;
				PerPawnData perPawnData;
				if (flag)
				{
					perPawnData = null;
				}
				else
				{
					bool flag2 = game != PerPawnAdjust.cachedGame;
					if (flag2)
					{
						PerPawnAdjust.cachedGame = game;
						PerPawnAdjust.cachedComp = game.GetComponent<TailorMadeGameComponent>();
					}
					TailorMadeGameComponent tailorMadeGameComponent = PerPawnAdjust.cachedComp;
					perPawnData = ((tailorMadeGameComponent != null) ? tailorMadeGameComponent.perPawn : null);
				}
				return perPawnData;
			}
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000DF8B File Offset: 0x0000C18B
		public static TailorAdjust Get(Pawn pawn, string defName)
		{
			TailorAdjust tailorAdjust;
			if (pawn != null)
			{
				PerPawnData data = PerPawnAdjust.Data;
				tailorAdjust = ((data != null) ? data.Get(pawn.thingIDNumber, defName) : null);
			}
			else
			{
				tailorAdjust = null;
			}
			return tailorAdjust;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000DFAB File Offset: 0x0000C1AB
		public static TailorAdjust GetById(int pawnId, string defName)
		{
			PerPawnData data = PerPawnAdjust.Data;
			return (data != null) ? data.Get(pawnId, defName) : null;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000DFC0 File Offset: 0x0000C1C0
		public static bool Has(Pawn pawn, string defName)
		{
			return PerPawnAdjust.Get(pawn, defName) != null;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000DFCC File Offset: 0x0000C1CC
		public static TailorAdjust GetOrAdd(Pawn pawn, string defName, TailorAdjust seed)
		{
			TailorAdjust tailorAdjust;
			if (pawn != null)
			{
				PerPawnData data = PerPawnAdjust.Data;
				tailorAdjust = ((data != null) ? data.GetOrAdd(pawn.thingIDNumber, defName, seed) : null);
			}
			else
			{
				tailorAdjust = null;
			}
			return tailorAdjust;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000DFF0 File Offset: 0x0000C1F0
		public static void Remove(Pawn pawn, string defName)
		{
			bool flag = pawn != null;
			if (flag)
			{
				PerPawnData data = PerPawnAdjust.Data;
				if (data != null)
				{
					data.Remove(pawn.thingIDNumber, defName);
				}
			}
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000E01E File Offset: 0x0000C21E
		public static void ClearAll()
		{
			PerPawnData data = PerPawnAdjust.Data;
			if (data != null)
			{
				data.ClearAll();
			}
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000E031 File Offset: 0x0000C231
		public static TailorAdjust Effective(Pawn pawn, string defName)
		{
			TailorAdjust tailorAdjust;
			if ((tailorAdjust = PerPawnAdjust.Get(pawn, defName)) == null)
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				tailorAdjust = ((settings != null) ? settings.GetAdjust(defName) : null);
			}
			return tailorAdjust;
		}

		// Token: 0x040000CA RID: 202
		private static Game cachedGame;

		// Token: 0x040000CB RID: 203
		private static TailorMadeGameComponent cachedComp;
	}
}
