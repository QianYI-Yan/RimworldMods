using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000006 RID: 6
	public static class ApparelClassifier
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002069 File Offset: 0x00000269
		public static void Clear()
		{
			ApparelClassifier.cache.Clear();
			ApparelClassifier.chestArt.Clear();
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002084 File Offset: 0x00000284
		public static ApparelClassInfo Info(ThingDef def)
		{
			bool flag = ((def != null) ? def.apparel : null) == null;
			ApparelClassInfo apparelClassInfo;
			if (flag)
			{
				apparelClassInfo = default(ApparelClassInfo);
			}
			else
			{
				ApparelClassInfo apparelClassInfo2;
				bool flag2 = ApparelClassifier.cache.TryGetValue(def.shortHash, out apparelClassInfo2);
				if (flag2)
				{
					apparelClassInfo = apparelClassInfo2;
				}
				else
				{
					apparelClassInfo2 = ApparelClassifier.Compute(def);
					ApparelClassifier.cache[def.shortHash] = apparelClassInfo2;
					apparelClassInfo = apparelClassInfo2;
				}
			}
			return apparelClassInfo;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020EC File Offset: 0x000002EC
		public static Vector2 BandFor(ApparelClass cls)
		{
			switch (cls)
			{
			case ApparelClass.Boots:
				return ApparelClassifier.BootsBand;
			case ApparelClass.Pants:
				return ApparelClassifier.PantsBand;
			case ApparelClass.Chest:
				return ApparelClassifier.ChestBand;
			}
			return ApparelClassifier.FullBand;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000213D File Offset: 0x0000033D
		public static bool IsBanded(ApparelClass cls)
		{
			return cls == ApparelClass.Boots || cls == ApparelClass.Pants || cls == ApparelClass.Chest;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002150 File Offset: 0x00000350
		public static bool ChestArtCached(ThingDef def, out bool confirmed)
		{
			bool flag = def == null;
			bool flag2;
			if (flag)
			{
				confirmed = false;
				flag2 = true;
			}
			else
			{
				flag2 = ApparelClassifier.chestArt.TryGetValue(def.shortHash, out confirmed);
			}
			return flag2;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002184 File Offset: 0x00000384
		public static bool ConfirmChestArt(ThingDef def, Texture2D southReadable)
		{
			bool flag = def == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag4;
				bool flag3 = ApparelClassifier.chestArt.TryGetValue(def.shortHash, out flag4);
				if (flag3)
				{
					flag2 = flag4;
				}
				else
				{
					bool flag5 = southReadable == null;
					if (flag5)
					{
						flag2 = false;
					}
					else
					{
						Rect rect = TexBake.AlphaBoundsUV(southReadable);
						flag4 = rect.height > 0.0001f && rect.yMin >= 0.4f;
						ApparelClassifier.chestArt[def.shortHash] = flag4;
						flag2 = flag4;
					}
				}
			}
			return flag2;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000220C File Offset: 0x0000040C
		private static ApparelClassInfo Compute(ThingDef def)
		{
			ApparelClassInfo apparelClassInfo = default(ApparelClassInfo);
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled || !settings.thigappeDetect;
			ApparelClassInfo apparelClassInfo2;
			if (flag)
			{
				apparelClassInfo2 = apparelClassInfo;
			}
			else
			{
				ApparelProperties apparel = def.apparel;
				List<string> tags = apparel.tags;
				bool flag2 = tags != null && tags.Count > 0;
				if (flag2)
				{
					bool flag3 = tags.Contains("THIGAPPE_IgnoreTag");
					if (flag3)
					{
						apparelClassInfo.ignore = true;
						return apparelClassInfo;
					}
					bool flag4 = settings.detectBoots && tags.Contains("THIGAPPE_BootsFlag");
					if (flag4)
					{
						apparelClassInfo.cls = ApparelClass.Boots;
						return apparelClassInfo;
					}
					bool flag5 = settings.detectPants && tags.Contains("THIGAPPE_PantsFlag");
					if (flag5)
					{
						apparelClassInfo.cls = ApparelClass.Pants;
						return apparelClassInfo;
					}
					bool flag6 = settings.detectArmor && tags.Contains("THIGAPPE_ArmorCoverageFlag") && tags.Contains("THIGAPPE_ArmorMainFlag");
					if (flag6)
					{
						apparelClassInfo.cls = ApparelClass.Armor;
						return apparelClassInfo;
					}
					bool flag7 = settings.detectBodysuit && tags.Contains("THIGAPPE_OnSkinFullCoverageFlag") && tags.Contains("THIGAPPE_OnSkinFullLayerFlag");
					if (flag7)
					{
						apparelClassInfo.cls = ApparelClass.Bodysuit;
						return apparelClassInfo;
					}
				}
				List<BodyPartGroupDef> bodyPartGroups = apparel.bodyPartGroups;
				bool flag8 = settings.detectBoots && bodyPartGroups != null && bodyPartGroups.Count == 1 && ApparelClassifier.IsGroup(bodyPartGroups[0], "Feet");
				if (flag8)
				{
					apparelClassInfo.cls = ApparelClass.Boots;
					apparelClassInfo2 = apparelClassInfo;
				}
				else
				{
					bool flag9 = settings.detectPants && bodyPartGroups != null && bodyPartGroups.Count == 1 && ApparelClassifier.IsGroup(bodyPartGroups[0], "Legs");
					if (flag9)
					{
						apparelClassInfo.cls = ApparelClass.Pants;
						apparelClassInfo2 = apparelClassInfo;
					}
					else
					{
						bool flag10 = settings.detectChest && ApparelClassifier.ContainsGroup(bodyPartGroups, "Torso") && !ApparelClassifier.ContainsGroup(bodyPartGroups, "Legs") && !ApparelClassifier.ContainsGroup(bodyPartGroups, "Feet");
						if (flag10)
						{
							apparelClassInfo.cls = ApparelClass.Chest;
							apparelClassInfo2 = apparelClassInfo;
						}
						else
						{
							bool flag11 = ApparelClassifier.ContainsGroup(bodyPartGroups, "Torso") && ApparelClassifier.ContainsGroup(bodyPartGroups, "Legs");
							if (flag11)
							{
								bool flag12 = settings.detectArmor && ApparelClassifier.LooksLikeArmor(def);
								if (flag12)
								{
									apparelClassInfo.cls = ApparelClass.Armor;
									return apparelClassInfo;
								}
								bool flag13 = settings.detectBodysuit && ApparelClassifier.HasLayer(apparel, ApparelLayerDefOf.OnSkin);
								if (flag13)
								{
									apparelClassInfo.cls = ApparelClass.Bodysuit;
									return apparelClassInfo;
								}
							}
							apparelClassInfo2 = apparelClassInfo;
						}
					}
				}
			}
			return apparelClassInfo2;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000024B6 File Offset: 0x000006B6
		private static bool IsGroup(BodyPartGroupDef g, string defName)
		{
			return g != null && g.defName == defName;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000024CC File Offset: 0x000006CC
		private static bool ContainsGroup(List<BodyPartGroupDef> list, string defName)
		{
			bool flag = list == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				for (int i = 0; i < list.Count; i++)
				{
					bool flag3 = list[i] != null && list[i].defName == defName;
					if (flag3)
					{
						return true;
					}
				}
				flag2 = false;
			}
			return flag2;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002528 File Offset: 0x00000728
		private static bool HasLayer(ApparelProperties ap, ApparelLayerDef layer)
		{
			List<ApparelLayerDef> layers = ap.layers;
			bool flag = layers == null || layer == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				for (int i = 0; i < layers.Count; i++)
				{
					bool flag3 = layers[i] == layer;
					if (flag3)
					{
						return true;
					}
				}
				flag2 = false;
			}
			return flag2;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002580 File Offset: 0x00000780
		private static bool LooksLikeArmor(ThingDef def)
		{
			bool flag = ApparelClassifier.Contains(def.defName, "armor");
			bool flag2;
			if (flag)
			{
				flag2 = true;
			}
			else
			{
				bool flag3 = ApparelClassifier.Contains(def.label, "armor");
				if (flag3)
				{
					flag2 = true;
				}
				else
				{
					bool flag4 = ApparelClassifier.Contains(def.description, "armor") || ApparelClassifier.Contains(def.description, "protective");
					flag2 = flag4;
				}
			}
			return flag2;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000025F0 File Offset: 0x000007F0
		private static bool Contains(string s, string sub)
		{
			return !GenText.NullOrEmpty(s) && s.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x0400000B RID: 11
		public static readonly Vector2 FullBand = new Vector2(0f, 1f);

		// Token: 0x0400000C RID: 12
		public static readonly Vector2 BootsBand = new Vector2(0f, 0.2f);

		// Token: 0x0400000D RID: 13
		public static readonly Vector2 PantsBand = new Vector2(0f, 0.58f);

		// Token: 0x0400000E RID: 14
		public static readonly Vector2 ChestBand = new Vector2(0.45f, 1f);

		// Token: 0x0400000F RID: 15
		private const float ChestArtBottomThreshold = 0.4f;

		// Token: 0x04000010 RID: 16
		private const string IgnoreTag = "THIGAPPE_IgnoreTag";

		// Token: 0x04000011 RID: 17
		private const string BootsFlag = "THIGAPPE_BootsFlag";

		// Token: 0x04000012 RID: 18
		private const string PantsFlag = "THIGAPPE_PantsFlag";

		// Token: 0x04000013 RID: 19
		private const string ArmorCoverageFlag = "THIGAPPE_ArmorCoverageFlag";

		// Token: 0x04000014 RID: 20
		private const string ArmorMainFlag = "THIGAPPE_ArmorMainFlag";

		// Token: 0x04000015 RID: 21
		private const string OnSkinFullCoverageFlag = "THIGAPPE_OnSkinFullCoverageFlag";

		// Token: 0x04000016 RID: 22
		private const string OnSkinFullLayerFlag = "THIGAPPE_OnSkinFullLayerFlag";

		// Token: 0x04000017 RID: 23
		private static readonly Dictionary<ushort, ApparelClassInfo> cache = new Dictionary<ushort, ApparelClassInfo>();

		// Token: 0x04000018 RID: 24
		private static readonly Dictionary<ushort, bool> chestArt = new Dictionary<ushort, bool>();
	}
}
