using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000021 RID: 33
	public class TailorMadeSettings : ModSettings
	{
		// Token: 0x060000AB RID: 171 RVA: 0x0000B0B8 File Offset: 0x000092B8
		public bool RaceBodyOverride(ThingDef race, out HarBodyMode mode, out string provider)
		{
			mode = this.harBodyMode;
			provider = this.forcedBodyTex ?? "";
			string text;
			bool flag = race == null || this.raceBodyMap == null || !this.raceBodyMap.TryGetValue(race.defName, out text) || GenText.NullOrEmpty(text);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = text == "off";
				if (flag3)
				{
					mode = HarBodyMode.Off;
					provider = "";
					flag2 = true;
				}
				else
				{
					bool flag4 = text == "auto";
					if (flag4)
					{
						mode = HarBodyMode.AutoResize;
						provider = "";
						flag2 = true;
					}
					else
					{
						bool flag5 = text == "force";
						if (flag5)
						{
							mode = HarBodyMode.ForceCB2;
							provider = "";
							flag2 = true;
						}
						else
						{
							bool flag6 = text.StartsWith("force:");
							if (flag6)
							{
								mode = HarBodyMode.ForceCB2;
								provider = text.Substring(6);
								flag2 = true;
							}
							else
							{
								flag2 = false;
							}
						}
					}
				}
			}
			return flag2;
		}

		// Token: 0x060000AC RID: 172 RVA: 0x0000B198 File Offset: 0x00009398
		public ClipMode ClipModeFor(BodyTypeDef bt)
		{
			ClipMode clipMode;
			bool flag = bt != null && this.bodyClipMode != null && this.bodyClipMode.TryGetValue(bt.defName, out clipMode);
			ClipMode clipMode2;
			if (flag)
			{
				clipMode2 = clipMode;
			}
			else
			{
				clipMode2 = this.defaultClipMode;
			}
			return clipMode2;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x0000B1DC File Offset: 0x000093DC
		public bool ShouldClipBody(ThingDef race, BodyTypeDef bt)
		{
			bool flag = bt == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				ClipMode clipMode = this.ClipModeFor(bt);
				ClipMode clipMode2 = clipMode;
				flag2 = clipMode2 == ClipMode.Always || (clipMode2 != ClipMode.Never && BodyTexCache.HasRetexture(race, bt));
			}
			return flag2;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x0000B220 File Offset: 0x00009420
		public static bool IsFemaleBody(BodyTypeDef bt)
		{
			bool flag = bt == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = bt == BodyTypeDefOf.Female;
				flag2 = flag3 || bt.defName.IndexOf("female", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return flag2;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x0000B268 File Offset: 0x00009468
		public TailorAdjust GetAdjust(string defName)
		{
			bool flag = GenText.NullOrEmpty(defName) || this.adjustments == null;
			TailorAdjust tailorAdjust;
			if (flag)
			{
				tailorAdjust = null;
			}
			else
			{
				TailorAdjust tailorAdjust2;
				tailorAdjust = (this.adjustments.TryGetValue(defName, out tailorAdjust2) ? tailorAdjust2 : null);
			}
			return tailorAdjust;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x0000B2AC File Offset: 0x000094AC
		public TailorAdjust GetOrAddAdjust(string defName)
		{
			bool flag = this.adjustments == null;
			if (flag)
			{
				this.adjustments = new Dictionary<string, TailorAdjust>();
			}
			TailorAdjust tailorAdjust;
			bool flag2 = !this.adjustments.TryGetValue(defName, out tailorAdjust);
			if (flag2)
			{
				tailorAdjust = new TailorAdjust();
				this.adjustments[defName] = tailorAdjust;
			}
			return tailorAdjust;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x0000B304 File Offset: 0x00009504
		public bool BodyTypeEnabled(BodyTypeDef def)
		{
			bool flag;
			return def != null && (!this.bodyTypeEnabled.TryGetValue(def.defName, out flag) || flag);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x0000B334 File Offset: 0x00009534
		public static bool IsRefitableLayer(ApparelLayerDef def)
		{
			bool flag = def == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = def == ApparelLayerDefOf.Overhead || def == ApparelLayerDefOf.EyeCover || def == ApparelLayerDefOf.Belt;
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag4 = ApparelLayerDefOf.Belt != null && def.drawOrder >= ApparelLayerDefOf.Belt.drawOrder;
					flag2 = !flag4;
				}
			}
			return flag2;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x0000B3A0 File Offset: 0x000095A0
		public bool LayerEnabled(ApparelLayerDef def)
		{
			bool flag = def == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = !TailorMadeSettings.IsRefitableLayer(def);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag5;
					bool flag4 = this.layerEnabled.TryGetValue(def.defName, out flag5);
					if (flag4)
					{
						flag2 = flag5;
					}
					else
					{
						flag2 = def != ApparelLayerDefOf.Shell;
					}
				}
			}
			return flag2;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x0000B3F8 File Offset: 0x000095F8
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<bool>(ref this.enabled, "enabled", true, false);
			Scribe_Values.Look<bool>(ref this.thigappeDetect, "thigappeDetect", true, false);
			Scribe_Values.Look<bool>(ref this.detectBoots, "detectBoots", true, false);
			Scribe_Values.Look<bool>(ref this.detectBodysuit, "detectBodysuit", true, false);
			Scribe_Values.Look<bool>(ref this.detectPants, "detectPants", true, false);
			Scribe_Values.Look<bool>(ref this.detectChest, "detectChest", true, false);
			Scribe_Values.Look<bool>(ref this.detectArmor, "detectArmor", true, false);
			Scribe_Values.Look<bool>(ref this.sizedApparelDefer, "sizedApparelDefer", true, false);
			Scribe_Values.Look<bool>(ref this.paperPatternDefer, "paperPatternDefer", true, false);
			Scribe_Values.Look<bool>(ref this.femaleBodyVariants, "femaleBodyVariants", true, false);
			Scribe_Values.Look<bool>(ref this.autoFit, "autoFit", true, false);
			Scribe_Values.Look<bool>(ref this.uniformScale, "uniformScale", false, false);
			Scribe_Values.Look<HarBodyMode>(ref this.harBodyMode, "harBodyMode", HarBodyMode.AutoResize, false);
			Scribe_Values.Look<string>(ref this.forcedBodyTex, "forcedBodyTex", "", false);
			bool flag = this.forcedBodyTex == null;
			if (flag)
			{
				this.forcedBodyTex = "";
			}
			Scribe_Collections.Look<string, string>(ref this.raceBodyMap, "raceBodyMap", 1, 1);
			bool flag2 = this.raceBodyMap == null;
			if (flag2)
			{
				this.raceBodyMap = new Dictionary<string, string>();
			}
			Scribe_Values.Look<bool>(ref this.unlockRestrictedApparel, "unlockRestrictedApparel", true, false);
			Scribe_Values.Look<bool>(ref this.forceFemaleBody, "forceFemaleBody", true, false);
			Scribe_Values.Look<bool>(ref this.clipFemaleOnly, "clipFemaleOnly", true, false);
			Scribe_Values.Look<ClipMode>(ref this.defaultClipMode, "defaultClipMode", ClipMode.Always, false);
			Scribe_Values.Look<bool>(ref this.clipSettingsMigrated, "clipSettingsMigrated", false, false);
			Scribe_Values.Look<bool>(ref this.clipAutoToAlwaysMigrated, "clipAutoToAlwaysMigrated", false, false);
			Scribe_Collections.Look<string, ClipMode>(ref this.bodyClipMode, "bodyClipMode", 1, 1);
			bool flag3 = this.bodyClipMode == null;
			if (flag3)
			{
				this.bodyClipMode = new Dictionary<string, ClipMode>();
			}
			bool flag4 = Scribe.mode == 2 && !this.clipSettingsMigrated;
			if (flag4)
			{
				bool flag5 = !this.clipFemaleOnly;
				if (flag5)
				{
					this.defaultClipMode = ClipMode.Always;
				}
				this.clipSettingsMigrated = true;
			}
			bool flag6 = Scribe.mode == 2 && !this.clipAutoToAlwaysMigrated;
			if (flag6)
			{
				bool flag7 = this.defaultClipMode == ClipMode.Auto;
				if (flag7)
				{
					this.defaultClipMode = ClipMode.Always;
				}
				this.clipAutoToAlwaysMigrated = true;
			}
			Scribe_Collections.Look<string>(ref this.hiddenRenderDefs, "hiddenRenderDefs", 1);
			bool flag8 = this.hiddenRenderDefs == null;
			if (flag8)
			{
				this.hiddenRenderDefs = new HashSet<string>();
			}
			Scribe_Values.Look<bool>(ref this.keepHairUnderBodyApparel, "keepHairUnderBodyApparel", true, false);
			Scribe_Collections.Look<string>(ref this.hairForceItems, "hairForceItems", 1);
			bool flag9 = this.hairForceItems == null;
			if (flag9)
			{
				this.hairForceItems = new HashSet<string>();
			}
			Scribe_Values.Look<bool>(ref this.linkFacings, "linkFacings", true, false);
			bool flag10 = Scribe.mode == 1 && this.adjustments != null;
			if (flag10)
			{
				foreach (string text in (from kv in this.adjustments
					where kv.Value == null || kv.Value.IsDefault
					select kv.Key).ToList<string>())
				{
					this.adjustments.Remove(text);
				}
			}
			Scribe_Collections.Look<string, TailorAdjust>(ref this.adjustments, "adjustments", 1, 2);
			bool flag11 = this.adjustments == null;
			if (flag11)
			{
				this.adjustments = new Dictionary<string, TailorAdjust>();
			}
			Scribe_Values.Look<int>(ref this.maxResolution, "maxResolution", 512, false);
			Scribe_Values.Look<bool>(ref this.trilinearFilter, "trilinearFilter", true, false);
			Scribe_Values.Look<bool>(ref this.unlockResolution, "unlockResolution", false, false);
			Scribe_Values.Look<bool>(ref this.sharpResampling, "sharpResampling", false, false);
			Scribe_Values.Look<bool>(ref this.bodyUnlockResolution, "bodyUnlockResolution", false, false);
			Scribe_Values.Look<bool>(ref this.fitSideViews, "fitSideViews", true, false);
			Scribe_Values.Look<bool>(ref this.sideFitMigrated, "sideFitMigrated", false, false);
			bool flag12 = Scribe.mode == 2 && !this.sideFitMigrated;
			if (flag12)
			{
				this.fitSideViews = true;
				this.sideFitMigrated = true;
			}
			Scribe_Values.Look<float>(ref this.sideOutlineBoost, "sideOutlineBoost", 2f, false);
			Scribe_Values.Look<bool>(ref this.bodyMaskOutline, "bodyMaskOutline", true, false);
			Scribe_Values.Look<int>(ref this.outlinePixels, "outlinePixels", 2, false);
			Scribe_Collections.Look<string, bool>(ref this.bodyTypeEnabled, "bodyTypeEnabled", 1, 1);
			Scribe_Collections.Look<string, bool>(ref this.layerEnabled, "layerEnabled", 1, 1);
			bool flag13 = this.bodyTypeEnabled == null;
			if (flag13)
			{
				this.bodyTypeEnabled = new Dictionary<string, bool>();
			}
			bool flag14 = this.layerEnabled == null;
			if (flag14)
			{
				this.layerEnabled = new Dictionary<string, bool>();
			}
		}

		// Token: 0x04000080 RID: 128
		public bool enabled = true;

		// Token: 0x04000081 RID: 129
		public bool thigappeDetect = true;

		// Token: 0x04000082 RID: 130
		public bool detectBoots = true;

		// Token: 0x04000083 RID: 131
		public bool detectBodysuit = true;

		// Token: 0x04000084 RID: 132
		public bool detectPants = true;

		// Token: 0x04000085 RID: 133
		public bool detectChest = true;

		// Token: 0x04000086 RID: 134
		public bool detectArmor = true;

		// Token: 0x04000087 RID: 135
		public bool sizedApparelDefer = true;

		// Token: 0x04000088 RID: 136
		public bool paperPatternDefer = true;

		// Token: 0x04000089 RID: 137
		public bool femaleBodyVariants = true;

		// Token: 0x0400008A RID: 138
		public bool autoFit = true;

		// Token: 0x0400008B RID: 139
		public bool uniformScale = false;

		// Token: 0x0400008C RID: 140
		public HarBodyMode harBodyMode = HarBodyMode.AutoResize;

		// Token: 0x0400008D RID: 141
		public string forcedBodyTex = "";

		// Token: 0x0400008E RID: 142
		public Dictionary<string, string> raceBodyMap = new Dictionary<string, string>();

		// Token: 0x0400008F RID: 143
		public bool unlockRestrictedApparel = true;

		// Token: 0x04000090 RID: 144
		public bool forceFemaleBody = true;

		// Token: 0x04000091 RID: 145
		public bool clipFemaleOnly = true;

		// Token: 0x04000092 RID: 146
		public ClipMode defaultClipMode = ClipMode.Always;

		// Token: 0x04000093 RID: 147
		public Dictionary<string, ClipMode> bodyClipMode = new Dictionary<string, ClipMode>();

		// Token: 0x04000094 RID: 148
		private bool clipSettingsMigrated;

		// Token: 0x04000095 RID: 149
		private bool clipAutoToAlwaysMigrated;

		// Token: 0x04000096 RID: 150
		public HashSet<string> hiddenRenderDefs = new HashSet<string>();

		// Token: 0x04000097 RID: 151
		public bool keepHairUnderBodyApparel = true;

		// Token: 0x04000098 RID: 152
		public HashSet<string> hairForceItems = new HashSet<string>();

		// Token: 0x04000099 RID: 153
		public bool linkFacings = true;

		// Token: 0x0400009A RID: 154
		public Dictionary<string, TailorAdjust> adjustments = new Dictionary<string, TailorAdjust>();

		// Token: 0x0400009B RID: 155
		public int maxResolution = 512;

		// Token: 0x0400009C RID: 156
		public bool trilinearFilter = true;

		// Token: 0x0400009D RID: 157
		public bool unlockResolution = false;

		// Token: 0x0400009E RID: 158
		public bool sharpResampling = false;

		// Token: 0x0400009F RID: 159
		public bool bodyUnlockResolution = false;

		// Token: 0x040000A0 RID: 160
		public bool bodyMaskOutline = true;

		// Token: 0x040000A1 RID: 161
		public int outlinePixels = 2;

		// Token: 0x040000A2 RID: 162
		public bool fitSideViews = true;

		// Token: 0x040000A3 RID: 163
		private bool sideFitMigrated;

		// Token: 0x040000A4 RID: 164
		public float sideOutlineBoost = 2f;

		// Token: 0x040000A5 RID: 165
		public Dictionary<string, bool> bodyTypeEnabled = new Dictionary<string, bool>();

		// Token: 0x040000A6 RID: 166
		public Dictionary<string, bool> layerEnabled = new Dictionary<string, bool>();
	}
}
