using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200000F RID: 15
	public class Graphic_TailorMade : Graphic_Multi
	{
		// Token: 0x06000051 RID: 81 RVA: 0x00006CAE File Offset: 0x00004EAE
		public bool IsForDef(string d)
		{
			return this.apparelDefName == d;
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00006CBC File Offset: 0x00004EBC
		public override Material MatSingle
		{
			get
			{
				return this.tmats[2];
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00006CC6 File Offset: 0x00004EC6
		public override Material MatNorth
		{
			get
			{
				return this.tmats[0];
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000054 RID: 84 RVA: 0x00006CD0 File Offset: 0x00004ED0
		public override Material MatEast
		{
			get
			{
				return this.tmats[1];
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00006CDA File Offset: 0x00004EDA
		public override Material MatSouth
		{
			get
			{
				return this.tmats[2];
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000056 RID: 86 RVA: 0x00006CE4 File Offset: 0x00004EE4
		public override Material MatWest
		{
			get
			{
				return this.tmats[3];
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000057 RID: 87 RVA: 0x00006CEE File Offset: 0x00004EEE
		public override bool WestFlipped
		{
			get
			{
				return this.wFlip;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000058 RID: 88 RVA: 0x00006CF6 File Offset: 0x00004EF6
		public override bool EastFlipped
		{
			get
			{
				return this.eFlip;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00006CFE File Offset: 0x00004EFE
		public override float DrawRotatedExtraAngleOffset
		{
			get
			{
				return this.extraAngle;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00006D08 File Offset: 0x00004F08
		public override bool ShouldDrawRotated
		{
			get
			{
				bool flag = this.data != null && !this.data.drawRotated;
				return !flag && (this.MatEast == this.MatNorth || this.MatWest == this.MatNorth);
			}
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00006D64 File Offset: 0x00004F64
		public override void Init(GraphicRequest req)
		{
			this.reqCache = req;
			string[] array = req.path.Split(new char[1]);
			this.path = array[0];
			this.patternKey = ((array.Length > 1) ? array[1] : "");
			this.apparelDefName = ((array.Length > 2) ? array[2] : "");
			this.pawnKey = ((array.Length > 3) ? array[3] : "");
			this.maskPath = req.maskPath;
			this.data = req.graphicData;
			this.color = req.color;
			this.colorTwo = req.colorTwo;
			this.drawSize = req.drawSize;
			ApparelClass apparelClass = ApparelClass.None;
			ThingDef thingDef = (GenText.NullOrEmpty(this.apparelDefName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(this.apparelDefName));
			bool flag = thingDef != null;
			if (flag)
			{
				apparelClass = ApparelClassifier.Info(thingDef).cls;
			}
			ResolvedPattern resolvedPattern = PatternRegistry.Get(this.patternKey);
			TailorAdjust tailorAdjust = null;
			int num;
			bool flag2 = !GenText.NullOrEmpty(this.pawnKey) && int.TryParse(this.pawnKey, out num);
			if (flag2)
			{
				tailorAdjust = PerPawnAdjust.GetById(num, this.apparelDefName);
			}
			bool flag3 = tailorAdjust == null;
			if (flag3)
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				tailorAdjust = ((settings != null) ? settings.GetAdjust(this.apparelDefName) : null);
			}
			bool flag6;
			if (tailorAdjust == null)
			{
				bool? flag4;
				if (resolvedPattern == null)
				{
					flag4 = null;
				}
				else
				{
					TailorPatternDef def = resolvedPattern.Def;
					flag4 = ((def != null) ? new bool?(def.conform) : null);
				}
				bool? flag5 = flag4;
				flag6 = flag5.GetValueOrDefault(true);
			}
			else
			{
				flag6 = tailorAdjust.conform;
			}
			bool flag7 = flag6;
			RotTexSet rotTexSet = TexBake.ResolveRot(this.path, false, null, false, null);
			bool flag8 = !rotTexSet.valid;
			if (flag8)
			{
				Log.ErrorOnce("[TailorMade] Failed to find textures at " + this.path, this.path.GetHashCode());
				for (int i = 0; i < 4; i++)
				{
					this.tmats[i] = BaseContent.BadMat;
				}
			}
			else
			{
				this.eFlip = rotTexSet.eastFlipped;
				this.wFlip = rotTexSet.westFlipped;
				this.extraAngle = rotTexSet.extraAngle;
				bool flag9 = apparelClass == ApparelClass.Chest;
				if (flag9)
				{
					bool flag11;
					bool flag10 = !ApparelClassifier.ChestArtCached(thingDef, out flag11);
					if (flag10)
					{
						Texture2D texture2D = ((rotTexSet.tex[2] != null) ? TexBake.Readable(rotTexSet.tex[2]) : null);
						flag11 = ApparelClassifier.ConfirmChestArt(thingDef, texture2D);
					}
					bool flag12 = !flag11;
					if (flag12)
					{
						apparelClass = ApparelClass.None;
					}
				}
				bool flag13 = ApparelClassifier.IsBanded(apparelClass);
				Vector2 vector = ApparelClassifier.BandFor(apparelClass);
				bool flag14 = rotTexSet.tex[0] == rotTexSet.tex[1] && rotTexSet.tex[1] == rotTexSet.tex[2] && rotTexSet.tex[2] == rotTexSet.tex[3];
				RotTexSet rotTexSet2 = ((req.shader != null && ShaderUtility.SupportsMaskTex(req.shader)) ? TexBake.ResolveRot(this.path, true, this.maskPath, false, null) : null);
				RotTexSet rotTexSet3 = null;
				BodyTexCache.Entry entry = null;
				bool flag15 = resolvedPattern != null && flag7;
				if (flag15)
				{
					bool flag16 = resolvedPattern.Def != null && !GenText.NullOrEmpty(resolvedPattern.Def.maskPath);
					if (flag16)
					{
						rotTexSet3 = TexBake.ResolveRot(resolvedPattern.Def.maskPath, false, null, false, null);
					}
					else
					{
						entry = BodyTexCache.Get(resolvedPattern.Race, resolvedPattern.BodyType, resolvedPattern.FemaleBody);
					}
				}
				RotTexSet rotTexSet4 = null;
				BodyTypeDef bodyTypeDef = ((resolvedPattern != null) ? resolvedPattern.CanvasBodyType : null) ?? ((resolvedPattern != null) ? resolvedPattern.BodyType : null);
				bool flag17 = resolvedPattern != null && flag7 && rotTexSet3 == null;
				if (flag17)
				{
					bool flag18 = resolvedPattern.CanvasBodyType == null && entry != null && entry.valid && entry.customAlienBody && entry.sourceSet != null && entry.sourceSet.valid;
					if (flag18)
					{
						rotTexSet4 = entry.sourceSet;
					}
					else
					{
						bool flag19 = bodyTypeDef != null && !GenText.NullOrEmpty(bodyTypeDef.bodyNakedGraphicPath);
						if (flag19)
						{
							rotTexSet4 = TexBake.ResolveRot(bodyTypeDef.bodyNakedGraphicPath, false, null, true, null);
							bool flag20 = !rotTexSet4.valid;
							if (flag20)
							{
								rotTexSet4 = TexBake.ResolveRot(bodyTypeDef.bodyNakedGraphicPath, false, null, false, null);
							}
							bool flag21 = !rotTexSet4.valid;
							if (flag21)
							{
								rotTexSet4 = null;
							}
						}
					}
				}
				bool flag22 = flag13;
				if (flag22)
				{
					rotTexSet4 = null;
				}
				for (int j = 0; j < 4; j++)
				{
					Texture2D texture2D2 = rotTexSet.tex[j];
					bool flag23 = texture2D2 == null;
					if (flag23)
					{
						this.tmats[j] = BaseContent.BadMat;
					}
					else
					{
						bool flag24 = (j == 1 || j == 3) && (TailorMadeMod.Settings == null || !TailorMadeMod.Settings.fitSideViews);
						bool flag25 = flag7 && !flag24;
						bool flag26 = flag25 && (TailorMadeMod.Settings == null || TailorMadeMod.Settings.ShouldClipBody((resolvedPattern != null) ? resolvedPattern.Race : null, (resolvedPattern != null) ? resolvedPattern.BodyType : null));
						bool flag27 = tailorAdjust != null;
						Vector2 vector2;
						float num2;
						float num3;
						if (flag27)
						{
							vector2 = tailorAdjust.GetOffset(new Rot4(j));
							num2 = tailorAdjust.GetScale(new Rot4(j));
							num3 = tailorAdjust.GetAngle(new Rot4(j));
						}
						else
						{
							bool flag28 = ((resolvedPattern != null) ? resolvedPattern.Def : null) != null;
							if (flag28)
							{
								vector2 = resolvedPattern.Def.GetAdjustOffset(new Rot4(j));
								num2 = resolvedPattern.Def.GetAdjustScale(new Rot4(j));
								num3 = resolvedPattern.Def.GetAdjustAngle(new Rot4(j));
							}
							else
							{
								vector2 = Vector2.zero;
								num2 = 1f;
								num3 = 0f;
							}
						}
						bool flag29 = rotTexSet.mirrored[j];
						if (flag29)
						{
							vector2.x = -vector2.x;
							num3 = -num3;
						}
						bool flag30 = vector2.x != 0f || vector2.y != 0f || num2 != 1f || num3 != 0f;
						bool flag31 = flag14 || (!flag25 && !flag30);
						if (flag31)
						{
							Material[] array2 = this.tmats;
							int num4 = j;
							MaterialRequest materialRequest = default(MaterialRequest);
							materialRequest.mainTex = texture2D2;
							materialRequest.shader = req.shader;
							materialRequest.color = this.color;
							materialRequest.colorTwo = this.colorTwo;
							materialRequest.maskTex = ((rotTexSet2 != null && rotTexSet2.valid) ? rotTexSet2.tex[j] : null);
							materialRequest.shaderParameters = req.shaderParameters;
							materialRequest.renderQueue = req.renderQueue;
							array2[num4] = MaterialPool.MatFrom(materialRequest);
						}
						else
						{
							Texture2D texture2D3 = null;
							bool flag32 = false;
							bool flag33 = flag25 && rotTexSet3 != null && rotTexSet3.valid && rotTexSet3.tex[j] != null;
							if (flag33)
							{
								texture2D3 = TexBake.Readable(rotTexSet3.tex[j]);
								flag32 = rotTexSet3.mirrored[j];
							}
							else
							{
								bool flag34 = flag25 && entry != null && entry.valid && entry.tex[j] != null;
								if (flag34)
								{
									texture2D3 = entry.tex[j];
									flag32 = entry.mirrored[j];
								}
							}
							bool flag35 = texture2D3 != null && rotTexSet.mirrored[j] != flag32;
							Texture2D texture2D4 = null;
							bool flag36 = false;
							int num5 = 0;
							bool flag37 = flag25 && rotTexSet4 != null && rotTexSet4.tex[j] != null && texture2D3 != null;
							if (flag37)
							{
								texture2D4 = TexBake.Readable(rotTexSet4.tex[j]);
								bool flag38 = texture2D4 != null;
								if (flag38)
								{
									flag36 = rotTexSet4.mirrored[j] != rotTexSet.mirrored[j];
									num5 = texture2D4.GetInstanceID();
								}
							}
							string text = ((((resolvedPattern != null) ? resolvedPattern.Def : null) != null && !resolvedPattern.Def.autoFit) ? (resolvedPattern.Def.defName + ":" + j.ToString()) : "auto");
							string text2 = (flag30 ? string.Concat(new string[]
							{
								":adj",
								vector2.x.ToString("f3"),
								",",
								vector2.y.ToString("f3"),
								",",
								num2.ToString("f3"),
								",",
								num3.ToString("f1")
							}) : "");
							bool flag39 = !flag25;
							if (flag39)
							{
								text2 += ":nc";
							}
							else
							{
								bool flag40 = !flag26;
								if (flag40)
								{
									text2 += ":fc";
								}
							}
							string text3 = ((j == 0 || j == 2) ? "ns" : "ew");
							string text4 = (flag13 ? (":band" + apparelClass.ToString() + vector.y.ToString("f2")) : "");
							string text5 = string.Concat(new string[]
							{
								"app:",
								texture2D2.GetInstanceID().ToString(),
								":",
								((texture2D3 != null) ? texture2D3.GetInstanceID() : 0).ToString(),
								":",
								num5.ToString(),
								":",
								flag35.ToString(),
								":",
								flag36.ToString(),
								":",
								text3,
								":",
								text,
								text2,
								text4
							});
							string text6 = text5;
							Texture texture = texture2D2;
							Texture2D texture2D5 = texture2D3;
							bool flag41 = flag35;
							TailorPatternDef tailorPatternDef = ((resolvedPattern != null) ? resolvedPattern.Def : null);
							Rot4 rot = new Rot4(j);
							bool flag42 = flag26;
							Texture2D texture2D6 = texture2D4;
							bool flag43 = flag36;
							Vector2 vector3 = vector2;
							float num6 = num2;
							float num7 = num3;
							Vector2 vector4 = vector;
							Texture2D texture2D7 = TexBake.BakeFitted(text6, texture, texture2D5, flag41, tailorPatternDef, rot, flag42, texture2D6, flag43, vector3, num6, num7, null, vector4) ?? texture2D2;
							Texture2D texture2D8 = null;
							bool flag44 = rotTexSet2 != null && rotTexSet2.valid && rotTexSet2.tex[j] != null;
							if (flag44)
							{
								Texture2D texture2D9 = rotTexSet2.tex[j];
								string text7 = string.Concat(new string[]
								{
									"appm:",
									texture2D9.GetInstanceID().ToString(),
									":",
									((texture2D3 != null) ? texture2D3.GetInstanceID() : 0).ToString(),
									":",
									num5.ToString(),
									":",
									flag35.ToString(),
									":",
									flag36.ToString(),
									":",
									text3,
									":",
									text,
									text2,
									text4
								});
								string text8 = text7;
								Texture texture2 = texture2D9;
								Texture2D texture2D10 = texture2D3;
								bool flag45 = flag35;
								TailorPatternDef tailorPatternDef2 = ((resolvedPattern != null) ? resolvedPattern.Def : null);
								Rot4 rot2 = new Rot4(j);
								bool flag46 = false;
								Texture2D texture2D11 = texture2D4;
								bool flag47 = flag36;
								Vector2 vector5 = vector2;
								float num8 = num2;
								float num9 = num3;
								vector4 = vector;
								texture2D8 = TexBake.BakeFitted(text8, texture2, texture2D10, flag45, tailorPatternDef2, rot2, flag46, texture2D11, flag47, vector5, num8, num9, null, vector4) ?? texture2D9;
							}
							Material[] array3 = this.tmats;
							int num10 = j;
							MaterialRequest materialRequest = default(MaterialRequest);
							materialRequest.mainTex = texture2D7;
							materialRequest.shader = req.shader;
							materialRequest.color = this.color;
							materialRequest.colorTwo = this.colorTwo;
							materialRequest.maskTex = texture2D8;
							materialRequest.shaderParameters = req.shaderParameters;
							materialRequest.renderQueue = req.renderQueue;
							array3[num10] = MaterialPool.MatFrom(materialRequest);
						}
					}
				}
				bool flag48 = !this.inited;
				if (flag48)
				{
					Graphic_TailorMade.Live.Add(this);
				}
				bool flag49 = !GenText.NullOrEmpty(this.apparelDefName) && !flag14;
				if (flag49)
				{
					Graphic_TailorMade.LiveDefNames.Add(this.apparelDefName);
				}
				this.inited = true;
			}
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00007988 File Offset: 0x00005B88
		public void Repaint()
		{
			bool flag = this.inited;
			if (flag)
			{
				this.Init(this.reqCache);
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000079B0 File Offset: 0x00005BB0
		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_TailorMade>(string.Concat(new string[] { this.path, "\0", this.patternKey, "\0", this.apparelDefName, "\0", this.pawnKey }), newShader, this.drawSize, newColor, newColorTwo, this.data, this.maskPath);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00007A23 File Offset: 0x00005C23
		public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
		{
		}

		// Token: 0x04000055 RID: 85
		public static readonly List<Graphic_TailorMade> Live = new List<Graphic_TailorMade>();

		// Token: 0x04000056 RID: 86
		public static readonly HashSet<string> LiveDefNames = new HashSet<string>();

		// Token: 0x04000057 RID: 87
		private readonly Material[] tmats = new Material[4];

		// Token: 0x04000058 RID: 88
		private bool wFlip;

		// Token: 0x04000059 RID: 89
		private bool eFlip;

		// Token: 0x0400005A RID: 90
		private float extraAngle;

		// Token: 0x0400005B RID: 91
		private string patternKey = "";

		// Token: 0x0400005C RID: 92
		private string apparelDefName = "";

		// Token: 0x0400005D RID: 93
		private string pawnKey = "";

		// Token: 0x0400005E RID: 94
		private GraphicRequest reqCache;

		// Token: 0x0400005F RID: 95
		private bool inited;
	}
}
