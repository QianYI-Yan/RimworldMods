using System;
using Verse;

namespace TailorMade
{
	// Token: 0x0200001B RID: 27
	public class TailorMadeGameComponent : GameComponent
	{
		// Token: 0x0600009B RID: 155 RVA: 0x00009F00 File Offset: 0x00008100
		public TailorMadeGameComponent(Game game)
		{
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00009F15 File Offset: 0x00008115
		public override void FinalizeInit()
		{
			BodyNormalizer.SweepAll();
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00009F20 File Offset: 0x00008120
		public override void ExposeData()
		{
			Scribe_Deep.Look<PerPawnData>(ref this.perPawn, "tailorPerPawn", Array.Empty<object>());
			bool flag = this.perPawn == null;
			if (flag)
			{
				this.perPawn = new PerPawnData();
			}
		}

		// Token: 0x04000072 RID: 114
		public PerPawnData perPawn = new PerPawnData();
	}
}
