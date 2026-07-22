using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000016 RID: 22
	[HarmonyPatch(typeof(PawnRenderNodeWorker), "LayerFor")]
	internal static class Patch_RenderOrder
	{
		// Token: 0x0600008B RID: 139 RVA: 0x0000921C File Offset: 0x0000741C
		[HarmonyPostfix]
		private static void Postfix(PawnRenderNode node, ref float __result)
		{
			Apparel apparel = ((node != null) ? node.apparel : null);
			bool flag = ((apparel != null) ? apparel.def : null) == null;
			if (!flag)
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				bool flag2 = settings == null || !settings.enabled;
				if (!flag2)
				{
					float num = 0f;
					PawnRenderTree tree = node.tree;
					TailorAdjust tailorAdjust = PerPawnAdjust.Effective((tree != null) ? tree.pawn : null, apparel.def.defName);
					bool flag3 = tailorAdjust != null;
					if (flag3)
					{
						num = tailorAdjust.renderOrder;
					}
					bool flag4 = num == 0f && PatternRegistry.HasBehaviorDefs;
					if (flag4)
					{
						num = PatternRegistry.ShippedBehavior(apparel.def.defName).Item1;
					}
					bool flag5 = num != 0f;
					if (flag5)
					{
						__result += num;
						ApparelLayerDef lastLayer = apparel.def.apparel.LastLayer;
						bool flag6 = lastLayer != ApparelLayerDefOf.Overhead && lastLayer != ApparelLayerDefOf.EyeCover;
						if (flag6)
						{
							bool flag7 = __result > 49f;
							if (flag7)
							{
								__result = 49f;
							}
							else
							{
								bool flag8 = __result < 11f;
								if (flag8)
								{
									__result = 11f;
								}
							}
						}
					}
				}
			}
		}
	}
}
