using System;
using Verse;

namespace TailorMade
{
	// Token: 0x02000027 RID: 39
	public class PerPawnEntry : IExposable
	{
		// Token: 0x060000D6 RID: 214 RVA: 0x0000DBA2 File Offset: 0x0000BDA2
		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref this.pawnId, "pawnId", 0, false);
			Scribe_Values.Look<string>(ref this.defName, "def", null, false);
			Scribe_Deep.Look<TailorAdjust>(ref this.adj, "adj", Array.Empty<object>());
		}

		// Token: 0x040000C5 RID: 197
		public int pawnId;

		// Token: 0x040000C6 RID: 198
		public string defName;

		// Token: 0x040000C7 RID: 199
		public TailorAdjust adj;
	}
}
