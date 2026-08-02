using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace TailorMade
{
	// Token: 0x02000026 RID: 38
	public static class PatternRegistry
	{
		// Token: 0x060000CE RID: 206 RVA: 0x0000D670 File Offset: 0x0000B870
		private static void EnsureInit()
		{
			bool flag = PatternRegistry.defs != null;
			if (!flag)
			{
				PatternRegistry.defs = DefDatabase<TailorPatternDef>.AllDefsListForReading;
				PatternRegistry.HasBehaviorDefs = false;
				foreach (TailorPatternDef tailorPatternDef in PatternRegistry.defs)
				{
					tailorPatternDef.CompilePatterns();
					bool flag2 = tailorPatternDef.renderOrder != 0f || tailorPatternDef.hide || tailorPatternDef.keepHair;
					if (flag2)
					{
						PatternRegistry.HasBehaviorDefs = true;
					}
				}
			}
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000D710 File Offset: 0x0000B910
		public static ResolvedPattern Get(string key)
		{
			ResolvedPattern resolvedPattern;
			PatternRegistry.byKey.TryGetValue(key, out resolvedPattern);
			return resolvedPattern;
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000D734 File Offset: 0x0000B934
		public static ResolvedPattern Resolve(ThingDef race, BodyTypeDef bodyType, ApparelLayerDef layer, ThingDef apparelDef, bool femaleBody = false)
		{
			PatternRegistry.EnsureInit();
			ValueTuple<ushort, ushort, ushort, bool> valueTuple = new ValueTuple<ushort, ushort, ushort, bool>(race.shortHash, bodyType.index, apparelDef.shortHash, femaleBody);
			ResolvedPattern resolvedPattern;
			bool flag = PatternRegistry.apparelCache.TryGetValue(valueTuple, out resolvedPattern);
			ResolvedPattern resolvedPattern2;
			if (flag)
			{
				resolvedPattern2 = resolvedPattern;
			}
			else
			{
				ResolvedPattern resolvedPattern3 = PatternRegistry.ResolveInner(race, bodyType, layer, apparelDef, femaleBody);
				PatternRegistry.apparelCache[valueTuple] = resolvedPattern3;
				resolvedPattern2 = resolvedPattern3;
			}
			return resolvedPattern2;
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000D79C File Offset: 0x0000B99C
		private static ResolvedPattern ResolveInner(ThingDef race, BodyTypeDef bodyType, ApparelLayerDef layer, ThingDef apparelDef, bool femaleBody)
		{
			TailorPatternDef tailorPatternDef = null;
			for (int i = 0; i < PatternRegistry.defs.Count; i++)
			{
				TailorPatternDef tailorPatternDef2 = PatternRegistry.defs[i];
				bool flag = !GenText.NullOrEmpty(tailorPatternDef2.raceName) && tailorPatternDef2.raceName != "*" && tailorPatternDef2.raceName != race.defName;
				if (!flag)
				{
					bool flag2 = tailorPatternDef2.bodyType != null && tailorPatternDef2.bodyType != bodyType;
					if (!flag2)
					{
						bool flag3 = !GenText.NullOrEmpty(tailorPatternDef2.apparelLayer) && tailorPatternDef2.apparelLayer != layer.defName;
						if (!flag3)
						{
							bool flag4 = !tailorPatternDef2.MatchesApparel(apparelDef);
							if (!flag4)
							{
								tailorPatternDef = tailorPatternDef2;
							}
						}
					}
				}
			}
			bool flag5 = tailorPatternDef != null && tailorPatternDef.ignore;
			ResolvedPattern resolvedPattern;
			if (flag5)
			{
				resolvedPattern = null;
			}
			else
			{
				bool flag6 = tailorPatternDef == null || GenText.NullOrEmpty(tailorPatternDef.maskPath);
				if (flag6)
				{
					bool flag7 = GenText.NullOrEmpty(BodyTexCache.BasePathFor(race, bodyType));
					if (flag7)
					{
						return null;
					}
				}
				string text = string.Concat(new string[]
				{
					race.defName,
					"|",
					bodyType.defName,
					"|",
					layer.defName,
					"|",
					(tailorPatternDef != null) ? tailorPatternDef.defName : "auto",
					femaleBody ? "|f" : ""
				});
				ResolvedPattern resolvedPattern2;
				bool flag8 = !PatternRegistry.byKey.TryGetValue(text, out resolvedPattern2);
				if (flag8)
				{
					resolvedPattern2 = new ResolvedPattern
					{
						Key = text,
						Race = race,
						BodyType = bodyType,
						Layer = layer,
						Def = tailorPatternDef,
						FemaleBody = femaleBody
					};
					PatternRegistry.byKey[text] = resolvedPattern2;
				}
				resolvedPattern = resolvedPattern2;
			}
			return resolvedPattern;
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000D98C File Offset: 0x0000BB8C
		public static ResolvedPattern GetCanvasVariant(ResolvedPattern pattern, BodyTypeDef canvasBodyType)
		{
			bool flag = pattern == null || canvasBodyType == null || canvasBodyType == pattern.BodyType;
			ResolvedPattern resolvedPattern;
			if (flag)
			{
				resolvedPattern = pattern;
			}
			else
			{
				string text = pattern.Key + "|cv:" + canvasBodyType.defName;
				ResolvedPattern resolvedPattern2;
				bool flag2 = !PatternRegistry.byKey.TryGetValue(text, out resolvedPattern2);
				if (flag2)
				{
					resolvedPattern2 = new ResolvedPattern
					{
						Key = text,
						Race = pattern.Race,
						BodyType = pattern.BodyType,
						Layer = pattern.Layer,
						Def = pattern.Def,
						CanvasBodyType = canvasBodyType,
						FemaleBody = pattern.FemaleBody
					};
					PatternRegistry.byKey[text] = resolvedPattern2;
				}
				resolvedPattern = resolvedPattern2;
			}
			return resolvedPattern;
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000DA48 File Offset: 0x0000BC48
		[return: TupleElementNames(new string[] { "order", "hide", "keepHair" })]
		public static ValueTuple<float, bool, bool> ShippedBehavior(string apparelDefName)
		{
			PatternRegistry.EnsureInit();
			ValueTuple<float, bool, bool> valueTuple;
			bool flag = PatternRegistry.behaviorCache.TryGetValue(apparelDefName, out valueTuple);
			ValueTuple<float, bool, bool> valueTuple2;
			if (flag)
			{
				valueTuple2 = valueTuple;
			}
			else
			{
				float num = 0f;
				bool flag2 = false;
				bool flag3 = false;
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
				bool flag4 = namedSilentFail != null;
				if (flag4)
				{
					for (int i = 0; i < PatternRegistry.defs.Count; i++)
					{
						TailorPatternDef tailorPatternDef = PatternRegistry.defs[i];
						bool flag5 = tailorPatternDef.renderOrder == 0f && !tailorPatternDef.hide && !tailorPatternDef.keepHair;
						if (!flag5)
						{
							bool flag6 = !tailorPatternDef.MatchesApparel(namedSilentFail);
							if (!flag6)
							{
								bool flag7 = tailorPatternDef.renderOrder != 0f;
								if (flag7)
								{
									num = tailorPatternDef.renderOrder;
								}
								bool hide = tailorPatternDef.hide;
								if (hide)
								{
									flag2 = true;
								}
								bool keepHair = tailorPatternDef.keepHair;
								if (keepHair)
								{
									flag3 = true;
								}
							}
						}
					}
				}
				ValueTuple<float, bool, bool> valueTuple3 = new ValueTuple<float, bool, bool>(num, flag2, flag3);
				PatternRegistry.behaviorCache[apparelDefName] = valueTuple3;
				valueTuple2 = valueTuple3;
			}
			return valueTuple2;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000DB69 File Offset: 0x0000BD69
		public static void ClearApparelCache()
		{
			PatternRegistry.apparelCache.Clear();
			PatternRegistry.behaviorCache.Clear();
		}

		// Token: 0x040000C0 RID: 192
		private static List<TailorPatternDef> defs;

		// Token: 0x040000C1 RID: 193
		private static readonly Dictionary<ValueTuple<ushort, ushort, ushort, bool>, ResolvedPattern> apparelCache = new Dictionary<ValueTuple<ushort, ushort, ushort, bool>, ResolvedPattern>();

		// Token: 0x040000C2 RID: 194
		private static readonly Dictionary<string, ResolvedPattern> byKey = new Dictionary<string, ResolvedPattern>();

		// Token: 0x040000C3 RID: 195
		public static bool HasBehaviorDefs;

		// Token: 0x040000C4 RID: 196
		[TupleElementNames(new string[] { "order", "hide", "keepHair" })]
		private static readonly Dictionary<string, ValueTuple<float, bool, bool>> behaviorCache = new Dictionary<string, ValueTuple<float, bool, bool>>();
	}
}
