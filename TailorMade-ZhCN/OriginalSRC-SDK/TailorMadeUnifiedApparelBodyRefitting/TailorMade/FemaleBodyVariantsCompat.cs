using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace TailorMade
{
	// Token: 0x0200000D RID: 13
	public static class FemaleBodyVariantsCompat
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000044 RID: 68 RVA: 0x0000669C File Offset: 0x0000489C
		public static bool Active
		{
			get
			{
				FemaleBodyVariantsCompat.Resolve();
				return FemaleBodyVariantsCompat.present;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000045 RID: 69 RVA: 0x000066BC File Offset: 0x000048BC
		public static bool Enabled
		{
			get
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				return settings != null && settings.femaleBodyVariants && FemaleBodyVariantsCompat.Active;
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000066E8 File Offset: 0x000048E8
		private static void Resolve()
		{
			bool flag = FemaleBodyVariantsCompat.resolved;
			if (!flag)
			{
				FemaleBodyVariantsCompat.resolved = true;
				try
				{
					FemaleBodyVariantsCompat.present = AccessTools.TypeByName("FemaleBodyVariants.PawnRenderNode_Body_GraphicFor_Patch") != null;
					bool flag2 = FemaleBodyVariantsCompat.present;
					if (flag2)
					{
						Log.Message("[TailorMade] FemaleBodyVariants detected — apparel is fit to the female body variants it resolves (toggle in mod settings).");
					}
				}
				catch (Exception ex)
				{
					FemaleBodyVariantsCompat.present = false;
					Log.Warning("[TailorMade] FemaleBodyVariants compat failed to initialize: " + ex.Message);
				}
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00006768 File Offset: 0x00004968
		public static bool FemaleBody(Pawn pawn)
		{
			return pawn != null && pawn.gender != 1 && FemaleBodyVariantsCompat.Enabled;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00006790 File Offset: 0x00004990
		public static bool EligiblePath(string basePath)
		{
			return !GenText.NullOrEmpty(basePath) && !basePath.Contains("_Female") && (basePath.Contains("_Thin") || basePath.Contains("_Fat") || basePath.Contains("_Hulk"));
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000067E4 File Offset: 0x000049E4
		public static string FemaleVariant(string basePath)
		{
			bool flag = !FemaleBodyVariantsCompat.Enabled || !FemaleBodyVariantsCompat.EligiblePath(basePath);
			string text;
			if (flag)
			{
				text = null;
			}
			else
			{
				string text2;
				bool flag2 = !FemaleBodyVariantsCompat.variantCache.TryGetValue(basePath, out text2);
				if (flag2)
				{
					string text3 = basePath + "_Female";
					text2 = ((TexBake.Find(text3 + "_south", false) != null) ? text3 : "");
					FemaleBodyVariantsCompat.variantCache[basePath] = text2;
				}
				text = (GenText.NullOrEmpty(text2) ? null : text2);
			}
			return text;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00006874 File Offset: 0x00004A74
		public static string FemaleVariantFromProvider(string basePath, string packageId)
		{
			bool flag = !FemaleBodyVariantsCompat.Enabled || GenText.NullOrEmpty(packageId) || !FemaleBodyVariantsCompat.EligiblePath(basePath);
			string text;
			if (flag)
			{
				text = null;
			}
			else
			{
				string text2 = packageId + "|" + basePath;
				string text3;
				bool flag2 = !FemaleBodyVariantsCompat.variantCache.TryGetValue(text2, out text3);
				if (flag2)
				{
					string text4 = basePath + "_Female";
					text3 = ((TexBake.FindFromProvider(text4 + "_south", packageId) != null) ? text4 : "");
					FemaleBodyVariantsCompat.variantCache[text2] = text3;
				}
				text = (GenText.NullOrEmpty(text3) ? null : text3);
			}
			return text;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00006918 File Offset: 0x00004B18
		public static void Clear()
		{
			FemaleBodyVariantsCompat.variantCache.Clear();
		}

		// Token: 0x0400004E RID: 78
		private static bool resolved;

		// Token: 0x0400004F RID: 79
		private static bool present;

		// Token: 0x04000050 RID: 80
		private static readonly Dictionary<string, string> variantCache = new Dictionary<string, string>();
	}
}
