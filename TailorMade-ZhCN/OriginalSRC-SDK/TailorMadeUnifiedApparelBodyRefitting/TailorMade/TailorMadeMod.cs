using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000022 RID: 34
	public class TailorMadeMod : Mod
	{
		// Token: 0x060000B6 RID: 182 RVA: 0x0000BA54 File Offset: 0x00009C54
		public TailorMadeMod(ModContentPack content)
			: base(content)
		{
			TailorMadeMod.Settings = base.GetSettings<TailorMadeSettings>();
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x0000BA75 File Offset: 0x00009C75
		public override string SettingsCategory()
		{
			return "TailorMade";
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x0000BA7C File Offset: 0x00009C7C
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Rect rect = GenUI.ContractedBy(inRect, 4f);
			int num = 0;
			foreach (ApparelLayerDef apparelLayerDef in DefDatabase<ApparelLayerDef>.AllDefsListForReading)
			{
				bool flag = TailorMadeSettings.IsRefitableLayer(apparelLayerDef);
				if (flag)
				{
					num++;
				}
			}
			float num2 = ((TailorMadeMod.Settings.harBodyMode == HarBodyMode.ForceCB2) ? ((float)(TexBake.BodyProviders().Count + 2) * 30f + 30f) : 0f);
			float num3 = (float)TailorMadeMod.RaceList().Count * 28f + 60f;
			Rect rect2;
			rect2..ctor(0f, 0f, rect.width - 20f, 1340f + num2 + num3 + (float)(DefDatabase<BodyTypeDef>.AllDefsListForReading.Count + num) * 28f);
			Widgets.BeginScrollView(rect, ref this.scroll, rect2, true);
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(rect2);
			listing_Standard.CheckboxLabeled("Enabled", ref TailorMadeMod.Settings.enabled, "Master switch. Disable to render all apparel vanilla-style.", 0f, 1f);
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Fitting", -1f, null);
			listing_Standard.CheckboxLabeled("Automatic mask fitting", ref TailorMadeMod.Settings.autoFit, "Derive offsets and scaling from the apparel art and body silhouette automatically so apparel always fills the body mask. Disable to only use explicit TailorPatternDefs.", 0f, 1f);
			listing_Standard.CheckboxLabeled("Preserve aspect ratio", ref TailorMadeMod.Settings.uniformScale, "On: uniform scaling (art may overflow the mask slightly on one axis, gets clipped).\nOff: stretch each axis so the art exactly fills the mask.", 0f, 1f);
			listing_Standard.Label("Max baked texture resolution: " + TailorMadeMod.Settings.maxResolution.ToString(), -1f, null);
			TailorMadeMod.Settings.maxResolution = TailorMadeMod.RoundPow2(Mathf.RoundToInt(listing_Standard.Slider((float)TailorMadeMod.Settings.maxResolution, 128f, 2048f)));
			listing_Standard.CheckboxLabeled("Trilinear filtering", ref TailorMadeMod.Settings.trilinearFilter, "Smoother scaling of baked textures. Slightly blurrier up close.", 0f, 1f);
			listing_Standard.CheckboxLabeled("Unlock texture resolution", ref TailorMadeMod.Settings.unlockResolution, "Bake apparel at its native texture resolution instead of clamping to the slider above. Preserves full detail from high-resolution gear mods (which the cap would otherwise downscale). Uses more VRAM and a longer first bake.", 0f, 1f);
			listing_Standard.CheckboxLabeled("Sharp resampling (bicubic)", ref TailorMadeMod.Settings.sharpResampling, "Bicubic instead of bilinear sampling when art is scaled up — visibly crisper edges. Costs noticeably more CPU on the one-time bake. No substitute for higher-res source art (there is no AI upscaling in-engine).", 0f, 1f);
			listing_Standard.CheckboxLabeled("Unlock body texture resolution", ref TailorMadeMod.Settings.bodyUnlockResolution, "Bake body art (auto-resized alien bodies, forced body-mod textures, scaled heads) at its native resolution instead of clamping to the slider above — the same unlock as apparel, but for bodies. Keeps high-resolution body packs crisp. Uses more VRAM and a longer first bake.", 0f, 1f);
			listing_Standard.CheckboxLabeled("Conform side views (east/west)", ref TailorMadeMod.Settings.fitSideViews, "Fit apparel to the body on the east/west facings too, automatically (on by default). Uses a stable bounding-box fit on the sides (no per-row warp). Turn off for exactly-vanilla side rendering. Front/back are always refitted.", 0f, 1f);
			listing_Standard.CheckboxLabeled("Outline refit apparel", ref TailorMadeMod.Settings.bodyMaskOutline, "Draw a thin dark outline around masked apparel so it matches the body's stylized outline. On by default.", 0f, 1f);
			bool bodyMaskOutline = TailorMadeMod.Settings.bodyMaskOutline;
			if (bodyMaskOutline)
			{
				listing_Standard.Label("Outline thickness: " + TailorMadeMod.Settings.outlinePixels.ToString() + "px", -1f, null);
				TailorMadeMod.Settings.outlinePixels = Mathf.RoundToInt(listing_Standard.Slider((float)TailorMadeMod.Settings.outlinePixels, 1f, 6f));
				listing_Standard.Label("Side (east/west) outline boost: x" + TailorMadeMod.Settings.sideOutlineBoost.ToString("0.0"), -1f, null);
				TailorMadeMod.Settings.sideOutlineBoost = Mathf.Round(listing_Standard.Slider(TailorMadeMod.Settings.sideOutlineBoost, 1f, 3f) * 10f) / 10f;
			}
			listing_Standard.CheckboxLabeled("Force Female body type on female pawns", ref TailorMadeMod.Settings.forceFemaleBody, "Adult female humanlikes always use the Female body type, CB2's primary silhouette. Applies to newly generated pawns immediately and to existing pawns on load / settings change. HAR races are not affected.", 0f, 1f);
			bool flag2 = listing_Standard.ButtonTextLabeled("Clip apparel to body silhouette", TailorMadeMod.Settings.defaultClipMode.ToString(), 0, null, null);
			if (flag2)
			{
				TailorMadeMod.Settings.defaultClipMode = (TailorMadeMod.Settings.defaultClipMode + 1) % (ClipMode)3;
			}
			listing_Standard.Label("    Auto (recommended) clips a body type only when a body retexture is detected for it — so Male starts clipping automatically once a male body mod (Nal, CB2, WrelicK, ...) is active. Set per-body overrides in the Body types list below.", -1f, null);
			listing_Standard.CheckboxLabeled("Keep hair visible under body apparel", ref TailorMadeMod.Settings.keepHairUnderBodyApparel, "Stops full-body suits and armor (common in HAR races) from blanking hair, beard and eyes just because they list head body-part-groups for damage coverage. Only genuinely head-worn items (hats, helmets, masks that don't also cover the torso/legs) hide hair.", 0f, 1f);
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Apparel detection (THIGAPPE-style)", -1f, null);
			listing_Standard.CheckboxLabeled("Detect apparel coverage", ref TailorMadeMod.Settings.thigappeDetect, "Classify apparel by what it covers (boots, pants, bodysuits, armor) using THIGAPPE's detection rules and any THIGAPPE_ tags. Partial garments are then fit to their body region instead of the whole silhouette, so boot and pants textures land in the right place. On by default.", 0f, 1f);
			bool thigappeDetect = TailorMadeMod.Settings.thigappeDetect;
			if (thigappeDetect)
			{
				listing_Standard.CheckboxLabeled("    Boots (fit to the feet)", ref TailorMadeMod.Settings.detectBoots, "Footwear that only covers the feet is fit to the feet region of the body instead of being stretched over the whole silhouette.", 0f, 1f);
				listing_Standard.CheckboxLabeled("    Pants (fit to the legs)", ref TailorMadeMod.Settings.detectPants, "Legwear that only covers the legs is fit to the legs region of the body instead of being stretched over the whole silhouette.", 0f, 1f);
				listing_Standard.CheckboxLabeled("    Chest pieces (fit to the torso)", ref TailorMadeMod.Settings.detectChest, "Torso-only armor and tops whose texture is drawn just for the chest (breastplates, cuirasses, tunics from mods like Medieval Overhaul) are fit to the upper-body region instead of being stretched over the whole silhouette. Only kicks in when the worn texture is actually torso-only; long coats and capes that drape down the body are left full-body.", 0f, 1f);
				listing_Standard.CheckboxLabeled("    Bodysuits (full-body, on skin)", ref TailorMadeMod.Settings.detectBodysuit, "Torso-and-legs skinsuits are treated as full-body garments (whole-body fit).", 0f, 1f);
				listing_Standard.CheckboxLabeled("    Armor (full-body protective)", ref TailorMadeMod.Settings.detectArmor, "Torso-and-legs armor and protective suits are treated as full-body garments (whole-body fit).", 0f, 1f);
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Sized Apparel for RJW" + (SizedApparelCompat.Active ? "" : " (not loaded)"), -1f, null);
			bool active = SizedApparelCompat.Active;
			if (active)
			{
				listing_Standard.CheckboxLabeled("Defer to Sized Apparel", ref TailorMadeMod.Settings.sizedApparelDefer, "Apparel that Sized Apparel resizes, and bodies it manages, are left entirely to it — its hand-drawn size variants and body-part rendering stay untouched. TailorMade still fits everything Sized Apparel doesn't cover: unsupported apparel, races it skips, and gear with missing body-type textures. Turning this off lets TailorMade re-bake Sized Apparel's swapped art too (not recommended — it distorts art hand-aligned to Sized Apparel's bodies).", 0f, 1f);
			}
			else
			{
				GUI.color = new Color(0.6f, 0.6f, 0.6f);
				listing_Standard.Label("    Sized Apparel (OTYOTY.SizedApparel) is not active — nothing to defer to.", -1f, null);
				GUI.color = Color.white;
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Apparel Paper Pattern" + (PaperPatternCompat.Active ? "" : " (not loaded)"), -1f, null);
			bool active2 = PaperPatternCompat.Active;
			if (active2)
			{
				listing_Standard.CheckboxLabeled("Defer to Apparel Paper Pattern", ref TailorMadeMod.Settings.paperPatternDefer, "Apparel that APP re-renders through its pattern defs (including THIGAPPE's pattern packs) is left entirely to it. TailorMade still fits everything APP doesn't cover: races and layers with no pattern def, and apparel with missing body-type textures (which APP can't handle). To hand a specific pawn/apparel combination to TailorMade instead, exempt it in APP's own tuner — exempted items are picked up by TailorMade automatically. Turning this off makes TailorMade re-fit APP's apparel from the original art too (APP still spends VRAM rendering its version first — prefer exempting items in APP's tuner instead).", 0f, 1f);
				bool tunerAvailable = PaperPatternCompat.TunerAvailable;
				if (tunerAvailable)
				{
					bool flag3 = Current.ProgramState == 2;
					if (flag3)
					{
						bool flag4 = listing_Standard.ButtonText("Open APP tuner (User Tuner)", null, 1f);
						if (flag4)
						{
							PaperPatternCompat.OpenTuner();
						}
					}
					else
					{
						GUI.color = new Color(0.6f, 0.6f, 0.6f);
						listing_Standard.Label("    APP's tuner opens in-game. There is also an 'APP tuner' button in the TailorMade editor window.", -1f, null);
						GUI.color = Color.white;
					}
				}
			}
			else
			{
				GUI.color = new Color(0.6f, 0.6f, 0.6f);
				listing_Standard.Label("    Apparel Paper Pattern (nalsnoir.ApparelPaperPattern) is not active — nothing to defer to.", -1f, null);
				GUI.color = Color.white;
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("FemaleBodyVariants" + (FemaleBodyVariantsCompat.Active ? "" : " (not loaded)"), -1f, null);
			bool active3 = FemaleBodyVariantsCompat.Active;
			if (active3)
			{
				listing_Standard.CheckboxLabeled("Fit apparel to female body variants", ref TailorMadeMod.Settings.femaleBodyVariants, "FemaleBodyVariants draws non-male pawns with '_Female' variant body textures (Naked_Thin_Female, Naked_Fat_Female, Naked_Hulk_Female) when a body texture mod ships them. With this on, TailorMade bakes apparel against that same female silhouette — and bodies TailorMade renders itself (forced providers, auto-resized alien races) prefer the provider's own female art too. Tip: with a body pack that ships these variants you can turn OFF 'Force Female body type' above and keep Thin/Fat/Hulk builds on female pawns.", 0f, 1f);
			}
			else
			{
				GUI.color = new Color(0.6f, 0.6f, 0.6f);
				listing_Standard.Label("    FemaleBodyVariants (tiagocc0.FemaleBodyVariants) is not active — female Thin/Fat/Hulk pawns use the unisex body art.", -1f, null);
				GUI.color = Color.white;
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Humanoid Alien Races" + (HarSupport.Active ? "" : " (HAR not loaded)"), -1f, null);
			bool flag5 = listing_Standard.RadioButton("Off — leave alien bodies untouched", TailorMadeMod.Settings.harBodyMode == HarBodyMode.Off, 0f, null, null);
			if (flag5)
			{
				TailorMadeMod.Settings.harBodyMode = HarBodyMode.Off;
			}
			bool flag6 = listing_Standard.RadioButton("Auto-resize — refit alien body art to fill the body-mod silhouette", TailorMadeMod.Settings.harBodyMode == HarBodyMode.AutoResize, 0f, null, null);
			if (flag6)
			{
				TailorMadeMod.Settings.harBodyMode = HarBodyMode.AutoResize;
			}
			bool flag7 = listing_Standard.RadioButton("Force body texture — replace alien bodies with an installed body texture mod", TailorMadeMod.Settings.harBodyMode == HarBodyMode.ForceCB2, 0f, null, null);
			if (flag7)
			{
				TailorMadeMod.Settings.harBodyMode = HarBodyMode.ForceCB2;
			}
			bool flag8 = TailorMadeMod.Settings.harBodyMode == HarBodyMode.ForceCB2;
			if (flag8)
			{
				List<ModContentPack> list = TexBake.BodyProviders();
				bool flag9 = list.Count == 0;
				if (flag9)
				{
					GUI.color = new Color(1f, 0.75f, 0.3f);
					listing_Standard.Label("    No body texture mod detected — forcing would just use the plain vanilla Core bodies. Install a body retexture (CB2, Nal's, WrelicK, ...) to use this mode as intended.", -1f, null);
					GUI.color = Color.white;
				}
				else
				{
					bool flag10 = listing_Standard.RadioButton("    Auto — last-loaded body texture (" + TexBake.DetectBodyProvider() + ")", GenText.NullOrEmpty(TailorMadeMod.Settings.forcedBodyTex), 0f, null, null);
					if (flag10)
					{
						TailorMadeMod.Settings.forcedBodyTex = "";
					}
					foreach (ModContentPack modContentPack in list)
					{
						bool flag11 = listing_Standard.RadioButton("    " + modContentPack.Name, TailorMadeMod.Settings.forcedBodyTex == modContentPack.PackageId, 0f, null, null);
						if (flag11)
						{
							TailorMadeMod.Settings.forcedBodyTex = modContentPack.PackageId;
						}
					}
				}
			}
			listing_Standard.CheckboxLabeled("Unlock race-restricted apparel for vanilla pawns", ref TailorMadeMod.Settings.unlockRestrictedApparel, "HAR lets races mark apparel as race-only, which normally blocks vanilla pawns from wearing it. Since TailorMade refits any apparel to any body, this lifts that lock for non-alien pawns. Alien races keep their own restrictions.", 0f, 1f);
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Per-race body mapping (overrides the global mode above for that race)", -1f, null);
			foreach (ThingDef thingDef in TailorMadeMod.RaceList())
			{
				Rect rect3 = listing_Standard.GetRect(28f, 1f);
				bool flag12 = HarSupport.IsAlienRace(thingDef) && HarCompat.Engaged(thingDef);
				string text = ((thingDef.modContentPack != null && !thingDef.modContentPack.IsCoreMod) ? (" (" + thingDef.modContentPack.Name + ")") : "");
				Widgets.Label(new Rect(rect3.x, rect3.y + 4f, rect3.width - 205f, rect3.height), "  " + (GenText.NullOrEmpty(thingDef.label) ? thingDef.defName : thingDef.LabelCap.ToString()) + text);
				string text2;
				TailorMadeMod.Settings.raceBodyMap.TryGetValue(thingDef.defName, out text2);
				bool flag13 = Widgets.ButtonText(new Rect(rect3.xMax - 200f, rect3.y + 2f, 200f, rect3.height - 4f), TailorMadeMod.MapTokenLabel(text2), true, true, true, null);
				if (flag13)
				{
					string text3 = TailorMadeMod.NextMapToken(text2, flag12);
					bool flag14 = GenText.NullOrEmpty(text3);
					if (flag14)
					{
						TailorMadeMod.Settings.raceBodyMap.Remove(thingDef.defName);
					}
					else
					{
						TailorMadeMod.Settings.raceBodyMap[thingDef.defName] = text3;
					}
				}
				bool flag15 = Mouse.IsOver(rect3);
				if (flag15)
				{
					TooltipHandler.TipRegion(rect3, flag12 ? "Default -> Off -> Auto-resize -> Force per installed body mod. Default follows the global mode above." : "Vanilla-bodied race: Default follows the global body mod shadowing; Force renders a specific installed body mod's textures for this race.");
				}
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Apparel layers (which clothing layers get refitted)", -1f, null);
			foreach (ApparelLayerDef apparelLayerDef2 in DefDatabase<ApparelLayerDef>.AllDefsListForReading)
			{
				bool flag16 = !TailorMadeSettings.IsRefitableLayer(apparelLayerDef2);
				if (!flag16)
				{
					TailorMadeMod.DrawLayerToggle(listing_Standard, apparelLayerDef2);
				}
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Body types (untick to exclude; the button sets per-body clipping)", -1f, null);
			foreach (BodyTypeDef bodyTypeDef in DefDatabase<BodyTypeDef>.AllDefsListForReading)
			{
				Rect rect4 = listing_Standard.GetRect(28f, 1f);
				Rect rect5;
				rect5..ctor(rect4.x, rect4.y, Mathf.Max(60f, rect4.width - 175f), rect4.height);
				Rect rect6;
				rect6..ctor(rect5.xMax + 5f, rect4.y + 2f, 170f, rect4.height - 4f);
				bool flag17 = TailorMadeMod.Settings.BodyTypeEnabled(bodyTypeDef);
				bool flag18 = flag17;
				Widgets.CheckboxLabeled(rect5, "  " + bodyTypeDef.defName + ((bodyTypeDef.modContentPack != null && !bodyTypeDef.modContentPack.IsCoreMod) ? (" (" + bodyTypeDef.modContentPack.Name + ")") : ""), ref flag17, false, null, null, false, false);
				bool flag19 = flag17 != flag18;
				if (flag19)
				{
					TailorMadeMod.Settings.bodyTypeEnabled[bodyTypeDef.defName] = flag17;
				}
				ClipMode clipMode;
				bool flag20 = TailorMadeMod.Settings.bodyClipMode.TryGetValue(bodyTypeDef.defName, out clipMode);
				bool flag21 = Widgets.ButtonText(rect6, flag20 ? ("Clip: " + clipMode.ToString()) : "Clip: default", true, true, true, null);
				if (flag21)
				{
					bool flag22 = !flag20;
					if (flag22)
					{
						TailorMadeMod.Settings.bodyClipMode[bodyTypeDef.defName] = ClipMode.Always;
					}
					else
					{
						bool flag23 = clipMode == ClipMode.Always;
						if (flag23)
						{
							TailorMadeMod.Settings.bodyClipMode[bodyTypeDef.defName] = ClipMode.Never;
						}
						else
						{
							bool flag24 = clipMode == ClipMode.Never;
							if (flag24)
							{
								TailorMadeMod.Settings.bodyClipMode[bodyTypeDef.defName] = ClipMode.Auto;
							}
							else
							{
								TailorMadeMod.Settings.bodyClipMode.Remove(bodyTypeDef.defName);
							}
						}
					}
				}
				bool flag25 = Mouse.IsOver(rect4);
				if (flag25)
				{
					ClipMode clipMode2 = TailorMadeMod.Settings.ClipModeFor(bodyTypeDef);
					string text4 = "Effective clip: " + clipMode2.ToString();
					bool flag26 = clipMode2 == ClipMode.Auto;
					if (flag26)
					{
						text4 += (BodyTexCache.HasRetexture(ThingDefOf.Human, bodyTypeDef) ? " -> clipping (retexture detected)" : " -> not clipping (no retexture detected)");
					}
					TooltipHandler.TipRegion(rect4, text4);
				}
			}
			listing_Standard.GapLine(12f);
			listing_Standard.Label("Backup & sharing", -1f, null);
			listing_Standard.Label("    Export writes your per-item adjustments (fit, scale, render order, conform, hidden, keep-hair) as TailorPatternDef XML you can back up, share, or submit to be baked into the mod. Import merges such a file back in.", -1f, null);
			Rect rect7 = listing_Standard.GetRect(30f, 1f);
			float num4 = (rect7.width - 10f) / 2f;
			int num5 = TailorExportImport.CountExportable();
			bool flag27 = Widgets.ButtonText(new Rect(rect7.x, rect7.y, num4, rect7.height), "Export adjustments (" + num5.ToString() + ")", true, true, true, null);
			if (flag27)
			{
				bool flag28 = num5 == 0;
				if (flag28)
				{
					Messages.Message("No adjustments to export yet.", MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					string text5 = TailorExportImport.Export();
					Messages.Message("Exported " + num5.ToString() + " item(s) to " + text5, MessageTypeDefOf.TaskCompletion, false);
				}
			}
			bool flag29 = Widgets.ButtonText(new Rect(rect7.x + num4 + 10f, rect7.y, num4, rect7.height), "Import adjustments", true, true, true, null);
			if (flag29)
			{
				List<string> list2 = TailorExportImport.ListFiles();
				bool flag30 = list2.Count == 0;
				if (flag30)
				{
					Messages.Message("No export files in " + TailorExportImport.Folder, MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					List<FloatMenuOption> list3 = new List<FloatMenuOption>();
					foreach (string text6 in list2)
					{
						string cap = text6;
						list3.Add(new FloatMenuOption(Path.GetFileName(cap), delegate
						{
							try
							{
								int num6 = TailorExportImport.Import(cap);
								TailorMadeCache.ClearAndRepaint();
								Messages.Message("Imported " + num6.ToString() + " item(s).", MessageTypeDefOf.TaskCompletion, false);
							}
							catch (Exception ex)
							{
								Messages.Message("Import failed: " + ex.Message, MessageTypeDefOf.RejectInput, false);
							}
						}, 4, null, null, 0f, null, null, true, 0));
					}
					Find.WindowStack.Add(new FloatMenu(list3));
				}
			}
			bool flag31 = Widgets.ButtonText(listing_Standard.GetRect(28f, 1f), "Open export folder", true, true, true, null);
			if (flag31)
			{
				Application.OpenURL(TailorExportImport.Folder);
			}
			listing_Standard.End();
			Widgets.EndScrollView();
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000CD58 File Offset: 0x0000AF58
		private static List<ThingDef> RaceList()
		{
			bool flag = TailorMadeMod.raceList != null;
			List<ThingDef> list;
			if (flag)
			{
				list = TailorMadeMod.raceList;
			}
			else
			{
				TailorMadeMod.raceList = new List<ThingDef>();
				foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
				{
					bool flag2 = thingDef.race != null && thingDef.race.Humanlike;
					if (flag2)
					{
						TailorMadeMod.raceList.Add(thingDef);
					}
				}
				GenCollection.SortBy<ThingDef, string>(TailorMadeMod.raceList, (ThingDef d) => (HarSupport.IsAlienRace(d) ? "0" : "1") + (d.label ?? d.defName));
				list = TailorMadeMod.raceList;
			}
			return list;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x0000CE24 File Offset: 0x0000B024
		private static string MapTokenLabel(string tok)
		{
			bool flag = GenText.NullOrEmpty(tok);
			string text;
			if (flag)
			{
				text = "Default";
			}
			else
			{
				bool flag2 = tok == "off";
				if (flag2)
				{
					text = "Off";
				}
				else
				{
					bool flag3 = tok == "auto";
					if (flag3)
					{
						text = "Auto-resize";
					}
					else
					{
						bool flag4 = tok == "force";
						if (flag4)
						{
							text = "Force: Auto";
						}
						else
						{
							bool flag5 = tok.StartsWith("force:");
							if (flag5)
							{
								string text2 = tok.Substring(6);
								foreach (ModContentPack modContentPack in TexBake.BodyProviders())
								{
									bool flag6 = modContentPack.PackageId == text2;
									if (flag6)
									{
										return "Force: " + modContentPack.Name;
									}
								}
								text = "Force: " + text2 + " (missing)";
							}
							else
							{
								text = tok;
							}
						}
					}
				}
			}
			return text;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000CF38 File Offset: 0x0000B138
		private static string NextMapToken(string tok, bool alienEngaged)
		{
			List<ModContentPack> list = TexBake.BodyProviders();
			List<string> list2 = new List<string> { "" };
			if (alienEngaged)
			{
				list2.Add("off");
				list2.Add("auto");
				bool flag = list.Count > 0;
				if (flag)
				{
					list2.Add("force");
				}
			}
			foreach (ModContentPack modContentPack in list)
			{
				list2.Add("force:" + modContentPack.PackageId);
			}
			int num = list2.IndexOf(tok ?? "");
			return list2[(num + 1) % list2.Count];
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000D018 File Offset: 0x0000B218
		private static void DrawLayerToggle(Listing_Standard l, ApparelLayerDef layer)
		{
			bool flag = layer == null;
			if (!flag)
			{
				bool flag2 = TailorMadeMod.Settings.LayerEnabled(layer);
				bool flag3 = flag2;
				l.CheckboxLabeled("  " + layer.LabelCap, ref flag2, null, 0f, 1f);
				bool flag4 = flag2 != flag3;
				if (flag4)
				{
					TailorMadeMod.Settings.layerEnabled[layer.defName] = flag2;
				}
			}
		}

		// Token: 0x060000BD RID: 189 RVA: 0x0000D08C File Offset: 0x0000B28C
		private static int RoundPow2(int v)
		{
			int num = 128;
			while (num < v && num < 2048)
			{
				num <<= 1;
			}
			return num;
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000D0BE File Offset: 0x0000B2BE
		public override void WriteSettings()
		{
			base.WriteSettings();
			TailorMadeCache.ClearAndRepaint();
		}

		// Token: 0x040000A7 RID: 167
		public static TailorMadeSettings Settings;

		// Token: 0x040000A8 RID: 168
		private Vector2 scroll = Vector2.zero;

		// Token: 0x040000A9 RID: 169
		private static List<ThingDef> raceList;
	}
}
