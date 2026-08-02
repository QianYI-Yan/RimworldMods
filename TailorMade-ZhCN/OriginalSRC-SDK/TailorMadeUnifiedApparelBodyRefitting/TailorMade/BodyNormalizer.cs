using System;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x0200001A RID: 26
	public static class BodyNormalizer
	{
		// Token: 0x06000099 RID: 153 RVA: 0x00009D10 File Offset: 0x00007F10
		public static bool Normalize(Pawn pawn)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			bool flag = settings == null || !settings.enabled || !settings.forceFemaleBody;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3;
				if (pawn == null)
				{
					flag3 = null != null;
				}
				else
				{
					Pawn_StoryTracker story = pawn.story;
					flag3 = ((story != null) ? story.bodyType : null) != null;
				}
				bool flag4;
				if (flag3)
				{
					ThingDef def = pawn.def;
					flag4 = ((def != null) ? def.race : null) == null;
				}
				else
				{
					flag4 = true;
				}
				bool flag5 = flag4;
				if (flag5)
				{
					flag2 = false;
				}
				else
				{
					bool flag6 = !pawn.def.race.Humanlike;
					if (flag6)
					{
						flag2 = false;
					}
					else
					{
						bool flag7 = pawn.gender != 2;
						if (flag7)
						{
							flag2 = false;
						}
						else
						{
							bool flag8 = !DevelopmentalStageExtensions.Adult(pawn.DevelopmentalStage);
							if (flag8)
							{
								flag2 = false;
							}
							else
							{
								bool flag9 = HarSupport.IsAlienRace(pawn.def);
								if (flag9)
								{
									flag2 = false;
								}
								else
								{
									bool flag10 = pawn.story.bodyType == BodyTypeDefOf.Female;
									if (flag10)
									{
										flag2 = false;
									}
									else
									{
										pawn.story.bodyType = BodyTypeDefOf.Female;
										flag2 = true;
									}
								}
							}
						}
					}
				}
			}
			return flag2;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00009E14 File Offset: 0x00008014
		public static void SweepAll()
		{
			bool flag = Current.ProgramState != 2 || Find.Maps == null;
			if (!flag)
			{
				foreach (Map map in Find.Maps)
				{
					foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
					{
						try
						{
							bool flag2 = BodyNormalizer.Normalize(pawn);
							if (flag2)
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
						catch
						{
						}
					}
				}
			}
		}
	}
}
