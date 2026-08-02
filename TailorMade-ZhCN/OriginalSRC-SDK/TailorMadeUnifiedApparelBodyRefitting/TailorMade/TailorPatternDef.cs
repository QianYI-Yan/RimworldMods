using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200002B RID: 43
	public class TailorPatternDef : Def
	{
		// Token: 0x060000EE RID: 238 RVA: 0x0000E354 File Offset: 0x0000C554
		public float GetAdjustAngle(Rot4 rot)
		{
			bool flag = rot == Rot4.North;
			float num;
			if (flag)
			{
				num = this.adjustAngleNorth;
			}
			else
			{
				bool flag2 = rot == Rot4.East;
				if (flag2)
				{
					num = this.adjustAngleEast;
				}
				else
				{
					bool flag3 = rot == Rot4.West;
					if (flag3)
					{
						num = this.adjustAngleWest;
					}
					else
					{
						num = this.adjustAngleSouth;
					}
				}
			}
			return num;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000E3B4 File Offset: 0x0000C5B4
		public Vector2 GetAdjustOffset(Rot4 rot)
		{
			bool flag = rot == Rot4.North;
			Vector2 vector;
			if (flag)
			{
				vector = this.adjustNorth;
			}
			else
			{
				bool flag2 = rot == Rot4.East;
				if (flag2)
				{
					vector = this.adjustEast;
				}
				else
				{
					bool flag3 = rot == Rot4.West;
					if (flag3)
					{
						vector = this.adjustWest;
					}
					else
					{
						vector = this.adjustSouth;
					}
				}
			}
			return vector;
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000E414 File Offset: 0x0000C614
		public float GetAdjustScale(Rot4 rot)
		{
			bool flag = rot == Rot4.North;
			float num;
			if (flag)
			{
				num = this.adjustScaleNorth;
			}
			else
			{
				bool flag2 = rot == Rot4.East;
				if (flag2)
				{
					num = this.adjustScaleEast;
				}
				else
				{
					bool flag3 = rot == Rot4.West;
					if (flag3)
					{
						num = this.adjustScaleWest;
					}
					else
					{
						num = this.adjustScaleSouth;
					}
				}
			}
			return num;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000E474 File Offset: 0x0000C674
		public void CompilePatterns()
		{
			this.reTargetPackage = this.Compile(this.targetPackageIds);
			this.reIgnorePackage = this.Compile(this.ignorePackageIds);
			this.reTargetApparel = this.Compile(this.targetApparelDefs);
			this.reIgnoreApparel = this.Compile(this.ignoreApparelDefs);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000E4CC File Offset: 0x0000C6CC
		private List<Regex> Compile(List<string> patterns)
		{
			List<Regex> list = new List<Regex>();
			bool flag = patterns == null;
			List<Regex> list2;
			if (flag)
			{
				list2 = list;
			}
			else
			{
				foreach (string text in patterns)
				{
					bool flag2 = GenText.NullOrEmpty(text);
					if (!flag2)
					{
						try
						{
							list.Add(new Regex(text, RegexOptions.IgnoreCase | RegexOptions.Compiled));
						}
						catch
						{
							Log.Warning("[TailorMade] Bad regex in " + this.defName + ": " + text);
						}
					}
				}
				list2 = list;
			}
			return list2;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000E584 File Offset: 0x0000C784
		public bool MatchesApparel(ThingDef apparelDef)
		{
			ModContentPack modContentPack = apparelDef.modContentPack;
			string text = ((modContentPack != null) ? modContentPack.PackageId : null) ?? "";
			bool flag = this.reTargetPackage.Count > 0 && !TailorPatternDef.AnyMatch(this.reTargetPackage, text);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = this.reIgnorePackage.Count > 0 && TailorPatternDef.AnyMatch(this.reIgnorePackage, text);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag4 = this.reTargetApparel.Count > 0 && !TailorPatternDef.AnyMatch(this.reTargetApparel, apparelDef.defName);
					if (flag4)
					{
						flag2 = false;
					}
					else
					{
						bool flag5 = this.reIgnoreApparel.Count > 0 && TailorPatternDef.AnyMatch(this.reIgnoreApparel, apparelDef.defName);
						flag2 = !flag5;
					}
				}
			}
			return flag2;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000E660 File Offset: 0x0000C860
		private static bool AnyMatch(List<Regex> res, string s)
		{
			for (int i = 0; i < res.Count; i++)
			{
				bool flag = res[i].IsMatch(s);
				if (flag)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000E6A0 File Offset: 0x0000C8A0
		public Vector2 GetOffset(Rot4 rot)
		{
			bool flag = rot == Rot4.North;
			Vector2 vector;
			if (flag)
			{
				vector = this.offsetNorth;
			}
			else
			{
				bool flag2 = rot == Rot4.East;
				if (flag2)
				{
					vector = this.offsetEast;
				}
				else
				{
					bool flag3 = rot == Rot4.West;
					if (flag3)
					{
						vector = this.offsetWest;
					}
					else
					{
						vector = this.offsetSouth;
					}
				}
			}
			return vector;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000E700 File Offset: 0x0000C900
		public float GetScale(Rot4 rot)
		{
			bool flag = rot == Rot4.North;
			float num;
			if (flag)
			{
				num = this.scaleNorth;
			}
			else
			{
				bool flag2 = rot == Rot4.East;
				if (flag2)
				{
					num = this.scaleEast;
				}
				else
				{
					bool flag3 = rot == Rot4.West;
					if (flag3)
					{
						num = this.scaleWest;
					}
					else
					{
						num = this.scaleSouth;
					}
				}
			}
			return num;
		}

		// Token: 0x040000D1 RID: 209
		public string raceName = "Human";

		// Token: 0x040000D2 RID: 210
		public BodyTypeDef bodyType;

		// Token: 0x040000D3 RID: 211
		public string apparelLayer;

		// Token: 0x040000D4 RID: 212
		public string maskPath;

		// Token: 0x040000D5 RID: 213
		public bool ignore;

		// Token: 0x040000D6 RID: 214
		public bool autoFit = true;

		// Token: 0x040000D7 RID: 215
		public bool conform = true;

		// Token: 0x040000D8 RID: 216
		public float renderOrder;

		// Token: 0x040000D9 RID: 217
		public bool hide;

		// Token: 0x040000DA RID: 218
		public bool keepHair;

		// Token: 0x040000DB RID: 219
		public Vector2 offsetNorth = Vector2.zero;

		// Token: 0x040000DC RID: 220
		public Vector2 offsetEast = Vector2.zero;

		// Token: 0x040000DD RID: 221
		public Vector2 offsetSouth = Vector2.zero;

		// Token: 0x040000DE RID: 222
		public Vector2 offsetWest = Vector2.zero;

		// Token: 0x040000DF RID: 223
		public float scaleNorth = 1f;

		// Token: 0x040000E0 RID: 224
		public float scaleEast = 1f;

		// Token: 0x040000E1 RID: 225
		public float scaleSouth = 1f;

		// Token: 0x040000E2 RID: 226
		public float scaleWest = 1f;

		// Token: 0x040000E3 RID: 227
		public Vector2 adjustNorth = Vector2.zero;

		// Token: 0x040000E4 RID: 228
		public Vector2 adjustEast = Vector2.zero;

		// Token: 0x040000E5 RID: 229
		public Vector2 adjustSouth = Vector2.zero;

		// Token: 0x040000E6 RID: 230
		public Vector2 adjustWest = Vector2.zero;

		// Token: 0x040000E7 RID: 231
		public float adjustScaleNorth = 1f;

		// Token: 0x040000E8 RID: 232
		public float adjustScaleEast = 1f;

		// Token: 0x040000E9 RID: 233
		public float adjustScaleSouth = 1f;

		// Token: 0x040000EA RID: 234
		public float adjustScaleWest = 1f;

		// Token: 0x040000EB RID: 235
		public float adjustAngleNorth;

		// Token: 0x040000EC RID: 236
		public float adjustAngleEast;

		// Token: 0x040000ED RID: 237
		public float adjustAngleSouth;

		// Token: 0x040000EE RID: 238
		public float adjustAngleWest;

		// Token: 0x040000EF RID: 239
		public List<string> targetPackageIds = new List<string>();

		// Token: 0x040000F0 RID: 240
		public List<string> ignorePackageIds = new List<string>();

		// Token: 0x040000F1 RID: 241
		public List<string> targetApparelDefs = new List<string>();

		// Token: 0x040000F2 RID: 242
		public List<string> ignoreApparelDefs = new List<string>();

		// Token: 0x040000F3 RID: 243
		[Unsaved(false)]
		private List<Regex> reTargetPackage;

		// Token: 0x040000F4 RID: 244
		[Unsaved(false)]
		private List<Regex> reIgnorePackage;

		// Token: 0x040000F5 RID: 245
		[Unsaved(false)]
		private List<Regex> reTargetApparel;

		// Token: 0x040000F6 RID: 246
		[Unsaved(false)]
		private List<Regex> reIgnoreApparel;
	}
}
