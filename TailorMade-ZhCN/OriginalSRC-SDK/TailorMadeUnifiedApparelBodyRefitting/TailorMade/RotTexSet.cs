using System;
using UnityEngine;

namespace TailorMade
{
	// Token: 0x0200002E RID: 46
	public class RotTexSet
	{
		// Token: 0x040000FD RID: 253
		public Texture2D[] tex = new Texture2D[4];

		// Token: 0x040000FE RID: 254
		public bool[] mirrored = new bool[4];

		// Token: 0x040000FF RID: 255
		public bool eastFlipped;

		// Token: 0x04000100 RID: 256
		public bool westFlipped;

		// Token: 0x04000101 RID: 257
		public float extraAngle;

		// Token: 0x04000102 RID: 258
		public bool valid;
	}
}
