using System;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000008 RID: 8
	public class TailorAdjust : IExposable
	{
		// Token: 0x06000016 RID: 22 RVA: 0x00002C8C File Offset: 0x00000E8C
		public Vector2 GetOffset(Rot4 r)
		{
			return (r == Rot4.North) ? this.offN : ((r == Rot4.East) ? this.offE : ((r == Rot4.West) ? this.offW : this.offS));
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002CE0 File Offset: 0x00000EE0
		public float GetScale(Rot4 r)
		{
			return (r == Rot4.North) ? this.scaleN : ((r == Rot4.East) ? this.scaleE : ((r == Rot4.West) ? this.scaleW : this.scaleS));
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002D34 File Offset: 0x00000F34
		public float GetAngle(Rot4 r)
		{
			return (r == Rot4.North) ? this.angN : ((r == Rot4.East) ? this.angE : ((r == Rot4.West) ? this.angW : this.angS));
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002D88 File Offset: 0x00000F88
		public void SetOffset(Rot4 r, Vector2 v)
		{
			bool flag = r == Rot4.North;
			if (flag)
			{
				this.offN = v;
			}
			else
			{
				bool flag2 = r == Rot4.East;
				if (flag2)
				{
					this.offE = v;
				}
				else
				{
					bool flag3 = r == Rot4.West;
					if (flag3)
					{
						this.offW = v;
					}
					else
					{
						this.offS = v;
					}
				}
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002DE8 File Offset: 0x00000FE8
		public void SetScale(Rot4 r, float v)
		{
			bool flag = r == Rot4.North;
			if (flag)
			{
				this.scaleN = v;
			}
			else
			{
				bool flag2 = r == Rot4.East;
				if (flag2)
				{
					this.scaleE = v;
				}
				else
				{
					bool flag3 = r == Rot4.West;
					if (flag3)
					{
						this.scaleW = v;
					}
					else
					{
						this.scaleS = v;
					}
				}
			}
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002E48 File Offset: 0x00001048
		public void SetAngle(Rot4 r, float v)
		{
			bool flag = r == Rot4.North;
			if (flag)
			{
				this.angN = v;
			}
			else
			{
				bool flag2 = r == Rot4.East;
				if (flag2)
				{
					this.angE = v;
				}
				else
				{
					bool flag3 = r == Rot4.West;
					if (flag3)
					{
						this.angW = v;
					}
					else
					{
						this.angS = v;
					}
				}
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002EA8 File Offset: 0x000010A8
		public TailorAdjust Clone()
		{
			return new TailorAdjust
			{
				offN = this.offN,
				offE = this.offE,
				offS = this.offS,
				offW = this.offW,
				scaleN = this.scaleN,
				scaleE = this.scaleE,
				scaleS = this.scaleS,
				scaleW = this.scaleW,
				angN = this.angN,
				angE = this.angE,
				angS = this.angS,
				angW = this.angW,
				conform = this.conform,
				renderOrder = this.renderOrder
			};
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600001D RID: 29 RVA: 0x00002F64 File Offset: 0x00001164
		public bool IsDefault
		{
			get
			{
				return this.offN == Vector2.zero && this.offE == Vector2.zero && this.offS == Vector2.zero && this.offW == Vector2.zero && this.scaleN == 1f && this.scaleE == 1f && this.scaleS == 1f && this.scaleW == 1f && this.angN == 0f && this.angE == 0f && this.angS == 0f && this.angW == 0f && this.conform && this.renderOrder == 0f;
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00003044 File Offset: 0x00001244
		public void ExposeData()
		{
			Scribe_Values.Look<Vector2>(ref this.offN, "offN", default(Vector2), false);
			Scribe_Values.Look<Vector2>(ref this.offE, "offE", default(Vector2), false);
			Scribe_Values.Look<Vector2>(ref this.offS, "offS", default(Vector2), false);
			Scribe_Values.Look<Vector2>(ref this.offW, "offW", default(Vector2), false);
			Scribe_Values.Look<float>(ref this.scaleN, "scaleN", 1f, false);
			Scribe_Values.Look<float>(ref this.scaleE, "scaleE", 1f, false);
			Scribe_Values.Look<float>(ref this.scaleS, "scaleS", 1f, false);
			Scribe_Values.Look<float>(ref this.scaleW, "scaleW", 1f, false);
			Scribe_Values.Look<float>(ref this.angN, "angN", 0f, false);
			Scribe_Values.Look<float>(ref this.angE, "angE", 0f, false);
			Scribe_Values.Look<float>(ref this.angS, "angS", 0f, false);
			Scribe_Values.Look<float>(ref this.angW, "angW", 0f, false);
			Scribe_Values.Look<bool>(ref this.conform, "conform", true, false);
			Scribe_Values.Look<float>(ref this.renderOrder, "renderOrder", 0f, false);
		}

		// Token: 0x0400001B RID: 27
		public Vector2 offN;

		// Token: 0x0400001C RID: 28
		public Vector2 offE;

		// Token: 0x0400001D RID: 29
		public Vector2 offS;

		// Token: 0x0400001E RID: 30
		public Vector2 offW;

		// Token: 0x0400001F RID: 31
		public float scaleN = 1f;

		// Token: 0x04000020 RID: 32
		public float scaleE = 1f;

		// Token: 0x04000021 RID: 33
		public float scaleS = 1f;

		// Token: 0x04000022 RID: 34
		public float scaleW = 1f;

		// Token: 0x04000023 RID: 35
		public float angN;

		// Token: 0x04000024 RID: 36
		public float angE;

		// Token: 0x04000025 RID: 37
		public float angS;

		// Token: 0x04000026 RID: 38
		public float angW;

		// Token: 0x04000027 RID: 39
		public bool conform = true;

		// Token: 0x04000028 RID: 40
		public float renderOrder;
	}
}
