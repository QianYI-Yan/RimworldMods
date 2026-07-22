using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000011 RID: 17
	public class Graphic_TailorBody : Graphic_Multi
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600006F RID: 111 RVA: 0x00007E4D File Offset: 0x0000604D
		public override Material MatSingle
		{
			get
			{
				return this.tmats[2];
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00007E57 File Offset: 0x00006057
		public override Material MatNorth
		{
			get
			{
				return this.tmats[0];
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000071 RID: 113 RVA: 0x00007E61 File Offset: 0x00006061
		public override Material MatEast
		{
			get
			{
				return this.tmats[1];
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00007E6B File Offset: 0x0000606B
		public override Material MatSouth
		{
			get
			{
				return this.tmats[2];
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000073 RID: 115 RVA: 0x00007E75 File Offset: 0x00006075
		public override Material MatWest
		{
			get
			{
				return this.tmats[3];
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00007E7F File Offset: 0x0000607F
		public override bool WestFlipped
		{
			get
			{
				return this.wFlip;
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000075 RID: 117 RVA: 0x00007E87 File Offset: 0x00006087
		public override bool EastFlipped
		{
			get
			{
				return this.eFlip;
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00007E8F File Offset: 0x0000608F
		public override float DrawRotatedExtraAngleOffset
		{
			get
			{
				return this.extraAngle;
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00007E98 File Offset: 0x00006098
		public override void Init(GraphicRequest req)
		{
			this.reqCache = req;
			string[] array = req.path.Split(new char[1]);
			this.path = array[0];
			this.bodyKey = ((array.Length > 1) ? array[1] : "");
			this.maskPath = req.maskPath;
			this.data = req.graphicData;
			this.color = req.color;
			this.colorTwo = req.colorTwo;
			this.drawSize = req.drawSize;
			ThingDef thingDef = null;
			BodyTypeDef bodyTypeDef = null;
			bool flag = false;
			string[] array2 = this.bodyKey.Split(new char[] { '|' });
			bool flag2 = array2.Length >= 2;
			if (flag2)
			{
				thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(array2[0]);
				bodyTypeDef = DefDatabase<BodyTypeDef>.GetNamedSilentFail(array2[1]);
				flag = array2.Length > 2 && array2[2] == "f";
			}
			BodyTexCache.Entry entry = ((thingDef != null && bodyTypeDef != null) ? BodyTexCache.Get(thingDef, bodyTypeDef, flag) : null);
			bool flag3 = entry == null || !entry.valid;
			if (flag3)
			{
				Log.ErrorOnce("[TailorMade] Failed to build body textures for " + this.bodyKey, this.bodyKey.GetHashCode());
				for (int i = 0; i < 4; i++)
				{
					this.tmats[i] = BaseContent.BadMat;
				}
			}
			else
			{
				this.eFlip = entry.eastFlipped;
				this.wFlip = entry.westFlipped;
				this.extraAngle = entry.extraAngle;
				for (int j = 0; j < 4; j++)
				{
					Texture2D texture2D = entry.tex[j] ?? entry.tex[2];
					bool flag4 = texture2D == null;
					if (flag4)
					{
						this.tmats[j] = BaseContent.BadMat;
					}
					else
					{
						Material[] array3 = this.tmats;
						int num = j;
						MaterialRequest materialRequest = default(MaterialRequest);
						materialRequest.mainTex = texture2D;
						materialRequest.shader = req.shader;
						materialRequest.color = this.color;
						materialRequest.colorTwo = this.colorTwo;
						materialRequest.shaderParameters = req.shaderParameters;
						materialRequest.renderQueue = req.renderQueue;
						array3[num] = MaterialPool.MatFrom(materialRequest);
					}
				}
				bool flag5 = !this.inited;
				if (flag5)
				{
					Graphic_TailorBody.Live.Add(this);
				}
				this.inited = true;
			}
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000080F0 File Offset: 0x000062F0
		public void Repaint()
		{
			bool flag = this.inited;
			if (flag)
			{
				this.Init(this.reqCache);
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00008118 File Offset: 0x00006318
		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_TailorBody>(this.path + "\0" + this.bodyKey, newShader, this.drawSize, newColor, newColorTwo, this.data, this.maskPath);
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000815A File Offset: 0x0000635A
		public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
		{
		}

		// Token: 0x04000068 RID: 104
		public static readonly List<Graphic_TailorBody> Live = new List<Graphic_TailorBody>();

		// Token: 0x04000069 RID: 105
		private readonly Material[] tmats = new Material[4];

		// Token: 0x0400006A RID: 106
		private bool wFlip;

		// Token: 0x0400006B RID: 107
		private bool eFlip;

		// Token: 0x0400006C RID: 108
		private float extraAngle;

		// Token: 0x0400006D RID: 109
		private string bodyKey = "";

		// Token: 0x0400006E RID: 110
		private GraphicRequest reqCache;

		// Token: 0x0400006F RID: 111
		private bool inited;
	}
}
