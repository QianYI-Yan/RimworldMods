using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200000B RID: 11
	public class Window_Tailor : Window
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000033FE File Offset: 0x000015FE
		private static Pawn CurPawn
		{
			get
			{
				Selector selector = Find.Selector;
				return ((selector != null) ? selector.SingleSelectedThing : null) as Pawn;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000023 RID: 35 RVA: 0x00003416 File Offset: 0x00001616
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(740f, 660f);
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00003428 File Offset: 0x00001628
		public Window_Tailor()
			: base(null)
		{
			this.optionalTitle = null;
			this.draggable = false;
			this.preventCameraMotion = false;
			this.closeOnClickedOutside = true;
			this.closeOnCancel = true;
			this.doCloseX = false;
			this.absorbInputAroundWindow = false;
			this.doWindowBackground = false;
			this.drawShadow = true;
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000025 RID: 37 RVA: 0x000034AC File Offset: 0x000016AC
		protected override float Margin
		{
			get
			{
				return 0f;
			}
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000034B4 File Offset: 0x000016B4
		public override void PreClose()
		{
			this.CommitIfDirty();
			bool flag = this.sessionDirty;
			if (flag)
			{
				TailorMadeMod mod = LoadedModManager.GetMod<TailorMadeMod>();
				if (mod != null)
				{
					mod.WriteSettings();
				}
			}
			this.sessionDirty = false;
			base.PreClose();
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000034F4 File Offset: 0x000016F4
		public override void DoWindowContents(Rect winRect)
		{
			Palette.DrawCard(winRect);
			Rect rect = GenUI.ContractedBy(winRect, 16f);
			Rect rect2;
			rect2..ctor(rect.x, rect.y, rect.width - 28f, 52f);
			bool tunerAvailable = PaperPatternCompat.TunerAvailable;
			Rect rect3;
			rect3..ctor(rect.xMax - 140f, rect.y, 110f, 26f);
			Event current = Event.current;
			bool flag = current.type == null && current.button == 0 && rect2.Contains(current.mousePosition) && (!tunerAvailable || !rect3.Contains(current.mousePosition));
			if (flag)
			{
				this.headerDragging = true;
				current.Use();
			}
			else
			{
				bool flag2 = this.headerDragging && current.type == 3;
				if (flag2)
				{
					this.windowRect.x = this.windowRect.x + current.delta.x;
					this.windowRect.y = this.windowRect.y + current.delta.y;
					current.Use();
				}
				else
				{
					bool flag3 = this.headerDragging && current.type == 1;
					if (flag3)
					{
						this.headerDragging = false;
						current.Use();
					}
				}
			}
			bool flag4 = current.type == 4 && current.keyCode == 27;
			if (flag4)
			{
				this.Close(true);
				current.Use();
			}
			else
			{
				Rect rect4;
				rect4..ctor(rect.xMax - 26f, rect.y - 2f, 24f, 24f);
				Widgets.DrawHighlightIfMouseover(rect4);
				bool flag5 = TailorTex.Close != null;
				if (flag5)
				{
					GUI.DrawTexture(rect4, TailorTex.Close);
				}
				else
				{
					Text.Font = 2;
					GUI.color = Palette.TextDim;
					Widgets.Label(rect4, "x");
					GUI.color = Color.white;
					Text.Font = 1;
				}
				bool flag6 = Widgets.ButtonInvisible(rect4, true);
				if (flag6)
				{
					this.Close(true);
				}
				else
				{
					bool flag7 = tunerAvailable;
					if (flag7)
					{
						TooltipHandler.TipRegion(rect3, "Open Apparel Paper Pattern's tuner window (the User Tuner THIGAPPE uses). Red items in the worn list are controlled by THIGAPPE/APP. Exempt them there to hand them to TailorMade.");
						bool flag8 = Window_Tailor.FlatButton(rect3, "APP tuner");
						if (flag8)
						{
							PaperPatternCompat.OpenTuner();
						}
					}
					Selector selector = Find.Selector;
					Pawn pawn = ((selector != null) ? selector.SingleSelectedThing : null) as Pawn;
					Text.Font = 2;
					GUI.color = Palette.Accent;
					Widgets.Label(new Rect(rect.x, rect.y, rect.width - 28f, 32f), "TailorMade");
					GUI.color = Palette.TextDim;
					Text.Font = 1;
					Widgets.Label(new Rect(rect.x, rect.y + 32f, rect.width, 22f), (pawn != null) ? ("Editing " + pawn.LabelShortCap) : "Select a single colonist.");
					GUI.color = Color.white;
					float num = rect.y + 60f;
					bool flag9 = ((pawn != null) ? pawn.apparel : null) == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike;
					if (flag9)
					{
						Rect rect5;
						rect5..ctor(rect.x, num, rect.width, rect.yMax - num);
						Palette.DrawBackdrop(rect5);
						GUI.color = Palette.TextDim;
						Text.Anchor = 4;
						Widgets.Label(rect5, "Select a single humanlike colonist to edit their apparel masks.");
						Text.Anchor = 0;
						GUI.color = Color.white;
					}
					else
					{
						Rect rect6;
						rect6..ctor(rect.x, num, 230f, rect.yMax - num);
						Rect rect7;
						rect7..ctor(rect6.xMax + 14f, num, rect.xMax - rect6.xMax - 14f, rect.yMax - num);
						this.DrawList(rect6, pawn);
						this.DrawRight(rect7, pawn);
						bool flag10 = this.liveDirty && !this.dragging && Time.realtimeSinceStartup - this.lastEdit > 0.18f;
						if (flag10)
						{
							this.CommitIfDirty();
						}
						bool flag11 = this.outlinePending && Time.realtimeSinceStartup - this.outlineEditAt > 0.2f;
						if (flag11)
						{
							this.outlinePending = false;
							TexBake.Version++;
							foreach (Graphic_TailorMade graphic_TailorMade in Graphic_TailorMade.Live)
							{
								graphic_TailorMade.Repaint();
							}
							if (pawn != null)
							{
								Pawn_DrawTracker drawer = pawn.Drawer;
								if (drawer != null)
								{
									PawnRenderer renderer = drawer.renderer;
									if (renderer != null)
									{
										renderer.SetAllGraphicsDirty();
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003A00 File Offset: 0x00001C00
		private bool AppControls(Pawn pawn, Apparel ap)
		{
			bool flag = !PaperPatternCompat.DeferEnabled || ap == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = Time.realtimeSinceStartup - this.appCacheAt > 0.5f;
				if (flag3)
				{
					this.appControlled.Clear();
					this.appCacheAt = Time.realtimeSinceStartup;
				}
				bool flag5;
				bool flag4 = !this.appControlled.TryGetValue(ap.thingIDNumber, out flag5);
				if (flag4)
				{
					BodyTypeDef bodyTypeDef;
					if (pawn == null)
					{
						bodyTypeDef = null;
					}
					else
					{
						Pawn_StoryTracker story = pawn.story;
						bodyTypeDef = ((story != null) ? story.bodyType : null);
					}
					flag5 = PaperPatternCompat.ControlledByApp(ap, bodyTypeDef);
					this.appControlled[ap.thingIDNumber] = flag5;
				}
				flag2 = flag5;
			}
			return flag2;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00003AAC File Offset: 0x00001CAC
		private static bool FlatButton(Rect r, string label)
		{
			Widgets.DrawBoxSolid(r, Palette.BGL);
			bool flag = Mouse.IsOver(r);
			if (flag)
			{
				Widgets.DrawHighlight(r);
			}
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = 4;
			GUI.color = Palette.Stat;
			Widgets.Label(r, label);
			GUI.color = Color.white;
			Text.Anchor = anchor;
			return Widgets.ButtonInvisible(r, true);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003B14 File Offset: 0x00001D14
		private void DrawList(Rect area, Pawn pawn)
		{
			Palette.DrawBackdrop(area);
			Rect rect = GenUI.ContractedBy(area, 8f);
			Palette.Header(new Rect(rect.x, rect.y, rect.width, 20f), "WORN APPAREL (top = rendered on top)");
			Rect rect2;
			rect2..ctor(rect.x, rect.y + 26f, rect.width, rect.height - 26f);
			List<Apparel> list = new List<Apparel>(pawn.apparel.WornApparel);
			list.Sort((Apparel a, Apparel b) => Window_Tailor.OrderKey(pawn, b).CompareTo(Window_Tailor.OrderKey(pawn, a)));
			float num = (float)list.Count * 32f;
			Rect rect3;
			rect3..ctor(0f, 0f, FlatScroll.ViewWidth(rect2, num), num);
			FlatScroll.Begin(rect2, ref this.listScroll, rect3);
			Event current = Event.current;
			HashSet<string> hiddenRenderDefs = TailorMadeMod.Settings.hiddenRenderDefs;
			int num2 = -1;
			float num3 = 0f;
			for (int i = 0; i < list.Count; i++)
			{
				Apparel apparel = list[i];
				Rect rect4;
				rect4..ctor(0f, num3, rect3.width, 30f);
				string defName = apparel.def.defName;
				bool flag = defName == this.selectedDef;
				bool flag2 = Graphic_TailorMade.LiveDefNames.Contains(defName);
				bool flag3 = hiddenRenderDefs.Contains(defName);
				bool flag4 = !flag2 && this.AppControls(pawn, apparel);
				bool flag5 = this.dragRowDef == defName;
				if (flag5)
				{
					Widgets.DrawBoxSolid(rect4, new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.3f));
				}
				else
				{
					bool flag6 = flag;
					if (flag6)
					{
						Widgets.DrawBoxSolid(rect4, new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.18f));
					}
					else
					{
						bool flag7 = Mouse.IsOver(rect4);
						if (flag7)
						{
							Widgets.DrawHighlight(rect4);
						}
					}
				}
				bool flag8 = this.dragRowDef != null && this.dragRowDef != defName && Mouse.IsOver(rect4);
				if (flag8)
				{
					bool flag9 = current.mousePosition.y < rect4.y + 15f;
					num2 = (flag9 ? i : (i + 1));
					float num4 = (flag9 ? rect4.y : rect4.yMax);
					Widgets.DrawBoxSolid(new Rect(rect4.x + 2f, num4 - 1f, rect4.width - 4f, 2f), Palette.Accent);
				}
				bool flag10 = flag2;
				if (flag10)
				{
					Rect rect5;
					rect5..ctor(rect4.xMax - 74f, rect4.y + 4f, 22f, 22f);
					Rect rect6;
					rect6..ctor(rect4.xMax - 50f, rect4.y + 4f, 22f, 22f);
					Rect rect7;
					rect7..ctor(rect4.xMax - 26f, rect4.y + 4f, 22f, 22f);
					TooltipHandler.TipRegion(rect5, "Render one step up");
					bool flag11 = TailorTex.ArrowUp != null && Widgets.ButtonImage(rect5, TailorTex.ArrowUp, true, null);
					if (flag11)
					{
						this.MoveRenderOrder(pawn, defName, 1);
					}
					TooltipHandler.TipRegion(rect6, "Render one step down");
					bool flag12 = TailorTex.ArrowDown != null && Widgets.ButtonImage(rect6, TailorTex.ArrowDown, true, null);
					if (flag12)
					{
						this.MoveRenderOrder(pawn, defName, -1);
					}
					bool flag13 = !flag3;
					TooltipHandler.TipRegion(rect7, flag13 ? "Visible — click to hide on the pawn" : "Hidden on the pawn — click to show");
					GUI.color = (flag13 ? Palette.Stat : new Color(Palette.TextDim.r, Palette.TextDim.g, Palette.TextDim.b, 0.6f));
					Texture2D texture2D = (flag13 ? TailorTex.EyeOpen : TailorTex.EyeClosed);
					bool flag14 = ((texture2D != null) ? Widgets.ButtonImage(rect7, texture2D, true, null) : Widgets.ButtonText(rect7, flag13 ? "O" : "-", true, true, true, null));
					GUI.color = Color.white;
					bool flag15 = flag14;
					if (flag15)
					{
						bool flag16 = flag13;
						if (flag16)
						{
							hiddenRenderDefs.Add(defName);
						}
						else
						{
							hiddenRenderDefs.Remove(defName);
						}
						this.sessionDirty = true;
						Pawn_DrawTracker drawer = pawn.Drawer;
						if (drawer != null)
						{
							PawnRenderer renderer = drawer.renderer;
							if (renderer != null)
							{
								renderer.SetAllGraphicsDirty();
							}
						}
					}
				}
				GUI.color = (flag3 ? new Color(Palette.TextDim.r, Palette.TextDim.g, Palette.TextDim.b, 0.55f) : (flag2 ? (flag ? Palette.Accent : Palette.Stat) : (flag4 ? Palette.Warn : Palette.TextDim)));
				Widgets.Label(new Rect(rect4.x + 8f, rect4.y + 5f, rect4.width - (flag2 ? 84f : 12f), 22f), apparel.LabelCap);
				GUI.color = Color.white;
				Rect rect8;
				rect8..ctor(rect4.x, rect4.y, rect4.width - (flag2 ? 78f : 0f), rect4.height);
				bool flag17 = flag4;
				if (flag17)
				{
					TooltipHandler.TipRegion(rect8, "Controlled by Apparel Paper Pattern (THIGAPPE), so TailorMade is deferring to its re-render. Exempt it in APP's tuner to hand it to TailorMade.");
				}
				bool flag18 = current.type == null && current.button == 0 && Mouse.IsOver(rect8);
				if (flag18)
				{
					this.selectedDef = defName;
					this.dragging = false;
					bool flag19 = flag2;
					if (flag19)
					{
						this.dragRowDef = defName;
					}
					current.Use();
				}
				num3 += 32f;
			}
			bool flag20 = this.dragRowDef != null && current.type == 1;
			if (flag20)
			{
				this.DropReorder(pawn, list, num2);
				this.dragRowDef = null;
				current.Use();
			}
			FlatScroll.End(rect2, ref this.listScroll, rect3, 7301);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000041A8 File Offset: 0x000023A8
		private void DrawRight(Rect area, Pawn pawn)
		{
			Rect rect = area;
			float num = Mathf.Min(280f, rect.width);
			Rect rect2;
			rect2..ctor(rect.x + (rect.width - num) / 2f, rect.y, num, num);
			bool flag = !GenText.NullOrEmpty(this.selectedDef) && Graphic_TailorMade.LiveDefNames.Contains(this.selectedDef);
			this.SyncLive();
			this.DrawPreview(rect2, pawn, flag);
			float num2 = rect2.yMax + 10f;
			float num3 = (rect.width - 18f) / 4f;
			for (int i = 0; i < 4; i++)
			{
				Rect rect3;
				rect3..ctor(rect.x + (float)i * (num3 + 6f), num2, num3, 30f);
				bool flag2 = i == this.rotTab;
				if (flag2)
				{
					Widgets.DrawBoxSolid(rect3, new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.22f));
				}
				Widgets.DrawHighlightIfMouseover(rect3);
				Texture2D texture2D = TailorTex.Face[i];
				bool flag3 = texture2D != null;
				if (flag3)
				{
					GUI.DrawTexture(new Rect(rect3.center.x - 15f, rect3.y + 1f, 28f, 28f), texture2D);
				}
				bool flag4 = Widgets.ButtonInvisible(rect3, true);
				if (flag4)
				{
					this.rotTab = i;
					this.dragging = false;
				}
			}
			float num4 = num2 + 40f;
			bool flag5 = GenText.NullOrEmpty(this.selectedDef);
			if (flag5)
			{
				GUI.color = Palette.TextDim;
				Widgets.Label(new Rect(rect.x, num4, rect.width, 40f), "Pick an apparel item on the left.");
				GUI.color = Color.white;
			}
			else
			{
				bool flag6 = !flag;
				if (flag6)
				{
					Apparel apparel = GenCollection.FirstOrDefault<Apparel>(pawn.apparel.WornApparel, (Apparel a) => a.def.defName == this.selectedDef);
					bool flag7 = apparel != null && this.AppControls(pawn, apparel);
					if (flag7)
					{
						GUI.color = Palette.Warn;
						Widgets.Label(new Rect(rect.x, num4, rect.width, 84f), "“" + this.selectedDef + "” is controlled by Apparel Paper Pattern (THIGAPPE), not TailorMade. Exempt it in APP's tuner (or turn off deference in TailorMade's mod settings) to fit and edit it here.");
						GUI.color = Color.white;
						bool flag8 = Window_Tailor.FlatButton(new Rect(rect.x, num4 + 88f, 150f, 28f), "Open APP tuner");
						if (flag8)
						{
							PaperPatternCompat.OpenTuner();
						}
					}
					else
					{
						GUI.color = Palette.TextDim;
						Widgets.Label(new Rect(rect.x, num4, rect.width, 60f), "“" + this.selectedDef + "” isn't currently refitted (its layer or body type may be excluded). Nudges won't show until it is.");
						GUI.color = Color.white;
					}
				}
				else
				{
					float num5 = (Event.current.shift ? 0.025f : 0.005f);
					float num6 = (Event.current.shift ? 0.05f : 0.01f);
					float num7 = rect.x + 30f + 6f;
					float num8 = num4 + 30f + 2f;
					bool flag9 = Window_Tailor.IconBtn(new Rect(num7, num8 - 30f - 3f, 30f, 30f), TailorTex.ArrowUp, "Move up");
					if (flag9)
					{
						this.Edit(0f, num5, 0f, 0f);
					}
					bool flag10 = Window_Tailor.IconBtn(new Rect(num7, num8 + 3f, 30f, 30f), TailorTex.ArrowDown, "Move down");
					if (flag10)
					{
						this.Edit(0f, -num5, 0f, 0f);
					}
					bool flag11 = Window_Tailor.IconBtn(new Rect(num7 - 30f - 3f, num8 - 15f, 30f, 30f), TailorTex.ArrowLeft, "Move left");
					if (flag11)
					{
						this.Edit(-num5, 0f, 0f, 0f);
					}
					bool flag12 = Window_Tailor.IconBtn(new Rect(num7 + 30f + 3f, num8 - 15f, 30f, 30f), TailorTex.ArrowRight, "Move right");
					if (flag12)
					{
						this.Edit(num5, 0f, 0f, 0f);
					}
					float num9 = rect.x + rect.width - 60f - 8f;
					bool flag13 = Window_Tailor.IconBtn(new Rect(num9, num8 - 15f, 30f, 30f), TailorTex.Minus, "Smaller");
					if (flag13)
					{
						this.Edit(0f, 0f, -num6, 0f);
					}
					bool flag14 = Window_Tailor.IconBtn(new Rect(num9 + 30f + 4f, num8 - 15f, 30f, 30f), TailorTex.Plus, "Larger");
					if (flag14)
					{
						this.Edit(0f, 0f, num6, 0f);
					}
					GUI.color = Palette.TextDim;
					Text.Anchor = 4;
					Widgets.Label(new Rect(rect.x, num8 - 10f, rect.width, 20f), string.Concat(new string[]
					{
						"X ",
						this.liveOff.x.ToString("f3"),
						"   Y ",
						this.liveOff.y.ToString("f3"),
						"   ×",
						this.liveScale.ToString("f2")
					}));
					Text.Anchor = 0;
					GUI.color = Color.white;
					num4 = num8 + 30f + 12f;
					float num10 = (Event.current.shift ? 15f : 3f);
					bool flag15 = Window_Tailor.IconBtn(new Rect(rect.x, num4, 30f, 30f), TailorTex.RotL, "Rotate left");
					if (flag15)
					{
						this.Edit(0f, 0f, 0f, -num10);
					}
					bool flag16 = Window_Tailor.IconBtn(new Rect(rect.x + 30f + 6f, num4, 30f, 30f), TailorTex.RotR, "Rotate right");
					if (flag16)
					{
						this.Edit(0f, 0f, 0f, num10);
					}
					GUI.color = Palette.TextDim;
					Widgets.Label(new Rect(rect.x + 60f + 16f, num4 + 6f, rect.width - 60f - 16f, 22f), "Rotate  " + this.liveAngle.ToString("f0") + "°");
					GUI.color = Color.white;
					num4 += 40f;
					TailorMadeSettings s = TailorMadeMod.Settings;
					bool flag17 = s.hairForceItems.Contains(this.selectedDef);
					bool flag18 = Window_Tailor.IconToggle(new Rect(rect.x, num4, 30f, 30f), TailorTex.HairIco, flag17, "Always keep hair visible with this item");
					if (flag18)
					{
						bool flag19 = flag17;
						if (flag19)
						{
							s.hairForceItems.Remove(this.selectedDef);
						}
						else
						{
							s.hairForceItems.Add(this.selectedDef);
						}
						this.sessionDirty = true;
						Pawn_DrawTracker drawer = pawn.Drawer;
						if (drawer != null)
						{
							PawnRenderer renderer = drawer.renderer;
							if (renderer != null)
							{
								renderer.SetAllGraphicsDirty();
							}
						}
					}
					float num11 = 38f;
					TailorAdjust tailorAdjust = PerPawnAdjust.Effective(pawn, this.selectedDef);
					bool flag20 = tailorAdjust == null || tailorAdjust.conform;
					bool flag21 = Window_Tailor.IconToggle(new Rect(rect.x + num11, num4, 30f, 30f), TailorTex.Conform, flag20, "Conform to body (ON). Turn OFF to keep protruding armor parts — pauldrons, hoods, flared coats — with no body warp or silhouette clip; only your manual nudge applies.");
					if (flag21)
					{
						TailorAdjust orAdd = PerPawnAdjust.GetOrAdd(pawn, this.selectedDef, s.GetAdjust(this.selectedDef));
						bool flag22 = orAdd != null;
						if (flag22)
						{
							orAdd.conform = !orAdd.conform;
						}
						this.sessionDirty = true;
						Window_Tailor.RebakeDef(this.selectedDef);
					}
					bool flag23 = Window_Tailor.IconToggle(new Rect(rect.x + num11 * 2f, num4, 30f, 30f), TailorTex.LinkIco, s.linkFacings, "Link opposite facings (applies when you edit). ON: edits to this view also set the opposite view - north↔south, east↔west - as a mirror image (X offset and rotation flip sign; height and scale copy).");
					if (flag23)
					{
						s.linkFacings = !s.linkFacings;
						this.sessionDirty = true;
					}
					bool flag24 = Window_Tailor.IconBtn(new Rect(rect.x + num11 * 3f, num4, 30f, 30f), TailorTex.ResetIco, "Reset THIS pawn's fit for this item (revert to the shared default / automatic)");
					if (flag24)
					{
						PerPawnAdjust.Remove(pawn, this.selectedDef);
						this.liveOff = Vector2.zero;
						this.liveScale = 1f;
						this.liveAngle = 0f;
						this.liveDirty = false;
						this.liveDef = null;
						this.sessionDirty = true;
						Window_Tailor.RebakeDef(this.selectedDef);
						Pawn_DrawTracker drawer2 = pawn.Drawer;
						if (drawer2 != null)
						{
							PawnRenderer renderer2 = drawer2.renderer;
							if (renderer2 != null)
							{
								renderer2.SetAllGraphicsDirty();
							}
						}
					}
					bool flag25 = Window_Tailor.IconBtn(new Rect(rect.x + num11 * 4f, num4, 30f, 30f), TailorTex.CopyIco, "Copy as TailorPatternDef XML (clipboard)");
					if (flag25)
					{
						GUIUtility.systemCopyBuffer = Window_Tailor.BuildXml(pawn, this.selectedDef);
						Messages.Message("TailorPatternDef XML copied to clipboard.", MessageTypeDefOf.TaskCompletion, false);
					}
					bool flag26 = Window_Tailor.IconBtn(new Rect(rect.x + num11 * 5f, num4, 30f, 30f), TailorTex.SaveIco, "Apply to ALL pawns: promote this pawn's fit to the shared default for this apparel (persists across saves)");
					if (flag26)
					{
						this.CommitIfDirty();
						TailorAdjust tailorAdjust2 = PerPawnAdjust.Effective(pawn, this.selectedDef);
						bool flag27 = tailorAdjust2 != null;
						if (flag27)
						{
							s.adjustments[this.selectedDef] = tailorAdjust2.Clone();
							PerPawnAdjust.Remove(pawn, this.selectedDef);
						}
						TailorMadeMod mod = LoadedModManager.GetMod<TailorMadeMod>();
						if (mod != null)
						{
							mod.WriteSettings();
						}
						this.sessionDirty = false;
						this.liveDef = null;
						TexBake.Version++;
						foreach (Graphic_TailorMade graphic_TailorMade in Graphic_TailorMade.Live)
						{
							graphic_TailorMade.Repaint();
						}
						bool flag28 = Find.Maps != null;
						if (flag28)
						{
							foreach (Map map in Find.Maps)
							{
								foreach (Pawn pawn2 in map.mapPawns.AllPawnsSpawned)
								{
									Pawn_DrawTracker drawer3 = pawn2.Drawer;
									if (drawer3 != null)
									{
										PawnRenderer renderer3 = drawer3.renderer;
										if (renderer3 != null)
										{
											renderer3.SetAllGraphicsDirty();
										}
									}
								}
							}
						}
						Messages.Message("Applied to every pawn wearing " + this.selectedDef + " (saved as the shared default).", MessageTypeDefOf.TaskCompletion, false);
					}
					GUI.color = Palette.TextDim;
					Widgets.Label(new Rect(rect.x + num11 * 6f + 4f, num4 + 5f, rect.width - num11 * 6f - 4f, 22f), "Hair·Conform·Link·Reset·Copy·All");
					GUI.color = Color.white;
					num4 += 38f;
					Rect rect4;
					rect4..ctor(rect.x, num4, 150f, 26f);
					bool flag29 = Widgets.ButtonText(rect4, "Reset all items", true, true, true, null);
					if (flag29)
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("Reset every apparel adjustment (fit, scale, render order, conform) back to automatic? This affects all pawns and can't be undone.", delegate
						{
							s.adjustments.Clear();
							PerPawnAdjust.ClearAll();
							this.liveOff = Vector2.zero;
							this.liveScale = 1f;
							this.liveAngle = 0f;
							this.liveDirty = false;
							TexBake.Version++;
							foreach (Graphic_TailorMade graphic_TailorMade2 in Graphic_TailorMade.Live)
							{
								graphic_TailorMade2.Repaint();
							}
							Selector selector = Find.Selector;
							Pawn pawn3 = ((selector != null) ? selector.SingleSelectedThing : null) as Pawn;
							if (pawn3 != null)
							{
								Pawn_DrawTracker drawer4 = pawn3.Drawer;
								if (drawer4 != null)
								{
									PawnRenderer renderer4 = drawer4.renderer;
									if (renderer4 != null)
									{
										renderer4.SetAllGraphicsDirty();
									}
								}
							}
							TailorMadeMod mod2 = LoadedModManager.GetMod<TailorMadeMod>();
							if (mod2 != null)
							{
								mod2.WriteSettings();
							}
							this.sessionDirty = false;
							Messages.Message("All apparel adjustments reset to automatic.", MessageTypeDefOf.TaskCompletion, false);
						}, true, null, 1));
					}
					TailorMadeSettings settings = TailorMadeMod.Settings;
					int num12 = (settings.bodyMaskOutline ? Mathf.Clamp(settings.outlinePixels, 1, 6) : 0);
					float num13 = rect4.xMax + 14f;
					GUI.color = Palette.TextDim;
					Widgets.Label(new Rect(num13, num4 + 4f, 134f, 22f), "Clothing outline: " + ((num12 == 0) ? "Off" : (num12.ToString() + "px")));
					GUI.color = Color.white;
					int num14 = Mathf.RoundToInt(Widgets.HorizontalSlider(new Rect(num13 + 136f, num4 + 6f, Mathf.Max(60f, rect.xMax - (num13 + 136f)), 16f), (float)num12, 0f, 6f, false, null, null, null, 1f));
					bool flag30 = num14 != num12;
					if (flag30)
					{
						settings.bodyMaskOutline = num14 > 0;
						bool flag31 = num14 > 0;
						if (flag31)
						{
							settings.outlinePixels = num14;
						}
						this.sessionDirty = true;
						this.outlinePending = true;
						this.outlineEditAt = Time.realtimeSinceStartup;
					}
					num4 += 30f;
					GUI.color = Palette.TextDim;
					Widgets.Label(new Rect(rect.x, num4, rect.width, 22f), "Drag to move, scroll to scale. Esc or X closes.");
					GUI.color = Color.white;
				}
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00004FC0 File Offset: 0x000031C0
		private void SyncLive()
		{
			bool flag = this.liveDef == this.selectedDef && this.liveRot == this.rotTab;
			if (!flag)
			{
				this.CommitIfDirty();
				this.liveDef = this.selectedDef;
				this.liveRot = this.rotTab;
				TailorAdjust tailorAdjust = PerPawnAdjust.Effective(Window_Tailor.CurPawn, this.selectedDef);
				Rot4 rot;
				rot..ctor(this.rotTab);
				this.liveOff = ((tailorAdjust != null) ? tailorAdjust.GetOffset(rot) : Vector2.zero);
				this.liveScale = ((tailorAdjust != null) ? tailorAdjust.GetScale(rot) : 1f);
				this.liveAngle = ((tailorAdjust != null) ? tailorAdjust.GetAngle(rot) : 0f);
				this.liveDirty = false;
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00005084 File Offset: 0x00003284
		private void Edit(float dx, float dy, float ds, float dang = 0f)
		{
			this.liveOff.x = Mathf.Clamp(this.liveOff.x + dx, -0.5f, 0.5f);
			this.liveOff.y = Mathf.Clamp(this.liveOff.y + dy, -0.5f, 0.5f);
			this.liveScale = Mathf.Clamp(this.liveScale + ds, 0.4f, 2f);
			this.liveAngle = Mathf.Repeat(this.liveAngle + dang + 180f, 360f) - 180f;
			this.liveDirty = true;
			this.lastEdit = Time.realtimeSinceStartup;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00005134 File Offset: 0x00003334
		private void CommitIfDirty()
		{
			bool flag = !this.liveDirty || GenText.NullOrEmpty(this.liveDef);
			if (!flag)
			{
				this.Commit(this.liveDef, new Rot4(this.liveRot));
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00005178 File Offset: 0x00003378
		private void Commit(string def, Rot4 rot)
		{
			TailorAdjust orAdd = PerPawnAdjust.GetOrAdd(Window_Tailor.CurPawn, def, TailorMadeMod.Settings.GetAdjust(def));
			bool flag = orAdd == null;
			if (!flag)
			{
				orAdd.SetOffset(rot, this.liveOff);
				orAdd.SetScale(rot, this.liveScale);
				orAdd.SetAngle(rot, this.liveAngle);
				bool linkFacings = TailorMadeMod.Settings.linkFacings;
				if (linkFacings)
				{
					Rot4 opposite = rot.Opposite;
					orAdd.SetOffset(opposite, new Vector2(-this.liveOff.x, this.liveOff.y));
					orAdd.SetScale(opposite, this.liveScale);
					orAdd.SetAngle(opposite, -this.liveAngle);
				}
				this.liveDirty = false;
				this.sessionDirty = true;
				Window_Tailor.RebakeDef(def);
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00005244 File Offset: 0x00003444
		private static void RebakeDef(string def)
		{
			TexBake.Version++;
			foreach (Graphic_TailorMade graphic_TailorMade in Graphic_TailorMade.Live)
			{
				bool flag = graphic_TailorMade.IsForDef(def);
				if (flag)
				{
					graphic_TailorMade.Repaint();
				}
			}
			Selector selector = Find.Selector;
			Pawn pawn = ((selector != null) ? selector.SingleSelectedThing : null) as Pawn;
			if (pawn != null)
			{
				Pawn_DrawTracker drawer = pawn.Drawer;
				if (drawer != null)
				{
					PawnRenderer renderer = drawer.renderer;
					if (renderer != null)
					{
						renderer.SetAllGraphicsDirty();
					}
				}
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000052E8 File Offset: 0x000034E8
		private void DrawPreview(Rect rect, Pawn pawn, bool refit)
		{
			Palette.DrawBackdrop(rect);
			Rect rect2 = GenUI.ContractedBy(rect, 8f);
			Widgets.DrawBoxSolid(rect2, new Color(0.52f, 0.55f, 0.6f, 1f));
			Vector2 vector;
			vector..ctor(rect2.width, rect2.height);
			RenderTexture renderTexture = PortraitsCache.Get(pawn, vector, new Rot4(this.rotTab), default(Vector3), 1f, true, true, true, true, null, null, false, null);
			bool flag = renderTexture != null;
			if (flag)
			{
				GUI.DrawTexture(rect2, renderTexture, 2);
			}
			bool flag2 = refit && Mouse.IsOver(rect);
			if (flag2)
			{
				GUI.color = new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, this.dragging ? 0.85f : 0.4f);
				Widgets.DrawBox(rect, 1, null);
				GUI.color = Color.white;
			}
			bool flag3 = refit && !GenText.NullOrEmpty(this.selectedDef);
			if (flag3)
			{
				this.HandlePreviewInput(rect);
			}
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00005418 File Offset: 0x00003618
		private void HandlePreviewInput(Rect rect)
		{
			Event current = Event.current;
			bool flag = !Mouse.IsOver(rect) && !this.dragging;
			if (!flag)
			{
				float num = rect.width - 16f;
				bool flag2 = current.type == 6 && Mouse.IsOver(rect);
				if (flag2)
				{
					this.Edit(0f, 0f, -current.delta.y * 0.02f, 0f);
					current.Use();
				}
				else
				{
					bool flag3 = current.type == null && current.button == 0 && Mouse.IsOver(rect);
					if (flag3)
					{
						this.dragging = true;
						current.Use();
					}
					else
					{
						bool flag4 = this.dragging && current.type == 3;
						if (flag4)
						{
							this.Edit(current.delta.x / num, -current.delta.y / num, 0f, 0f);
							current.Use();
						}
						else
						{
							bool flag5 = this.dragging && current.type == 1;
							if (flag5)
							{
								this.dragging = false;
								this.lastEdit = Time.realtimeSinceStartup;
								this.CommitIfDirty();
								current.Use();
							}
						}
					}
				}
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00005564 File Offset: 0x00003764
		private static float OrderKey(Pawn pawn, Apparel ap)
		{
			ApparelLayerDef lastLayer = ap.def.apparel.LastLayer;
			bool flag = lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover;
			int num = 0;
			List<Apparel> wornApparel = pawn.apparel.WornApparel;
			for (int i = 0; i < wornApparel.Count; i++)
			{
				Apparel apparel = wornApparel[i];
				bool flag2 = apparel == ap;
				if (flag2)
				{
					break;
				}
				bool flag3 = GenText.NullOrEmpty(apparel.WornGraphicPath);
				if (!flag3)
				{
					ApparelLayerDef lastLayer2 = apparel.def.apparel.LastLayer;
					bool flag4 = lastLayer2 == ApparelLayerDefOf.Overhead || lastLayer2 == ApparelLayerDefOf.EyeCover;
					bool flag5 = flag4 == flag;
					if (flag5)
					{
						num++;
					}
				}
			}
			TailorAdjust tailorAdjust = PerPawnAdjust.Effective(pawn, ap.def.defName);
			return (flag ? 70f : 20f) + (float)num + ((tailorAdjust != null) ? tailorAdjust.renderOrder : 0f);
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00005664 File Offset: 0x00003864
		private void MoveRenderOrder(Pawn pawn, string def, int dir)
		{
			List<Apparel> list = new List<Apparel>(pawn.apparel.WornApparel);
			list.Sort((Apparel a, Apparel b) => Window_Tailor.OrderKey(pawn, a).CompareTo(Window_Tailor.OrderKey(pawn, b)));
			int num = list.FindIndex((Apparel a) => a.def.defName == def);
			bool flag = num < 0;
			if (!flag)
			{
				int num2 = num + dir;
				bool flag2 = num2 < 0 || num2 >= list.Count;
				if (!flag2)
				{
					float num3 = Window_Tailor.OrderKey(pawn, list[num]);
					float num4 = Window_Tailor.OrderKey(pawn, list[num2]);
					TailorAdjust orAdd = PerPawnAdjust.GetOrAdd(pawn, def, TailorMadeMod.Settings.GetAdjust(def));
					bool flag3 = orAdd != null;
					if (flag3)
					{
						orAdd.renderOrder += num4 - num3 + (float)dir;
					}
					this.sessionDirty = true;
					Pawn_DrawTracker drawer = pawn.Drawer;
					if (drawer != null)
					{
						PawnRenderer renderer = drawer.renderer;
						if (renderer != null)
						{
							renderer.SetAllGraphicsDirty();
						}
					}
				}
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00005788 File Offset: 0x00003988
		private void DropReorder(Pawn pawn, List<Apparel> ordered, int dropIndex)
		{
			bool flag = dropIndex < 0 || this.dragRowDef == null;
			if (!flag)
			{
				Apparel apparel = GenCollection.FirstOrDefault<Apparel>(ordered, (Apparel a) => a.def.defName == this.dragRowDef);
				bool flag2 = apparel == null;
				if (!flag2)
				{
					float num = float.PositiveInfinity;
					float num2 = float.NegativeInfinity;
					for (int i = dropIndex - 1; i >= 0; i--)
					{
						bool flag3 = ordered[i].def.defName != this.dragRowDef;
						if (flag3)
						{
							num = Window_Tailor.OrderKey(pawn, ordered[i]);
							break;
						}
					}
					for (int j = dropIndex; j < ordered.Count; j++)
					{
						bool flag4 = ordered[j].def.defName != this.dragRowDef;
						if (flag4)
						{
							num2 = Window_Tailor.OrderKey(pawn, ordered[j]);
							break;
						}
					}
					bool flag5 = float.IsPositiveInfinity(num) && float.IsNegativeInfinity(num2);
					if (!flag5)
					{
						bool flag6 = float.IsPositiveInfinity(num);
						float num3;
						if (flag6)
						{
							num3 = num2 + 2f;
						}
						else
						{
							bool flag7 = float.IsNegativeInfinity(num2);
							if (flag7)
							{
								num3 = num - 2f;
							}
							else
							{
								num3 = (num + num2) / 2f;
							}
						}
						TailorAdjust orAdd = PerPawnAdjust.GetOrAdd(pawn, this.dragRowDef, TailorMadeMod.Settings.GetAdjust(this.dragRowDef));
						bool flag8 = orAdd != null;
						if (flag8)
						{
							orAdd.renderOrder += num3 - Window_Tailor.OrderKey(pawn, apparel);
						}
						this.sessionDirty = true;
						Pawn_DrawTracker drawer = pawn.Drawer;
						if (drawer != null)
						{
							PawnRenderer renderer = drawer.renderer;
							if (renderer != null)
							{
								renderer.SetAllGraphicsDirty();
							}
						}
					}
				}
			}
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00005938 File Offset: 0x00003B38
		private static bool IconBtn(Rect r, Texture2D tex, string tip)
		{
			Widgets.DrawHighlightIfMouseover(r);
			bool flag = !GenText.NullOrEmpty(tip);
			if (flag)
			{
				TooltipHandler.TipRegion(r, tip);
			}
			bool flag2 = tex == null;
			bool flag3;
			if (flag2)
			{
				flag3 = Widgets.ButtonText(r, "?", true, true, true, null);
			}
			else
			{
				flag3 = Widgets.ButtonImage(r, tex, true, null);
			}
			return flag3;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x0000599C File Offset: 0x00003B9C
		private static bool IconToggle(Rect r, Texture2D tex, bool on, string tip)
		{
			if (on)
			{
				Widgets.DrawBoxSolid(r, new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.25f));
			}
			Widgets.DrawHighlightIfMouseover(r);
			bool flag = !GenText.NullOrEmpty(tip);
			if (flag)
			{
				TooltipHandler.TipRegion(r, tip);
			}
			GUI.color = (on ? Palette.Accent : Color.white);
			bool flag2 = ((tex != null) ? Widgets.ButtonImage(r, tex, true, null) : Widgets.ButtonText(r, "H", true, true, true, null));
			GUI.color = Color.white;
			return flag2;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00005A54 File Offset: 0x00003C54
		private static string BuildXml(Pawn pawn, string defName)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			TailorAdjust tailorAdjust = PerPawnAdjust.Effective(pawn, defName);
			bool flag = settings.hiddenRenderDefs != null && settings.hiddenRenderDefs.Contains(defName);
			bool flag2 = settings.hairForceItems != null && settings.hairForceItems.Contains(defName);
			string text;
			if (pawn == null)
			{
				text = null;
			}
			else
			{
				ThingDef def = pawn.def;
				text = ((def != null) ? def.defName : null);
			}
			string text2 = text ?? "Human";
			string text3;
			if (pawn == null)
			{
				text3 = null;
			}
			else
			{
				Pawn_StoryTracker story = pawn.story;
				if (story == null)
				{
					text3 = null;
				}
				else
				{
					BodyTypeDef bodyType = story.bodyType;
					text3 = ((bodyType != null) ? bodyType.defName : null);
				}
			}
			string text4 = text3;
			return TailorExportImport.BuildDefXml(defName, text2, text4, tailorAdjust, flag, flag2, "");
		}

		// Token: 0x0400003C RID: 60
		private string selectedDef;

		// Token: 0x0400003D RID: 61
		private int rotTab = 2;

		// Token: 0x0400003E RID: 62
		private Vector2 listScroll;

		// Token: 0x0400003F RID: 63
		private bool dragging;

		// Token: 0x04000040 RID: 64
		private bool headerDragging;

		// Token: 0x04000041 RID: 65
		private string dragRowDef;

		// Token: 0x04000042 RID: 66
		private bool outlinePending;

		// Token: 0x04000043 RID: 67
		private float outlineEditAt;

		// Token: 0x04000044 RID: 68
		private bool sessionDirty;

		// Token: 0x04000045 RID: 69
		private string liveDef;

		// Token: 0x04000046 RID: 70
		private int liveRot = -1;

		// Token: 0x04000047 RID: 71
		private Vector2 liveOff;

		// Token: 0x04000048 RID: 72
		private float liveScale = 1f;

		// Token: 0x04000049 RID: 73
		private float liveAngle;

		// Token: 0x0400004A RID: 74
		private bool liveDirty;

		// Token: 0x0400004B RID: 75
		private float lastEdit;

		// Token: 0x0400004C RID: 76
		private readonly Dictionary<int, bool> appControlled = new Dictionary<int, bool>();

		// Token: 0x0400004D RID: 77
		private float appCacheAt = -1f;
	}
}
