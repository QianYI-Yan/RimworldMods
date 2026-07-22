using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x0200002C RID: 44
	public class TailorRaceDef : Def
	{
		// Token: 0x040000F7 RID: 247
		public string raceName;

		// Token: 0x040000F8 RID: 248
		public bool? engage;

		// Token: 0x040000F9 RID: 249
		public float headScaleFactor = 1f;

		// Token: 0x040000FA RID: 250
		public List<TailorRaceDef.BodyPathEntry> bodyPaths = new List<TailorRaceDef.BodyPathEntry>();

		// Token: 0x040000FB RID: 251
		public List<string> bodyPathTemplates = new List<string>();

		// Token: 0x02000039 RID: 57
		public class BodyPathEntry
		{
			// Token: 0x04000133 RID: 307
			public BodyTypeDef bodyType;

			// Token: 0x04000134 RID: 308
			public string path;
		}
	}
}
