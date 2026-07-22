using System;
using HarmonyLib;
using Verse;

namespace TailorMade
{
	// Token: 0x02000019 RID: 25
	[HarmonyPatch(typeof(PawnGenerator), "GenerateBodyType")]
	internal static class Patch_GenerateBodyType
	{
		// Token: 0x06000098 RID: 152 RVA: 0x00009CE0 File Offset: 0x00007EE0
		[HarmonyPostfix]
		private static void Postfix(Pawn pawn)
		{
			try
			{
				BodyNormalizer.Normalize(pawn);
			}
			catch
			{
			}
		}
	}
}
