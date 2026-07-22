using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x0200002A RID: 42
	public static class SizedApparelCompat
	{
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000E6 RID: 230 RVA: 0x0000E050 File Offset: 0x0000C250
		public static bool Active
		{
			get
			{
				SizedApparelCompat.Resolve();
				return SizedApparelCompat.canApply != null && !SizedApparelCompat.broken;
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000E7 RID: 231 RVA: 0x0000E080 File Offset: 0x0000C280
		public static bool DeferEnabled
		{
			get
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				return settings != null && settings.sizedApparelDefer && SizedApparelCompat.Active;
			}
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000E0AC File Offset: 0x0000C2AC
		private static void Resolve()
		{
			bool flag = SizedApparelCompat.resolved;
			if (!flag)
			{
				SizedApparelCompat.resolved = true;
				try
				{
					Type type = AccessTools.TypeByName("SizedApparel.SizedApparelUtility");
					bool flag2 = type == null;
					if (!flag2)
					{
						SizedApparelCompat.canApply = AccessTools.Method(type, "CanApplySizedApparel", new Type[] { typeof(Pawn) }, null);
						Type type2 = AccessTools.TypeByName("SizedApparel.SizedApparelSettings");
						bool flag3 = type2 != null;
						if (flag3)
						{
							SizedApparelCompat.useBodyTexture = AccessTools.Field(type2, "useBodyTexture");
						}
						bool flag4 = SizedApparelCompat.canApply == null;
						if (flag4)
						{
							Log.Warning("[TailorMade] Sized Apparel detected but SizedApparelUtility.CanApplySizedApparel did not resolve — its API drifted; deference is disabled.");
						}
						else
						{
							Log.Message("[TailorMade] Sized Apparel for RJW detected — deferring to it for apparel/bodies it manages (toggle in mod settings).");
						}
					}
				}
				catch (Exception ex)
				{
					SizedApparelCompat.broken = true;
					Log.Warning("[TailorMade] Sized Apparel compat failed to initialize: " + ex.Message);
				}
			}
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000E198 File Offset: 0x0000C398
		public static bool ManagesPawn(Pawn pawn)
		{
			bool flag = pawn == null || !SizedApparelCompat.Active;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				try
				{
					flag2 = (bool)SizedApparelCompat.canApply.Invoke(null, new object[] { pawn });
				}
				catch (Exception ex)
				{
					SizedApparelCompat.broken = true;
					string text = "[TailorMade] Sized Apparel compat disabled (CanApplySizedApparel threw): ";
					Exception ex2 = ex;
					Log.WarningOnce(text + ((ex2 != null) ? ex2.ToString() : null), 2048023041);
					flag2 = false;
				}
			}
			return flag2;
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000E21C File Offset: 0x0000C41C
		public static bool BodyManaged(Pawn pawn)
		{
			bool flag = !SizedApparelCompat.ManagesPawn(pawn);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				try
				{
					flag2 = SizedApparelCompat.useBodyTexture == null || (bool)SizedApparelCompat.useBodyTexture.GetValue(null);
				}
				catch
				{
					flag2 = true;
				}
			}
			return flag2;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000E278 File Offset: 0x0000C478
		public static string ExpectedVanillaPath(Apparel apparel, BodyTypeDef bodyType)
		{
			string wornGraphicPath = apparel.WornGraphicPath;
			bool flag = GenText.NullOrEmpty(wornGraphicPath);
			string text;
			if (flag)
			{
				text = wornGraphicPath;
			}
			else
			{
				ApparelLayerDef lastLayer = apparel.def.apparel.LastLayer;
				bool flag2 = lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover || PawnRenderUtility.RenderAsPack(apparel) || wornGraphicPath == BaseContent.PlaceholderImagePath || wornGraphicPath == BaseContent.PlaceholderGearImagePath;
				if (flag2)
				{
					text = wornGraphicPath;
				}
				else
				{
					text = wornGraphicPath + "_" + bodyType.defName;
				}
			}
			return text;
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000E300 File Offset: 0x0000C500
		public static bool RecordForeignSwapped(Apparel apparel, BodyTypeDef bodyType, string recPath)
		{
			bool flag = !SizedApparelCompat.DeferEnabled;
			return !flag && recPath != SizedApparelCompat.ExpectedVanillaPath(apparel, bodyType);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000E330 File Offset: 0x0000C530
		public static bool DeferBody(Pawn pawn)
		{
			return SizedApparelCompat.DeferEnabled && SizedApparelCompat.BodyManaged(pawn);
		}

		// Token: 0x040000CC RID: 204
		public const string HarmonyId = "SizedApparelforRJW";

		// Token: 0x040000CD RID: 205
		private static bool resolved;

		// Token: 0x040000CE RID: 206
		private static bool broken;

		// Token: 0x040000CF RID: 207
		private static MethodInfo canApply;

		// Token: 0x040000D0 RID: 208
		private static FieldInfo useBodyTexture;
	}
}
