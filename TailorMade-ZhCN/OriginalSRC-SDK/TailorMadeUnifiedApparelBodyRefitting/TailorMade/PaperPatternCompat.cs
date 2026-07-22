using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000024 RID: 36
	public static class PaperPatternCompat
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x0000D2D0 File Offset: 0x0000B4D0
		public static bool Active
		{
			get
			{
				PaperPatternCompat.Resolve();
				return PaperPatternCompat.graphicType != null && !PaperPatternCompat.broken;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x0000D300 File Offset: 0x0000B500
		public static bool DeferEnabled
		{
			get
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				return settings != null && settings.paperPatternDefer && PaperPatternCompat.Active;
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x0000D32C File Offset: 0x0000B52C
		private static void Resolve()
		{
			bool flag = PaperPatternCompat.resolved;
			if (!flag)
			{
				PaperPatternCompat.resolved = true;
				try
				{
					PaperPatternCompat.graphicType = AccessTools.TypeByName("ApparelPaperPattern.Graphic_RenderTextureR1");
					bool flag2 = PaperPatternCompat.graphicType == null;
					if (!flag2)
					{
						Type type = AccessTools.TypeByName("ApparelPaperPattern.MyApparelGraphicRecordGetter");
						bool flag3 = type != null;
						if (flag3)
						{
							PaperPatternCompat.getDef = AccessTools.Method(type, "GetDef", null, null);
						}
						PaperPatternCompat.tunerType = AccessTools.TypeByName("ApparelPaperPattern.APPControllerWindow");
						bool flag4 = PaperPatternCompat.tunerType != null;
						if (flag4)
						{
							PaperPatternCompat.tunerInstance = AccessTools.Field(PaperPatternCompat.tunerType, "instance");
						}
						bool flag5 = PaperPatternCompat.getDef == null;
						if (flag5)
						{
							Log.Warning("[TailorMade] Apparel Paper Pattern detected but MyApparelGraphicRecordGetter.GetDef did not resolve — its API drifted. TailorMade will assume APP handles every apparel it MIGHT handle (missing-texture fallbacks render unfitted instead of risking an APP crash).");
						}
						else
						{
							Log.Message("[TailorMade] Apparel Paper Pattern detected — deferring to it (and its THIGAPPE pattern packs) for apparel it re-renders. Exempt items in APP's tuner to hand them to TailorMade; toggle in mod settings.");
						}
					}
				}
				catch (Exception ex)
				{
					PaperPatternCompat.broken = true;
					Log.Warning("[TailorMade] Apparel Paper Pattern compat failed to initialize: " + ex.Message);
				}
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x0000D430 File Offset: 0x0000B630
		public static bool TunerAvailable
		{
			get
			{
				PaperPatternCompat.Resolve();
				return PaperPatternCompat.tunerType != null && !PaperPatternCompat.broken;
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000D460 File Offset: 0x0000B660
		public static void OpenTuner()
		{
			bool flag = !PaperPatternCompat.TunerAvailable;
			if (!flag)
			{
				try
				{
					Window window = ((PaperPatternCompat.tunerInstance != null) ? ((Window)PaperPatternCompat.tunerInstance.GetValue(null)) : null);
					bool flag2 = window == null;
					if (flag2)
					{
						window = (Window)Activator.CreateInstance(PaperPatternCompat.tunerType, true);
						FieldInfo fieldInfo = PaperPatternCompat.tunerInstance;
						if (fieldInfo != null)
						{
							fieldInfo.SetValue(null, window);
						}
					}
					window.layer = 1;
					bool flag3 = !window.IsOpen;
					if (flag3)
					{
						Find.WindowStack.Add(window);
					}
					else
					{
						Find.WindowStack.Notify_ClickedInsideWindow(window);
					}
				}
				catch (Exception ex)
				{
					Log.Warning("[TailorMade] Could not open Apparel Paper Pattern's tuner window: " + ex.Message);
				}
			}
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000D530 File Offset: 0x0000B730
		public static bool ControlledByApp(Apparel apparel, BodyTypeDef bodyType)
		{
			return PaperPatternCompat.DeferEnabled && PaperPatternCompat.WillHandle(apparel, bodyType);
		}

		// Token: 0x060000CA RID: 202 RVA: 0x0000D554 File Offset: 0x0000B754
		public static bool IsAppGraphic(Graphic g)
		{
			return g != null && PaperPatternCompat.Active && PaperPatternCompat.graphicType.IsInstanceOfType(g);
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000D580 File Offset: 0x0000B780
		public static bool DeferApparel(Graphic g)
		{
			return PaperPatternCompat.DeferEnabled && PaperPatternCompat.IsAppGraphic(g);
		}

		// Token: 0x060000CC RID: 204 RVA: 0x0000D5A4 File Offset: 0x0000B7A4
		public static bool WillHandle(Apparel apparel, BodyTypeDef bodyType)
		{
			bool flag = !PaperPatternCompat.Active || apparel == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = PaperPatternCompat.getDef == null;
				if (flag3)
				{
					flag2 = true;
				}
				else
				{
					try
					{
						Pawn wearer = apparel.Wearer;
						string text;
						if (wearer == null)
						{
							text = null;
						}
						else
						{
							ThingDef def = wearer.def;
							text = ((def != null) ? def.defName : null);
						}
						string text2 = text;
						flag2 = PaperPatternCompat.getDef.Invoke(null, new object[] { text2, bodyType, apparel }) != null;
					}
					catch (Exception ex)
					{
						PaperPatternCompat.broken = true;
						string text3 = "[TailorMade] Apparel Paper Pattern compat disabled (GetDef threw): ";
						Exception ex2 = ex;
						Log.WarningOnce(text3 + ((ex2 != null) ? ex2.ToString() : null), 2048023042);
						flag2 = false;
					}
				}
			}
			return flag2;
		}

		// Token: 0x040000B2 RID: 178
		public const string HarmonyId = "rimworld.Nals.ApparelPaperPattern";

		// Token: 0x040000B3 RID: 179
		private static bool resolved;

		// Token: 0x040000B4 RID: 180
		private static bool broken;

		// Token: 0x040000B5 RID: 181
		private static Type graphicType;

		// Token: 0x040000B6 RID: 182
		private static MethodInfo getDef;

		// Token: 0x040000B7 RID: 183
		private static Type tunerType;

		// Token: 0x040000B8 RID: 184
		private static FieldInfo tunerInstance;
	}
}
