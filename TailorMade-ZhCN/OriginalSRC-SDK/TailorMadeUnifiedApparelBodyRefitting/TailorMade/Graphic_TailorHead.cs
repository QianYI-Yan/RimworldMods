using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000010 RID: 16
	public class Graphic_TailorHead : Graphic_Multi
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00007A72 File Offset: 0x00005C72
		public override Material MatSingle
		{
			get
			{
				return this.tmats[2];
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00007A7C File Offset: 0x00005C7C
		public override Material MatNorth
		{
			get
			{
				return this.tmats[0];
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00007A86 File Offset: 0x00005C86
		public override Material MatEast
		{
			get
			{
				return this.tmats[1];
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00007A90 File Offset: 0x00005C90
		public override Material MatSouth
		{
			get
			{
				return this.tmats[2];
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00007A9A File Offset: 0x00005C9A
		public override Material MatWest
		{
			get
			{
				return this.tmats[3];
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000066 RID: 102 RVA: 0x00007AA4 File Offset: 0x00005CA4
		public override bool WestFlipped
		{
			get
			{
				return this.wFlip;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00007AAC File Offset: 0x00005CAC
		public override bool EastFlipped
		{
			get
			{
				return this.eFlip;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00007AB4 File Offset: 0x00005CB4
		public override float DrawRotatedExtraAngleOffset
		{
			get
			{
				return this.extraAngle;
			}
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00007ABC File Offset: 0x00005CBC
		public override void Init(GraphicRequest req)
		{
			this.reqCache = req;
			string[] array = req.path.Split(new char[1]);
			this.path = array[0];
			this.headKey = ((array.Length > 1) ? array[1] : "");
			this.maskPath = req.maskPath;
			this.data = req.graphicData;
			this.color = req.color;
			this.colorTwo = req.colorTwo;
			this.drawSize = req.drawSize;
			float num = 1f;
			string[] array2 = this.headKey.Split(new char[] { '|' });
			bool flag = array2.Length >= 2;
			if (flag)
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(array2[0]);
				BodyTypeDef namedSilentFail2 = DefDatabase<BodyTypeDef>.GetNamedSilentFail(array2[1]);
				bool flag2 = array2.Length > 2 && array2[2] == "f";
				bool flag3 = namedSilentFail != null && namedSilentFail2 != null;
				if (flag3)
				{
					BodyTexCache.Entry entry = BodyTexCache.Get(namedSilentFail, namedSilentFail2, flag2);
					bool flag4 = entry != null && entry.valid;
					if (flag4)
					{
						num = entry.headScale;
					}
				}
			}
			RotTexSet rotTexSet = TexBake.ResolveRot(this.path, false, null, false, null);
			bool flag5 = !rotTexSet.valid;
			if (flag5)
			{
				Log.ErrorOnce("[TailorMade] Failed to find head textures at " + this.path, this.path.GetHashCode());
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
				for (int j = 0; j < 4; j++)
				{
					Texture2D texture2D = rotTexSet.tex[j];
					bool flag6 = texture2D == null;
					if (flag6)
					{
						this.tmats[j] = BaseContent.BadMat;
					}
					else
					{
						Texture2D texture2D2 = (((j == 1 || j == 3) && (TailorMadeMod.Settings == null || !TailorMadeMod.Settings.fitSideViews)) ? texture2D : (TexBake.BakeScaled("head:" + texture2D.GetInstanceID().ToString() + ":" + num.ToString("f3"), texture2D, num, new bool?(TailorMadeMod.Settings != null && TailorMadeMod.Settings.bodyUnlockResolution)) ?? texture2D));
						Material[] array3 = this.tmats;
						int num2 = j;
						MaterialRequest materialRequest = default(MaterialRequest);
						materialRequest.mainTex = texture2D2;
						materialRequest.shader = req.shader;
						materialRequest.color = this.color;
						materialRequest.colorTwo = this.colorTwo;
						materialRequest.shaderParameters = req.shaderParameters;
						materialRequest.renderQueue = req.renderQueue;
						array3[num2] = MaterialPool.MatFrom(materialRequest);
					}
				}
				bool flag7 = !this.inited;
				if (flag7)
				{
					Graphic_TailorHead.Live.Add(this);
				}
				this.inited = true;
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00007DB4 File Offset: 0x00005FB4
		public void Repaint()
		{
			bool flag = this.inited;
			if (flag)
			{
				this.Init(this.reqCache);
			}
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00007DDC File Offset: 0x00005FDC
		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_TailorHead>(this.path + "\0" + this.headKey, newShader, this.drawSize, newColor, newColorTwo, this.data, this.maskPath);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00007E1E File Offset: 0x0000601E
		public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
		{
		}

		// Token: 0x04000060 RID: 96
		public static readonly List<Graphic_TailorHead> Live = new List<Graphic_TailorHead>();

		// Token: 0x04000061 RID: 97
		private readonly Material[] tmats = new Material[4];

		// Token: 0x04000062 RID: 98
		private bool wFlip;

		// Token: 0x04000063 RID: 99
		private bool eFlip;

		// Token: 0x04000064 RID: 100
		private float extraAngle;

		// Token: 0x04000065 RID: 101
		private string headKey = "";

		// Token: 0x04000066 RID: 102
		private GraphicRequest reqCache;

		// Token: 0x04000067 RID: 103
		private bool inited;
	}
}
