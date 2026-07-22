using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000017 RID: 23
	[HarmonyPatch]
	internal static class Patch_TryGetGraphicApparel
	{
		// Token: 0x0600008C RID: 140 RVA: 0x00009350 File Offset: 0x00007550
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel", new Type[]
			{
				typeof(Apparel),
				typeof(BodyTypeDef),
				typeof(bool),
				typeof(ApparelGraphicRecord).MakeByRefType()
			}, null);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000093B8 File Offset: 0x000075B8
		[HarmonyPrefix]
		private static bool Prefix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec, ref bool __result, ref bool __state)
		{
			__state = false;
			bool flag;
			if (forStatue)
			{
				flag = true;
			}
			else
			{
				try
				{
					bool flag3;
					bool flag2 = ApparelRefit.TryHandleMissingTexture(apparel, bodyType, ref rec, out flag3);
					if (flag2)
					{
						__state = !flag3;
						__result = true;
						return false;
					}
				}
				catch (Exception ex)
				{
					string text = "[TailorMade] Apparel texture fallback failed for ";
					string text2;
					if (apparel == null)
					{
						text2 = null;
					}
					else
					{
						ThingDef def = apparel.def;
						text2 = ((def != null) ? def.defName : null);
					}
					string text3 = text2 ?? "null";
					string text4 = ": ";
					Exception ex2 = ex;
					Log.ErrorOnce(text + text3 + text4 + ((ex2 != null) ? ex2.ToString() : null), 2048043419);
				}
				flag = true;
			}
			return flag;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x0000945C File Offset: 0x0000765C
		[HarmonyPostfix]
		[HarmonyAfter(new string[] { "SizedApparelforRJW", "rimworld.Nals.ApparelPaperPattern" })]
		[HarmonyPriority(200)]
		private static void Postfix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec, bool __result, bool __state)
		{
			bool flag = !__result || forStatue || __state;
			if (!flag)
			{
				try
				{
					ApparelRefit.TrySwap(apparel, bodyType, ref rec);
				}
				catch (Exception ex)
				{
					string text = "[TailorMade] Apparel refit failed for ";
					string text2;
					if (apparel == null)
					{
						text2 = null;
					}
					else
					{
						ThingDef def = apparel.def;
						text2 = ((def != null) ? def.defName : null);
					}
					string text3 = text2 ?? "null";
					string text4 = ": ";
					Exception ex2 = ex;
					Log.ErrorOnce(text + text3 + text4 + ((ex2 != null) ? ex2.ToString() : null), 2048043418);
				}
			}
		}
	}
}
