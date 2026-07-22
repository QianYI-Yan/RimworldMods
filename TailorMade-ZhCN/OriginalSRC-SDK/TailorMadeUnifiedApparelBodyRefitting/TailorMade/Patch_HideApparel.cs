using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000015 RID: 21
	[HarmonyPatch(typeof(PawnRenderNodeWorker), "CanDrawNow")]
	internal static class Patch_HideApparel
	{
		// Token: 0x0600008A RID: 138 RVA: 0x0000916C File Offset: 0x0000736C
		[HarmonyPostfix]
		private static void Postfix(PawnRenderNode node, ref bool __result)
		{
			bool flag = !__result;
			if (!flag)
			{
				Apparel apparel = ((node != null) ? node.apparel : null);
				bool flag2 = ((apparel != null) ? apparel.def : null) == null;
				if (!flag2)
				{
					TailorMadeSettings settings = TailorMadeMod.Settings;
					bool flag3 = settings == null || !settings.enabled;
					if (!flag3)
					{
						bool flag4 = settings.hiddenRenderDefs != null && settings.hiddenRenderDefs.Contains(apparel.def.defName);
						bool flag5 = !flag4 && PatternRegistry.HasBehaviorDefs;
						if (flag5)
						{
							flag4 = PatternRegistry.ShippedBehavior(apparel.def.defName).Item2;
						}
						bool flag6 = flag4;
						if (flag6)
						{
							__result = false;
						}
					}
				}
			}
		}
	}
}
