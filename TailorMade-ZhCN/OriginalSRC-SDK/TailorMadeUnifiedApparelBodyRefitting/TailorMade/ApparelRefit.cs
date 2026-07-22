using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000018 RID: 24
	public static class ApparelRefit
	{
		// Token: 0x0600008F RID: 143 RVA: 0x000094E8 File Offset: 0x000076E8
		private static bool AnyTexAt(string path)
		{
			return ContentFinder<Texture2D>.Get(path + "_south", false) != null || ContentFinder<Texture2D>.Get(path + "_north", false) != null || ContentFinder<Texture2D>.Get(path + "_east", false) != null || ContentFinder<Texture2D>.Get(path + "_west", false) != null || ContentFinder<Texture2D>.Get(path, false) != null;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00009570 File Offset: 0x00007770
		private static bool HasNativeBodytypeTex(string worn, BodyTypeDef bt)
		{
			string text = worn + "|" + bt.defName;
			bool flag2;
			bool flag = ApparelRefit.nativeTexMemo.TryGetValue(text, out flag2);
			bool flag3;
			if (flag)
			{
				flag3 = flag2;
			}
			else
			{
				flag2 = ApparelRefit.AnyTexAt(worn + "_" + bt.defName);
				ApparelRefit.nativeTexMemo[text] = flag2;
				flag3 = flag2;
			}
			return flag3;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x000095CE File Offset: 0x000077CE
		public static void ClearMemo()
		{
			ApparelRefit.nativeTexMemo.Clear();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000095DB File Offset: 0x000077DB
		private static IEnumerable<BodyTypeDef> FallbackOrder()
		{
			return new ApparelRefit.<FallbackOrder>d__4(-2);
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000095E4 File Offset: 0x000077E4
		private static Shader ShaderFor(Apparel apparel)
		{
			ThingStyleDef styleDef = apparel.StyleDef;
			bool flag = ((styleDef != null) ? styleDef.graphicData.shaderType : null) != null;
			Shader shader;
			if (flag)
			{
				shader = apparel.StyleDef.graphicData.shaderType.Shader;
			}
			else
			{
				bool flag2 = (apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask);
				if (flag2)
				{
					shader = ShaderDatabase.CutoutComplex;
				}
				else
				{
					shader = ShaderDatabase.Cutout;
				}
			}
			return shader;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00009670 File Offset: 0x00007870
		public static bool TryHandleMissingTexture(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec, out bool appRefitPending)
		{
			appRefitPending = false;
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled || !settings.autoFit;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3;
				if (apparel == null)
				{
					flag3 = null != null;
				}
				else
				{
					ThingDef def = apparel.def;
					flag3 = ((def != null) ? def.apparel : null) != null;
				}
				bool flag4 = !flag3 || bodyType == null;
				if (flag4)
				{
					flag2 = false;
				}
				else
				{
					bool ignore = ApparelClassifier.Info(apparel.def).ignore;
					if (ignore)
					{
						flag2 = false;
					}
					else
					{
						string wornGraphicPath = apparel.WornGraphicPath;
						bool flag5 = GenText.NullOrEmpty(wornGraphicPath);
						if (flag5)
						{
							flag2 = false;
						}
						else
						{
							ApparelLayerDef lastLayer = apparel.def.apparel.LastLayer;
							bool flag6 = lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover;
							if (flag6)
							{
								flag2 = false;
							}
							else
							{
								bool flag7 = PawnRenderUtility.RenderAsPack(apparel);
								if (flag7)
								{
									flag2 = false;
								}
								else
								{
									bool flag8 = wornGraphicPath == BaseContent.PlaceholderImagePath || wornGraphicPath == BaseContent.PlaceholderGearImagePath;
									if (flag8)
									{
										flag2 = false;
									}
									else
									{
										bool flag9 = ApparelRefit.HasNativeBodytypeTex(wornGraphicPath, bodyType);
										if (flag9)
										{
											flag2 = false;
										}
										else
										{
											BodyTypeDef bodyTypeDef = null;
											string text = null;
											foreach (BodyTypeDef bodyTypeDef2 in ApparelRefit.FallbackOrder())
											{
												bool flag10 = bodyTypeDef2 == bodyType;
												if (!flag10)
												{
													string text2 = wornGraphicPath + "_" + bodyTypeDef2.defName;
													bool flag11 = ApparelRefit.AnyTexAt(text2);
													if (flag11)
													{
														bodyTypeDef = bodyTypeDef2;
														text = text2;
														break;
													}
												}
											}
											bool flag12 = text == null && ApparelRefit.AnyTexAt(wornGraphicPath);
											if (flag12)
											{
												bodyTypeDef = bodyType;
												text = wornGraphicPath;
											}
											bool flag13 = text == null;
											if (flag13)
											{
												flag2 = false;
											}
											else
											{
												Shader shader = ApparelRefit.ShaderFor(apparel);
												Pawn wearer = apparel.Wearer;
												ResolvedPattern resolvedPattern = null;
												bool flag14;
												if (wearer == null)
												{
													flag14 = null != null;
												}
												else
												{
													ThingDef def2 = wearer.def;
													flag14 = ((def2 != null) ? def2.race : null) != null;
												}
												bool flag15 = flag14 && wearer.def.race.Humanlike && settings.LayerEnabled(lastLayer) && settings.BodyTypeEnabled(bodyType);
												if (flag15)
												{
													resolvedPattern = PatternRegistry.Resolve(wearer.def, bodyType, lastLayer, apparel.def, FemaleBodyVariantsCompat.FemaleBody(wearer));
												}
												bool flag16 = PaperPatternCompat.Active && PaperPatternCompat.WillHandle(apparel, bodyType);
												bool flag17 = resolvedPattern != null && !flag16;
												if (flag17)
												{
													ResolvedPattern canvasVariant = PatternRegistry.GetCanvasVariant(resolvedPattern, bodyTypeDef);
													string text3 = (PerPawnAdjust.Has(wearer, apparel.def.defName) ? wearer.thingIDNumber.ToString() : "");
													Graphic graphic = GraphicDatabase.Get<Graphic_TailorMade>(string.Concat(new string[]
													{
														text,
														"\0",
														canvasVariant.Key,
														"\0",
														apparel.def.defName,
														"\0",
														text3
													}), shader, apparel.def.graphicData.drawSize, apparel.DrawColor, apparel.DrawColorTwo, null, null);
													rec = new ApparelGraphicRecord(graphic, apparel);
													flag2 = true;
												}
												else
												{
													Graphic graphic2 = GraphicDatabase.Get<Graphic_Multi>(text, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
													rec = new ApparelGraphicRecord(graphic2, apparel);
													appRefitPending = flag16 && !PaperPatternCompat.DeferEnabled;
													flag2 = true;
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
			return flag2;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x000099E0 File Offset: 0x00007BE0
		private static ApparelLayerDef PickFitLayer(List<ApparelLayerDef> layers, TailorMadeSettings s)
		{
			bool flag = layers == null;
			ApparelLayerDef apparelLayerDef;
			if (flag)
			{
				apparelLayerDef = null;
			}
			else
			{
				for (int i = layers.Count - 1; i >= 0; i--)
				{
					ApparelLayerDef apparelLayerDef2 = layers[i];
					bool flag2 = apparelLayerDef2 == null || !TailorMadeSettings.IsRefitableLayer(apparelLayerDef2);
					if (!flag2)
					{
						bool flag3 = !s.LayerEnabled(apparelLayerDef2);
						if (!flag3)
						{
							return apparelLayerDef2;
						}
					}
				}
				apparelLayerDef = null;
			}
			return apparelLayerDef;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00009A54 File Offset: 0x00007C54
		public static void TrySwap(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled;
			if (!flag)
			{
				bool flag2;
				if (apparel == null)
				{
					flag2 = null != null;
				}
				else
				{
					ThingDef def = apparel.def;
					flag2 = ((def != null) ? def.apparel : null) != null;
				}
				bool flag3 = !flag2 || bodyType == null;
				if (!flag3)
				{
					bool flag4 = rec.graphic == null || GenText.NullOrEmpty(rec.graphic.path);
					if (!flag4)
					{
						Pawn wearer = apparel.Wearer;
						bool flag5;
						if (wearer == null)
						{
							flag5 = null != null;
						}
						else
						{
							ThingDef def2 = wearer.def;
							flag5 = ((def2 != null) ? def2.race : null) != null;
						}
						bool flag6 = !flag5 || !wearer.def.race.Humanlike;
						if (!flag6)
						{
							bool ignore = ApparelClassifier.Info(apparel.def).ignore;
							if (!ignore)
							{
								bool flag7 = PawnRenderUtility.RenderAsPack(apparel);
								if (!flag7)
								{
									ApparelLayerDef apparelLayerDef = ApparelRefit.PickFitLayer(apparel.def.apparel.layers, settings);
									bool flag8 = apparelLayerDef == null;
									if (!flag8)
									{
										bool flag9 = !settings.BodyTypeEnabled(bodyType);
										if (!flag9)
										{
											bool flag10 = rec.graphic is Graphic_TailorMade;
											if (!flag10)
											{
												bool flag11 = PaperPatternCompat.DeferApparel(rec.graphic);
												if (!flag11)
												{
													bool flag12 = SizedApparelCompat.RecordForeignSwapped(apparel, bodyType, rec.graphic.path);
													if (!flag12)
													{
														ResolvedPattern resolvedPattern = PatternRegistry.Resolve(wearer.def, bodyType, apparelLayerDef, apparel.def, FemaleBodyVariantsCompat.FemaleBody(wearer));
														bool flag13 = resolvedPattern == null;
														if (!flag13)
														{
															bool flag14 = !settings.autoFit && resolvedPattern.Def == null;
															if (!flag14)
															{
																string text = (PerPawnAdjust.Has(wearer, apparel.def.defName) ? wearer.thingIDNumber.ToString() : "");
																string text2 = string.Concat(new string[]
																{
																	rec.graphic.path,
																	"\0",
																	resolvedPattern.Key,
																	"\0",
																	apparel.def.defName,
																	"\0",
																	text
																});
																Graphic graphic = GraphicDatabase.Get<Graphic_TailorMade>(text2, rec.graphic.Shader, apparel.def.graphicData.drawSize, apparel.DrawColor, apparel.DrawColorTwo, null, rec.graphic.maskPath);
																rec = new ApparelGraphicRecord(graphic, apparel);
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

		// Token: 0x04000071 RID: 113
		private static readonly Dictionary<string, bool> nativeTexMemo = new Dictionary<string, bool>();
	}
}
