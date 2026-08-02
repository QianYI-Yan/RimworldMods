using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000012 RID: 18
	[HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
	internal static class Patch_AdjustParms_HairFix
	{
		// Token: 0x0600007D RID: 125 RVA: 0x0000818C File Offset: 0x0000638C
		[HarmonyPostfix]
		private static void Postfix(PawnRenderTree __instance, ref PawnDrawParms parms)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled || !settings.keepHairUnderBodyApparel;
			if (!flag)
			{
				Pawn pawn = __instance.pawn;
				bool flag2 = ((pawn != null) ? pawn.apparel : null) == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike;
				if (!flag2)
				{
					bool flag3 = false;
					bool flag4 = false;
					bool flag5 = false;
					foreach (Apparel apparel in pawn.apparel.WornApparel)
					{
						ThingDef def = apparel.def;
						ApparelProperties apparelProperties = ((def != null) ? def.apparel : null);
						bool flag6 = ((apparelProperties != null) ? apparelProperties.bodyPartGroups : null) == null;
						if (!flag6)
						{
							bool flag7 = settings.hairForceItems != null && settings.hairForceItems.Contains(apparel.def.defName);
							if (!flag7)
							{
								bool flag8 = PatternRegistry.HasBehaviorDefs && PatternRegistry.ShippedBehavior(apparel.def.defName).Item3;
								if (!flag8)
								{
									bool flag9 = apparelProperties.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) || apparelProperties.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs);
									if (!flag9)
									{
										bool flag10 = apparelProperties.renderSkipFlags != null;
										if (flag10)
										{
											foreach (RenderSkipFlagDef renderSkipFlagDef in apparelProperties.renderSkipFlags)
											{
												bool flag11 = renderSkipFlagDef == RenderSkipFlagDefOf.Hair;
												if (flag11)
												{
													flag3 = true;
												}
												else
												{
													bool flag12 = renderSkipFlagDef == RenderSkipFlagDefOf.Beard;
													if (flag12)
													{
														flag4 = true;
													}
													else
													{
														bool flag13 = renderSkipFlagDef == RenderSkipFlagDefOf.Eyes;
														if (flag13)
														{
															flag5 = true;
														}
													}
												}
											}
										}
										else
										{
											bool flag14 = apparelProperties.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead);
											if (flag14)
											{
												flag3 = true;
											}
											bool flag15 = apparelProperties.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead);
											if (flag15)
											{
												flag3 = true;
												flag4 = true;
												flag5 = true;
											}
										}
									}
								}
							}
						}
					}
					bool flag16 = !flag3;
					if (flag16)
					{
						parms.skipFlags &= ~RenderSkipFlagDefOf.Hair;
					}
					bool flag17 = !flag4;
					if (flag17)
					{
						parms.skipFlags &= ~RenderSkipFlagDefOf.Beard;
					}
					bool flag18 = !flag5;
					if (flag18)
					{
						parms.skipFlags &= ~RenderSkipFlagDefOf.Eyes;
					}
				}
			}
		}
	}
}
