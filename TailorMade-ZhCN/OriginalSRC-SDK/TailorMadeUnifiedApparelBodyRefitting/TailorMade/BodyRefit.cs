using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200001C RID: 28
	public static class BodyRefit
	{
		// Token: 0x0600009E RID: 158 RVA: 0x00009F5C File Offset: 0x0000815C
		public static void TrySwap(Pawn pawn, ref Graphic __result)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled;
			if (!flag)
			{
				bool flag2 = __result == null || __result is Graphic_TailorBody;
				if (!flag2)
				{
					object obj;
					if (pawn == null)
					{
						obj = null;
					}
					else
					{
						Pawn_StoryTracker story = pawn.story;
						obj = ((story != null) ? story.bodyType : null);
					}
					bool flag3 = obj == null;
					if (!flag3)
					{
						Pawn_DrawTracker drawer = pawn.Drawer;
						bool flag4 = ((drawer != null) ? drawer.renderer : null) != null && pawn.Drawer.renderer.CurRotDrawMode == 4;
						if (!flag4)
						{
							bool flag5 = pawn.IsMutant || pawn.IsCreepJoiner;
							if (!flag5)
							{
								BodyTypeDef bodyType = pawn.story.bodyType;
								bool flag6 = HarSupport.IsAlienRace(pawn.def) && HarCompat.Engaged(pawn.def);
								bool flag7 = FemaleBodyVariantsCompat.FemaleBody(pawn);
								HarBodyMode harBodyMode;
								string text;
								bool flag8 = settings.RaceBodyOverride(pawn.def, out harBodyMode, out text);
								bool flag9 = !flag8 && SizedApparelCompat.DeferBody(pawn);
								if (!flag9)
								{
									bool flag10 = !flag6;
									if (flag10)
									{
										bool flag11 = flag8 && harBodyMode == HarBodyMode.ForceCB2 && !GenText.NullOrEmpty(text);
										bool flag12 = !flag11;
										if (flag12)
										{
											bool flag13 = flag7 && pawn.def.race != null && pawn.def.race.Humanlike && settings.BodyTypeEnabled(bodyType);
											if (flag13)
											{
												string bodyNakedGraphicPath = bodyType.bodyNakedGraphicPath;
												bool flag14 = !GenText.NullOrEmpty(bodyNakedGraphicPath) && __result.path == bodyNakedGraphicPath;
												if (flag14)
												{
													string text2 = FemaleBodyVariantsCompat.FemaleVariant(bodyNakedGraphicPath);
													bool flag15 = text2 != null;
													if (flag15)
													{
														__result = GraphicDatabase.Get<Graphic_Multi>(text2, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, null);
													}
												}
											}
										}
										else
										{
											bool flag16 = pawn.def.race == null || !pawn.def.race.Humanlike;
											if (!flag16)
											{
												bool flag17 = !settings.BodyTypeEnabled(bodyType);
												if (!flag17)
												{
													string bodyNakedGraphicPath2 = bodyType.bodyNakedGraphicPath;
													bool flag18 = GenText.NullOrEmpty(bodyNakedGraphicPath2);
													if (!flag18)
													{
														string text3 = (flag7 ? FemaleBodyVariantsCompat.FemaleVariant(bodyNakedGraphicPath2) : null);
														bool flag19 = __result.path != bodyNakedGraphicPath2 && (text3 == null || __result.path != text3);
														if (!flag19)
														{
															BodyTexCache.Entry entry = BodyTexCache.Get(pawn.def, bodyType, flag7);
															bool flag20 = entry == null || !entry.valid;
															if (!flag20)
															{
																string text4 = string.Concat(new string[]
																{
																	bodyNakedGraphicPath2,
																	"\0",
																	pawn.def.defName,
																	"|",
																	bodyType.defName,
																	flag7 ? "|f" : ""
																});
																__result = GraphicDatabase.Get<Graphic_TailorBody>(text4, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, null);
															}
														}
													}
												}
											}
										}
									}
									else
									{
										string text5 = HarSupport.TryGetBodyPath(pawn.def, bodyType);
										string bodyNakedGraphicPath3 = bodyType.bodyNakedGraphicPath;
										bool flag21 = GenText.NullOrEmpty(text5) || GenText.NullOrEmpty(bodyNakedGraphicPath3) || text5 == bodyNakedGraphicPath3;
										if (!flag21)
										{
											bool flag22 = settings.BodyTypeEnabled(bodyType) && HarCompat.For(pawn.def).CanFit(bodyType);
											bool flag23 = harBodyMode == HarBodyMode.ForceCB2 && flag22;
											if (flag23)
											{
												bool flag24 = !GenText.NullOrEmpty(text);
												if (flag24)
												{
													BodyTexCache.Entry entry2 = BodyTexCache.Get(pawn.def, bodyType, flag7);
													bool flag25 = entry2 != null && entry2.valid;
													if (flag25)
													{
														string text6 = string.Concat(new string[]
														{
															bodyNakedGraphicPath3,
															"\0",
															pawn.def.defName,
															"|",
															bodyType.defName,
															flag7 ? "|f" : ""
														});
														__result = GraphicDatabase.Get<Graphic_TailorBody>(text6, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, null);
														return;
													}
												}
												string text7 = (flag7 ? (FemaleBodyVariantsCompat.FemaleVariant(bodyNakedGraphicPath3) ?? bodyNakedGraphicPath3) : bodyNakedGraphicPath3);
												__result = GraphicDatabase.Get<Graphic_Multi>(text7, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, null);
											}
											else
											{
												bool flag26 = harBodyMode == HarBodyMode.AutoResize && flag22;
												if (flag26)
												{
													BodyTexCache.Entry entry3 = BodyTexCache.Get(pawn.def, bodyType, flag7);
													bool flag27 = entry3 != null && entry3.valid;
													if (flag27)
													{
														string text8 = string.Concat(new string[]
														{
															text5,
															"\0",
															pawn.def.defName,
															"|",
															bodyType.defName,
															flag7 ? "|f" : ""
														});
														__result = GraphicDatabase.Get<Graphic_TailorBody>(text8, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, null);
														return;
													}
												}
												string text9 = (flag7 ? FemaleBodyVariantsCompat.FemaleVariant(bodyNakedGraphicPath3) : null);
												bool flag28 = __result.path == bodyNakedGraphicPath3 || (text9 != null && __result.path == text9);
												if (flag28)
												{
													string text10 = (flag7 ? (FemaleBodyVariantsCompat.FemaleVariant(text5) ?? text5) : text5);
													Log.WarningOnce(string.Concat(new string[]
													{
														"[TailorMade] ",
														pawn.def.defName,
														" body graphic resolved to the vanilla path '",
														__result.path,
														"' (",
														__result.GetType().Name,
														") — HAR's body prefix was bypassed for this pawn (or another body mod redirected it). Restoring the race's own body art '",
														text10,
														"'."
													}), (int)pawn.def.shortHash ^ 2047979729);
													__result = GraphicDatabase.Get<Graphic_Multi>(text10, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, null);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x0600009F RID: 159 RVA: 0x0000A590 File Offset: 0x00008790
		public static void TrySwapHead(Pawn pawn, ref Graphic __result)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled;
			if (!flag)
			{
				HarBodyMode harBodyMode;
				string text;
				bool flag2 = settings.RaceBodyOverride((pawn != null) ? pawn.def : null, out harBodyMode, out text);
				bool flag3 = harBodyMode != HarBodyMode.AutoResize;
				if (!flag3)
				{
					bool flag4 = !flag2 && SizedApparelCompat.DeferBody(pawn);
					if (!flag4)
					{
						bool flag5 = __result == null || __result is Graphic_TailorHead;
						if (!flag5)
						{
							bool flag6 = GenText.NullOrEmpty(__result.path);
							if (!flag6)
							{
								object obj;
								if (pawn == null)
								{
									obj = null;
								}
								else
								{
									Pawn_StoryTracker story = pawn.story;
									obj = ((story != null) ? story.bodyType : null);
								}
								bool flag7 = obj == null;
								if (!flag7)
								{
									Pawn_DrawTracker drawer = pawn.Drawer;
									bool flag8 = ((drawer != null) ? drawer.renderer : null) != null && pawn.Drawer.renderer.CurRotDrawMode == 4;
									if (!flag8)
									{
										bool flag9 = pawn.IsMutant || pawn.IsCreepJoiner;
										if (!flag9)
										{
											bool flag10 = !HarSupport.IsAlienRace(pawn.def);
											if (!flag10)
											{
												bool flag11 = !HarCompat.Engaged(pawn.def);
												if (!flag11)
												{
													BodyTypeDef bodyType = pawn.story.bodyType;
													bool flag12 = !settings.BodyTypeEnabled(bodyType);
													if (!flag12)
													{
														bool flag13 = !HarCompat.For(pawn.def).CanFit(bodyType);
														if (!flag13)
														{
															bool flag14 = FemaleBodyVariantsCompat.FemaleBody(pawn);
															BodyTexCache.Entry entry = BodyTexCache.Get(pawn.def, bodyType, flag14);
															bool flag15 = entry == null || !entry.valid || !entry.customAlienBody;
															if (!flag15)
															{
																bool flag16 = Mathf.Abs(entry.headScale - 1f) < 0.05f;
																if (!flag16)
																{
																	string text2 = string.Concat(new string[]
																	{
																		__result.path,
																		"\0",
																		pawn.def.defName,
																		"|",
																		bodyType.defName,
																		flag14 ? "|f" : ""
																	});
																	__result = GraphicDatabase.Get<Graphic_TailorHead>(text2, __result.Shader, __result.drawSize, __result.color, __result.colorTwo, null, __result.maskPath);
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
