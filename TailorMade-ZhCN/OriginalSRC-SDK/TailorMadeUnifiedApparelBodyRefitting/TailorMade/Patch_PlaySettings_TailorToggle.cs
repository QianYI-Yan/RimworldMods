using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x0200000A RID: 10
	[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
	internal static class Patch_PlaySettings_TailorToggle
	{
		// Token: 0x06000021 RID: 33 RVA: 0x0000334C File Offset: 0x0000154C
		private static void Postfix(WidgetRow row, bool worldView)
		{
			bool flag = worldView || row == null;
			if (!flag)
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				bool flag2 = settings == null || !settings.enabled || TailorTex.Toggle == null;
				if (!flag2)
				{
					bool flag3 = row.ButtonIcon(TailorTex.Toggle, "TailorMade: adjust apparel masks per item", null, null, null, true, -1f);
					if (flag3)
					{
						Window_Tailor window_Tailor = Find.WindowStack.WindowOfType<Window_Tailor>();
						bool flag4 = window_Tailor != null;
						if (flag4)
						{
							window_Tailor.Close(true);
						}
						else
						{
							Find.WindowStack.Add(new Window_Tailor());
						}
					}
				}
			}
		}
	}
}
