using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace TailorMade
{
	// Token: 0x02000014 RID: 20
	[StaticConstructorOnStartup]
	public static class TailorMadePatcher
	{
		// Token: 0x06000083 RID: 131 RVA: 0x00008914 File Offset: 0x00006B14
		static TailorMadePatcher()
		{
			Harmony harmony = new Harmony("astryl.tailormade");
			harmony.PatchAll();
			TailorMadePatcher.PatchBodyGraphicNodes(harmony);
			HarSupport.TryPatchRestrictions(harmony);
			HarCompat.EnsureScanned();
			Log.Message("[TailorMade] Body silhouette provider: " + TexBake.DetectBodyProvider() + " (apparel is fit to whatever body texture mod shadows the vanilla body paths).");
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00008962 File Offset: 0x00006B62
		private static void PatchBodyGraphicNodes(Harmony harmony)
		{
			TailorMadePatcher.PatchNodeFamily(harmony, typeof(PawnRenderNode_Body), "BodyGraphicPostfix");
			TailorMadePatcher.PatchNodeFamily(harmony, typeof(PawnRenderNode_Head), "HeadGraphicPostfix");
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00008994 File Offset: 0x00006B94
		private static void PatchNodeFamily(Harmony harmony, Type baseType, string postfixName)
		{
			HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(TailorMadePatcher), postfixName, null)
			{
				priority = 0
			};
			List<Type> list = new List<Type> { baseType };
			list.AddRange(GenTypes.AllSubclassesNonAbstract(baseType));
			foreach (Type type in list)
			{
				MethodInfo methodInfo;
				try
				{
					methodInfo = AccessTools.DeclaredMethod(type, "GraphicFor", new Type[] { typeof(Pawn) }, null);
				}
				catch
				{
					continue;
				}
				bool flag = methodInfo == null;
				if (!flag)
				{
					try
					{
						harmony.Patch(methodInfo, null, harmonyMethod, null, null);
					}
					catch (Exception ex)
					{
						Log.Warning("[TailorMade] Could not patch " + type.FullName + ".GraphicFor: " + ex.Message);
						bool flag2 = TailorMadePatcher.RemoveBrokenForeignPatches(harmony, methodInfo);
						if (flag2)
						{
							try
							{
								harmony.Patch(methodInfo, null, harmonyMethod, null, null);
								Log.Message("[TailorMade] " + type.FullName + ".GraphicFor patched successfully after removing the broken foreign patch record.");
							}
							catch (Exception ex2)
							{
								Log.Warning("[TailorMade] Retry still failed for " + type.FullName + ".GraphicFor: " + ex2.Message);
							}
						}
					}
				}
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00008B20 File Offset: 0x00006D20
		private static bool RemoveBrokenForeignPatches(Harmony harmony, MethodBase original)
		{
			bool flag2;
			try
			{
				Patches patchInfo = Harmony.GetPatchInfo(original);
				bool flag = patchInfo == null;
				if (flag)
				{
					Log.Warning("[TailorMade]   no patches are registered on " + original.Name + " — the failure did not come from a foreign patch record.");
					flag2 = false;
				}
				else
				{
					List<KeyValuePair<string, Patch>> list = new List<KeyValuePair<string, Patch>>();
					bool flag3 = patchInfo.Prefixes != null;
					if (flag3)
					{
						foreach (Patch patch in patchInfo.Prefixes)
						{
							list.Add(new KeyValuePair<string, Patch>("prefix", patch));
						}
					}
					bool flag4 = patchInfo.Postfixes != null;
					if (flag4)
					{
						foreach (Patch patch2 in patchInfo.Postfixes)
						{
							list.Add(new KeyValuePair<string, Patch>("postfix", patch2));
						}
					}
					bool flag5 = patchInfo.Transpilers != null;
					if (flag5)
					{
						foreach (Patch patch3 in patchInfo.Transpilers)
						{
							list.Add(new KeyValuePair<string, Patch>("transpiler", patch3));
						}
					}
					bool flag6 = patchInfo.Finalizers != null;
					if (flag6)
					{
						foreach (Patch patch4 in patchInfo.Finalizers)
						{
							list.Add(new KeyValuePair<string, Patch>("finalizer", patch4));
						}
					}
					bool flag7 = false;
					StringBuilder stringBuilder = new StringBuilder("[TailorMade]   patches registered on ").Append((original.DeclaringType != null) ? original.DeclaringType.Name : "?").Append('.').Append(original.Name)
						.Append(':');
					foreach (KeyValuePair<string, Patch> keyValuePair in list)
					{
						MethodInfo patchMethod = keyValuePair.Value.PatchMethod;
						stringBuilder.Append("\n    ").Append(keyValuePair.Key).Append(" owner='")
							.Append(keyValuePair.Value.owner)
							.Append("' ")
							.Append((patchMethod.DeclaringType != null) ? patchMethod.DeclaringType.FullName : "?")
							.Append('.')
							.Append(patchMethod.Name)
							.Append('(');
						ParameterInfo[] parameters = patchMethod.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
						{
							bool flag8 = i > 0;
							if (flag8)
							{
								stringBuilder.Append(", ");
							}
							stringBuilder.Append(parameters[i].ParameterType).Append(' ').Append(parameters[i].Name);
						}
						stringBuilder.Append(')');
						string text;
						bool flag9 = TailorMadePatcher.BrokenResultParam(patchMethod, original, out text);
						if (flag9)
						{
							stringBuilder.Append("  <-- BROKEN: ").Append(text);
							try
							{
								harmony.Unpatch(original, patchMethod);
								flag7 = true;
								stringBuilder.Append(" [removed]");
							}
							catch (Exception ex)
							{
								stringBuilder.Append(" [remove failed: ").Append(ex.Message).Append(']');
							}
						}
					}
					Log.Warning(stringBuilder.ToString());
					flag2 = flag7;
				}
			}
			catch (Exception ex2)
			{
				Log.Warning("[TailorMade]   could not inspect the patches on " + original.Name + ": " + ex2.Message);
				flag2 = false;
			}
			return flag2;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00008F80 File Offset: 0x00007180
		private static bool BrokenResultParam(MethodInfo patchMethod, MethodBase original, out string why)
		{
			why = null;
			MethodInfo methodInfo = original as MethodInfo;
			Type type = ((methodInfo != null) ? methodInfo.ReturnType : typeof(void));
			foreach (ParameterInfo parameterInfo in patchMethod.GetParameters())
			{
				bool flag = parameterInfo.Name != "__result";
				if (!flag)
				{
					Type type2 = parameterInfo.ParameterType;
					bool isByRef = type2.IsByRef;
					if (isByRef)
					{
						type2 = type2.GetElementType();
					}
					bool flag2 = type2 == null;
					if (!flag2)
					{
						bool flag3 = type == typeof(void);
						bool flag4;
						if (flag3)
						{
							why = "declares __result but the method returns void";
							flag4 = true;
						}
						else
						{
							bool flag5 = !type2.IsAssignableFrom(type);
							if (!flag5)
							{
								goto IL_00DA;
							}
							why = "__result is " + type2.FullName + " but the method returns " + type.FullName;
							flag4 = true;
						}
						return flag4;
					}
				}
				IL_00DA:;
			}
			return false;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x0000907C File Offset: 0x0000727C
		public static void BodyGraphicPostfix(Pawn pawn, ref Graphic __result)
		{
			try
			{
				BodyRefit.TrySwap(pawn, ref __result);
			}
			catch (Exception ex)
			{
				string text = "[TailorMade] Body refit failed for ";
				string text2;
				if (pawn == null)
				{
					text2 = null;
				}
				else
				{
					ThingDef def = pawn.def;
					text2 = ((def != null) ? def.defName : null);
				}
				string text3 = text2 ?? "null";
				string text4 = ": ";
				Exception ex2 = ex;
				Log.ErrorOnce(text + text3 + text4 + ((ex2 != null) ? ex2.ToString() : null), 2048045271);
			}
		}

		// Token: 0x06000089 RID: 137 RVA: 0x000090F4 File Offset: 0x000072F4
		public static void HeadGraphicPostfix(Pawn pawn, ref Graphic __result)
		{
			try
			{
				BodyRefit.TrySwapHead(pawn, ref __result);
			}
			catch (Exception ex)
			{
				string text = "[TailorMade] Head refit failed for ";
				string text2;
				if (pawn == null)
				{
					text2 = null;
				}
				else
				{
					ThingDef def = pawn.def;
					text2 = ((def != null) ? def.defName : null);
				}
				string text3 = text2 ?? "null";
				string text4 = ": ";
				Exception ex2 = ex;
				Log.ErrorOnce(text + text3 + text4 + ((ex2 != null) ? ex2.ToString() : null), 2048045272);
			}
		}
	}
}
