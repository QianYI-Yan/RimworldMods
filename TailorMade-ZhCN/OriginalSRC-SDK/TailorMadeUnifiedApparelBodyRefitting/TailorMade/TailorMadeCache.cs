using System;
using Verse;

namespace TailorMade
{
	// Token: 0x0200001D RID: 29
	public static class TailorMadeCache
	{
		// Token: 0x060000A0 RID: 160 RVA: 0x0000A7F8 File Offset: 0x000089F8
		public static void ClearAndRepaint()
		{
			BodyNormalizer.SweepAll();
			HarCompat.Invalidate();
			ApparelRefit.ClearMemo();
			ApparelClassifier.Clear();
			PatternRegistry.ClearApparelCache();
			BodyTexCache.Clear();
			FemaleBodyVariantsCompat.Clear();
			HarSupport.ClearCache();
			TexBake.Clear();
			foreach (Graphic_TailorMade graphic_TailorMade in Graphic_TailorMade.Live)
			{
				graphic_TailorMade.Repaint();
			}
			foreach (Graphic_TailorBody graphic_TailorBody in Graphic_TailorBody.Live)
			{
				graphic_TailorBody.Repaint();
			}
			foreach (Graphic_TailorHead graphic_TailorHead in Graphic_TailorHead.Live)
			{
				graphic_TailorHead.Repaint();
			}
			bool flag = Current.ProgramState == 2 && Find.Maps != null;
			if (flag)
			{
				foreach (Map map in Find.Maps)
				{
					foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
					{
						Pawn_DrawTracker drawer = pawn.Drawer;
						if (drawer != null)
						{
							PawnRenderer renderer = drawer.renderer;
							if (renderer != null)
							{
								renderer.SetAllGraphicsDirty();
							}
						}
					}
				}
			}
		}
	}
}
