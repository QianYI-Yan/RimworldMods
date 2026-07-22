using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000007 RID: 7
	public static class BodyTexCache
	{
		// Token: 0x06000010 RID: 16 RVA: 0x00002680 File Offset: 0x00000880
		public static string BasePathFor(ThingDef race, BodyTypeDef bodyType)
		{
			bool flag = bodyType == null;
			string text;
			if (flag)
			{
				text = null;
			}
			else
			{
				string text2 = HarSupport.TryGetBodyPath(race, bodyType);
				bool flag2 = !GenText.NullOrEmpty(text2);
				if (flag2)
				{
					text = text2;
				}
				else
				{
					text = bodyType.bodyNakedGraphicPath;
				}
			}
			return text;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000026C0 File Offset: 0x000008C0
		public static BodyTexCache.Entry Get(ThingDef race, BodyTypeDef bodyType, bool female = false)
		{
			ValueTuple<ushort, ushort, bool> valueTuple = new ValueTuple<ushort, ushort, bool>(race.shortHash, bodyType.index, female);
			BodyTexCache.Entry entry;
			bool flag = BodyTexCache.cache.TryGetValue(valueTuple, out entry);
			BodyTexCache.Entry entry2;
			if (flag)
			{
				entry2 = entry;
			}
			else
			{
				BodyTexCache.Entry entry3 = BodyTexCache.Build(race, bodyType, female);
				BodyTexCache.cache[valueTuple] = entry3;
				entry2 = entry3;
			}
			return entry2;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002718 File Offset: 0x00000918
		private static BodyTexCache.Entry Build(ThingDef race, BodyTypeDef bodyType, bool female)
		{
			BodyTexCache.Entry entry = new BodyTexCache.Entry();
			TailorMadeSettings settings = TailorMadeMod.Settings;
			string text = HarSupport.TryGetBodyPath(race, bodyType);
			string bodyNakedGraphicPath = bodyType.bodyNakedGraphicPath;
			bool flag = !GenText.NullOrEmpty(text) && text != bodyNakedGraphicPath && !GenText.NullOrEmpty(bodyNakedGraphicPath);
			HarBodyMode harBodyMode;
			string text2;
			bool flag2 = settings.RaceBodyOverride(race, out harBodyMode, out text2);
			string text3 = ((flag && harBodyMode == HarBodyMode.ForceCB2) ? bodyNakedGraphicPath : (text ?? bodyNakedGraphicPath));
			bool flag3 = GenText.NullOrEmpty(text3);
			BodyTexCache.Entry entry2;
			if (flag3)
			{
				entry2 = entry;
			}
			else
			{
				bool flag4 = harBodyMode == HarBodyMode.ForceCB2 && !GenText.NullOrEmpty(text2) && (flag || flag2);
				string text4 = ((!female) ? null : (flag4 ? FemaleBodyVariantsCompat.FemaleVariantFromProvider(text3, text2) : FemaleBodyVariantsCompat.FemaleVariant(text3)));
				RotTexSet rotTexSet = (flag4 ? TexBake.ResolveRot(text4 ?? text3, false, null, false, text2) : TexBake.ResolveRot(text4 ?? text3, false, null, false, null));
				bool flag5 = !rotTexSet.valid && text4 != null;
				if (flag5)
				{
					rotTexSet = (flag4 ? TexBake.ResolveRot(text3, false, null, false, text2) : TexBake.ResolveRot(text3, false, null, false, null));
				}
				bool flag6 = flag4 && !rotTexSet.valid;
				if (flag6)
				{
					rotTexSet = TexBake.ResolveRot(text3, false, null, false, null);
				}
				bool flag7 = !rotTexSet.valid;
				if (flag7)
				{
					entry2 = entry;
				}
				else
				{
					entry.eastFlipped = rotTexSet.eastFlipped;
					entry.westFlipped = rotTexSet.westFlipped;
					entry.extraAngle = rotTexSet.extraAngle;
					entry.mirrored = rotTexSet.mirrored;
					entry.sourceSet = rotTexSet;
					entry.customAlienBody = flag;
					bool flag8 = flag && harBodyMode == HarBodyMode.AutoResize && settings.autoFit;
					string text5 = bodyNakedGraphicPath;
					if (female)
					{
						string text6 = FemaleBodyVariantsCompat.FemaleVariant(bodyNakedGraphicPath);
						bool flag9 = text6 != null;
						if (flag9)
						{
							text5 = text6;
						}
					}
					RotTexSet rotTexSet2 = (flag8 ? TexBake.ResolveRot(text5, false, null, false, null) : null);
					bool flag10 = flag8 && (rotTexSet2 == null || !rotTexSet2.valid);
					if (flag10)
					{
						flag8 = false;
					}
					bool flag11 = flag8 && rotTexSet.tex[2] != null && rotTexSet2.tex[2] != null;
					if (flag11)
					{
						Texture2D texture2D = TexBake.Readable(rotTexSet.tex[2]);
						Texture2D texture2D2 = TexBake.Readable(rotTexSet2.tex[2]);
						bool flag12 = texture2D != null && texture2D2 != null;
						if (flag12)
						{
							Rect rect = TexBake.AlphaBoundsUV(texture2D);
							Rect rect2 = TexBake.AlphaBoundsUV(texture2D2);
							bool flag13 = rect.width > 0.01f && rect.height > 0.01f;
							if (flag13)
							{
								entry.headScale = Mathf.Clamp((rect2.width / rect.width + rect2.height / rect.height) * 0.5f, 0.25f, 3f);
							}
						}
					}
					TailorRaceDef tailorRaceDef = TailorRaceRegistry.For(race);
					bool flag14 = tailorRaceDef != null && tailorRaceDef.headScaleFactor != 1f;
					if (flag14)
					{
						entry.headScale *= tailorRaceDef.headScaleFactor;
					}
					bool fitSideViews = settings.fitSideViews;
					for (int i = 0; i < 4; i++)
					{
						bool flag15 = rotTexSet.tex[i] == null;
						if (!flag15)
						{
							bool flag16 = flag8 && (fitSideViews || i == 0 || i == 2);
							bool flag17 = flag16;
							if (flag17)
							{
								Texture2D texture2D3 = TexBake.Readable(rotTexSet2.tex[i]);
								bool flag18 = rotTexSet.mirrored[i] != rotTexSet2.mirrored[i];
								string text7 = string.Concat(new string[]
								{
									"body:",
									rotTexSet.tex[i].GetInstanceID().ToString(),
									":",
									((texture2D3 != null) ? texture2D3.GetInstanceID() : 0).ToString(),
									":",
									flag18.ToString()
								});
								Texture2D[] tex = entry.tex;
								int num = i;
								string text8 = text7;
								Texture texture = rotTexSet.tex[i];
								Texture2D texture2D4 = texture2D3;
								bool flag19 = flag18;
								TailorPatternDef tailorPatternDef = null;
								Rot4 rot = new Rot4(i);
								bool flag20 = false;
								Texture2D texture2D5 = null;
								bool flag21 = false;
								bool? flag22 = new bool?(settings.bodyUnlockResolution);
								tex[num] = TexBake.BakeFitted(text8, texture, texture2D4, flag19, tailorPatternDef, rot, flag20, texture2D5, flag21, default(Vector2), 1f, 0f, flag22, default(Vector2));
							}
							else
							{
								entry.tex[i] = TexBake.Readable(rotTexSet.tex[i]);
							}
						}
					}
					entry.valid = entry.tex[2] != null;
					entry2 = entry;
				}
			}
			return entry2;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002BC0 File Offset: 0x00000DC0
		public static bool HasRetexture(ThingDef race, BodyTypeDef bodyType)
		{
			bool flag = bodyType == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				string text = BodyTexCache.BasePathFor(race, bodyType);
				bool flag3 = GenText.NullOrEmpty(text);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag5;
					bool flag4 = BodyTexCache.retexCache.TryGetValue(text, out flag5);
					if (flag4)
					{
						flag2 = flag5;
					}
					else
					{
						Texture2D texture2D = TexBake.Find(text + "_south", false);
						Texture2D texture2D2 = TexBake.Find(text + "_south", true);
						bool flag6 = texture2D != null && texture2D != texture2D2;
						BodyTexCache.retexCache[text] = flag6;
						flag2 = flag6;
					}
				}
			}
			return flag2;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002C5D File Offset: 0x00000E5D
		public static void Clear()
		{
			BodyTexCache.cache.Clear();
			BodyTexCache.retexCache.Clear();
		}

		// Token: 0x04000019 RID: 25
		private static readonly Dictionary<ValueTuple<ushort, ushort, bool>, BodyTexCache.Entry> cache = new Dictionary<ValueTuple<ushort, ushort, bool>, BodyTexCache.Entry>();

		// Token: 0x0400001A RID: 26
		private static readonly Dictionary<string, bool> retexCache = new Dictionary<string, bool>();

		// Token: 0x02000030 RID: 48
		public class Entry
		{
			// Token: 0x04000110 RID: 272
			public Texture2D[] tex = new Texture2D[4];

			// Token: 0x04000111 RID: 273
			public bool[] mirrored = new bool[4];

			// Token: 0x04000112 RID: 274
			public bool eastFlipped;

			// Token: 0x04000113 RID: 275
			public bool westFlipped;

			// Token: 0x04000114 RID: 276
			public float extraAngle;

			// Token: 0x04000115 RID: 277
			public bool valid;

			// Token: 0x04000116 RID: 278
			public RotTexSet sourceSet;

			// Token: 0x04000117 RID: 279
			public bool customAlienBody;

			// Token: 0x04000118 RID: 280
			public float headScale = 1f;
		}
	}
}
