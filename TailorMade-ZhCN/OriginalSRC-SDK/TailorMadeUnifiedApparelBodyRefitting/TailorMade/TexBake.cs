using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200002F RID: 47
	public static class TexBake
	{
		// Token: 0x060000FE RID: 254 RVA: 0x0000EA24 File Offset: 0x0000CC24
		private static Color[] BakeBuf(int count)
		{
			bool flag = TexBake._bakeBuf == null || TexBake._bakeBuf.Length != count;
			if (flag)
			{
				TexBake._bakeBuf = new Color[count];
			}
			return TexBake._bakeBuf;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000EA64 File Offset: 0x0000CC64
		public static string DetectBodyProvider()
		{
			bool flag = TexBake.bodyProviderName != null;
			string text;
			if (flag)
			{
				text = TexBake.bodyProviderName;
			}
			else
			{
				List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
				for (int i = runningModsListForReading.Count - 1; i >= 0; i--)
				{
					bool isOfficialMod = runningModsListForReading[i].IsOfficialMod;
					if (!isOfficialMod)
					{
						bool flag2 = runningModsListForReading[i].GetContentHolder<Texture2D>().Get("Things/Pawn/Humanlike/Bodies/Naked_Female_south") != null;
						if (flag2)
						{
							return TexBake.bodyProviderName = runningModsListForReading[i].Name;
						}
					}
				}
				text = (TexBake.bodyProviderName = "vanilla (Core)");
			}
			return text;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000EB04 File Offset: 0x0000CD04
		public static List<ModContentPack> BodyProviders()
		{
			bool flag = TexBake.bodyProviders != null;
			List<ModContentPack> list;
			if (flag)
			{
				list = TexBake.bodyProviders;
			}
			else
			{
				TexBake.bodyProviders = new List<ModContentPack>();
				List<string> list2 = new List<string>();
				foreach (BodyTypeDef bodyTypeDef in DefDatabase<BodyTypeDef>.AllDefsListForReading)
				{
					bool flag2 = GenText.NullOrEmpty(bodyTypeDef.bodyNakedGraphicPath);
					if (!flag2)
					{
						bool flag3 = bodyTypeDef.modContentPack != null && !bodyTypeDef.modContentPack.IsOfficialMod;
						if (!flag3)
						{
							list2.Add(bodyTypeDef.bodyNakedGraphicPath + "_south");
						}
					}
				}
				foreach (ModContentPack modContentPack in LoadedModManager.RunningModsListForReading)
				{
					bool isOfficialMod = modContentPack.IsOfficialMod;
					if (!isOfficialMod)
					{
						ModContentHolder<Texture2D> contentHolder = modContentPack.GetContentHolder<Texture2D>();
						for (int i = 0; i < list2.Count; i++)
						{
							bool flag4 = contentHolder.Get(list2[i]) == null;
							if (!flag4)
							{
								TexBake.bodyProviders.Add(modContentPack);
								break;
							}
						}
					}
				}
				list = TexBake.bodyProviders;
			}
			return list;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000EC74 File Offset: 0x0000CE74
		public static Texture2D FindFromProvider(string path, string packageId)
		{
			bool flag = GenText.NullOrEmpty(packageId);
			Texture2D texture2D;
			if (flag)
			{
				texture2D = null;
			}
			else
			{
				List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
				for (int i = 0; i < runningModsListForReading.Count; i++)
				{
					ModContentPack modContentPack = runningModsListForReading[i];
					bool flag2 = modContentPack.IsOfficialMod || modContentPack.PackageId != packageId;
					if (!flag2)
					{
						return modContentPack.GetContentHolder<Texture2D>().Get(path);
					}
				}
				texture2D = null;
			}
			return texture2D;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000ECF0 File Offset: 0x0000CEF0
		public static Texture2D Find(string path, bool coreOnly)
		{
			bool flag = !coreOnly;
			Texture2D texture2D;
			if (flag)
			{
				texture2D = ContentFinder<Texture2D>.Get(path, false);
			}
			else
			{
				List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
				for (int i = 0; i < runningModsListForReading.Count; i++)
				{
					bool flag2 = !runningModsListForReading[i].IsOfficialMod;
					if (!flag2)
					{
						Texture2D texture2D2 = runningModsListForReading[i].GetContentHolder<Texture2D>().Get(path);
						bool flag3 = texture2D2 != null;
						if (flag3)
						{
							return texture2D2;
						}
					}
				}
				string text = Path.Combine(Path.Combine("Assets", "Data"), string.Empty);
				string text2 = GenFilePaths.ContentPath<Texture2D>();
				for (int j = 0; j < runningModsListForReading.Count; j++)
				{
					ModContentPack modContentPack = runningModsListForReading[j];
					bool flag4;
					if (modContentPack.IsOfficialMod)
					{
						ModAssetBundlesHandler assetBundles = modContentPack.assetBundles;
						flag4 = ((assetBundles != null) ? assetBundles.loadedAssetBundles : null) == null;
					}
					else
					{
						flag4 = true;
					}
					bool flag5 = flag4;
					if (!flag5)
					{
						string text3 = Path.Combine(Path.Combine(Path.Combine(text, modContentPack.FolderName), text2), path);
						foreach (AssetBundle assetBundle in modContentPack.assetBundles.loadedAssetBundles)
						{
							foreach (string text4 in ModAssetBundlesHandler.TextureExtensions)
							{
								Texture2D texture2D3 = assetBundle.LoadAsset(text3 + text4, typeof(Texture2D)) as Texture2D;
								bool flag6 = texture2D3 != null;
								if (flag6)
								{
									return texture2D3;
								}
							}
						}
					}
				}
				texture2D = null;
			}
			return texture2D;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000EEB8 File Offset: 0x0000D0B8
		public static RotTexSet ResolveRot(string basePath, bool asShaderMask = false, string maskPathOverride = null, bool coreOnly = false, string provider = null)
		{
			TexBake.<>c__DisplayClass16_0 CS$<>8__locals1;
			CS$<>8__locals1.provider = provider;
			CS$<>8__locals1.coreOnly = coreOnly;
			RotTexSet rotTexSet = new RotTexSet();
			string text = basePath;
			string text2 = "";
			if (asShaderMask)
			{
				text = (GenText.NullOrEmpty(maskPathOverride) ? basePath : maskPathOverride);
				text2 = (GenText.NullOrEmpty(maskPathOverride) ? "m" : "");
			}
			Texture2D[] tex = rotTexSet.tex;
			tex[0] = TexBake.<ResolveRot>g__L|16_0(text + "_north" + text2, ref CS$<>8__locals1);
			tex[1] = TexBake.<ResolveRot>g__L|16_0(text + "_east" + text2, ref CS$<>8__locals1);
			tex[2] = TexBake.<ResolveRot>g__L|16_0(text + "_south" + text2, ref CS$<>8__locals1);
			tex[3] = TexBake.<ResolveRot>g__L|16_0(text + "_west" + text2, ref CS$<>8__locals1);
			bool flag = tex[0] == null;
			if (flag)
			{
				bool flag2 = tex[2] != null;
				if (flag2)
				{
					tex[0] = tex[2];
					rotTexSet.extraAngle = 180f;
				}
				else
				{
					bool flag3 = tex[1] != null;
					if (flag3)
					{
						tex[0] = tex[1];
						rotTexSet.extraAngle = -90f;
					}
					else
					{
						bool flag4 = tex[3] != null;
						if (flag4)
						{
							tex[0] = tex[3];
							rotTexSet.extraAngle = 90f;
						}
						else
						{
							tex[0] = TexBake.<ResolveRot>g__L|16_0(text + text2, ref CS$<>8__locals1);
						}
					}
				}
			}
			bool flag5 = tex[0] == null;
			RotTexSet rotTexSet2;
			if (flag5)
			{
				rotTexSet2 = rotTexSet;
			}
			else
			{
				bool flag6 = tex[2] == null;
				if (flag6)
				{
					tex[2] = tex[0];
				}
				bool flag7 = tex[1] == null;
				if (flag7)
				{
					bool flag8 = tex[3] != null;
					if (flag8)
					{
						tex[1] = tex[3];
						rotTexSet.eastFlipped = true;
						rotTexSet.mirrored[1] = true;
					}
					else
					{
						tex[1] = tex[0];
					}
				}
				bool flag9 = tex[3] == null;
				if (flag9)
				{
					bool flag10 = tex[1] != null;
					if (flag10)
					{
						tex[3] = tex[1];
						rotTexSet.westFlipped = true;
						rotTexSet.mirrored[3] = !rotTexSet.mirrored[1];
					}
					else
					{
						tex[3] = tex[0];
					}
				}
				rotTexSet.valid = true;
				rotTexSet2 = rotTexSet;
			}
			return rotTexSet2;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000F0E8 File Offset: 0x0000D2E8
		public static Texture2D Readable(Texture src)
		{
			bool flag = src == null;
			Texture2D texture2D;
			if (flag)
			{
				texture2D = null;
			}
			else
			{
				int instanceID = src.GetInstanceID();
				Texture2D texture2D2;
				bool flag2 = TexBake.readableCache.TryGetValue(instanceID, out texture2D2) && texture2D2 != null;
				if (flag2)
				{
					texture2D = texture2D2;
				}
				else
				{
					RenderTexture temporary = RenderTexture.GetTemporary(src.width, src.height, 0, 0, 0);
					RenderTexture active = RenderTexture.active;
					Graphics.Blit(src, temporary);
					RenderTexture.active = temporary;
					Texture2D texture2D3 = new Texture2D(src.width, src.height, 4, false);
					texture2D3.ReadPixels(new Rect(0f, 0f, (float)src.width, (float)src.height), 0, 0);
					texture2D3.Apply(false, false);
					RenderTexture.active = active;
					RenderTexture.ReleaseTemporary(temporary);
					texture2D3.name = "TailorMade_readable_" + src.name;
					TexBake.readableCache[instanceID] = texture2D3;
					texture2D = texture2D3;
				}
			}
			return texture2D;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000F1E8 File Offset: 0x0000D3E8
		public static Rect AlphaBoundsUV(Texture2D readable)
		{
			int instanceID = readable.GetInstanceID();
			Rect rect;
			bool flag = TexBake.boundsCache.TryGetValue(instanceID, out rect);
			Rect rect2;
			if (flag)
			{
				rect2 = rect;
			}
			else
			{
				Color32[] pixels = readable.GetPixels32();
				int width = readable.width;
				int height = readable.height;
				byte b = 25;
				int num = width;
				int num2 = height;
				int num3 = -1;
				int num4 = -1;
				for (int i = 0; i < height; i++)
				{
					int num5 = i * width;
					for (int j = 0; j < width; j++)
					{
						bool flag2 = pixels[num5 + j].a < b;
						if (!flag2)
						{
							bool flag3 = j < num;
							if (flag3)
							{
								num = j;
							}
							bool flag4 = j > num3;
							if (flag4)
							{
								num3 = j;
							}
							bool flag5 = i < num2;
							if (flag5)
							{
								num2 = i;
							}
							bool flag6 = i > num4;
							if (flag6)
							{
								num4 = i;
							}
						}
					}
				}
				Rect rect3 = ((num3 < 0) ? new Rect(0f, 0f, 1f, 1f) : new Rect((float)num / (float)width, (float)num2 / (float)height, (float)(num3 - num + 1) / (float)width, (float)(num4 - num2 + 1) / (float)height));
				TexBake.boundsCache[instanceID] = rect3;
				rect2 = rect3;
			}
			return rect2;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000F33C File Offset: 0x0000D53C
		public static Rect AlphaBoundsUVInBand(Texture2D readable, float vLo, float vHi)
		{
			Rect rect = TexBake.AlphaBoundsUV(readable);
			bool flag = rect.height <= 0.0001f;
			Rect rect2;
			if (flag)
			{
				rect2 = rect;
			}
			else
			{
				string text = string.Concat(new string[]
				{
					readable.GetInstanceID().ToString(),
					":",
					vLo.ToString("f3"),
					":",
					vHi.ToString("f3")
				});
				Rect rect3;
				bool flag2 = TexBake.bandBoundsCache.TryGetValue(text, out rect3);
				if (flag2)
				{
					rect2 = rect3;
				}
				else
				{
					float num = rect.y + Mathf.Clamp01(vLo) * rect.height;
					float num2 = rect.y + Mathf.Clamp01(vHi) * rect.height;
					Color32[] pixels = readable.GetPixels32();
					int width = readable.width;
					int height = readable.height;
					byte b = 25;
					int num3 = Mathf.Clamp(Mathf.FloorToInt(num * (float)height), 0, height - 1);
					int num4 = Mathf.Clamp(Mathf.CeilToInt(num2 * (float)height) - 1, 0, height - 1);
					int num5 = width;
					int num6 = height;
					int num7 = -1;
					int num8 = -1;
					for (int i = num3; i <= num4; i++)
					{
						int num9 = i * width;
						for (int j = 0; j < width; j++)
						{
							bool flag3 = pixels[num9 + j].a < b;
							if (!flag3)
							{
								bool flag4 = j < num5;
								if (flag4)
								{
									num5 = j;
								}
								bool flag5 = j > num7;
								if (flag5)
								{
									num7 = j;
								}
								bool flag6 = i < num6;
								if (flag6)
								{
									num6 = i;
								}
								bool flag7 = i > num8;
								if (flag7)
								{
									num8 = i;
								}
							}
						}
					}
					Rect rect4 = ((num7 < 0) ? new Rect(rect.x, num, rect.width, Mathf.Max(0.001f, num2 - num)) : new Rect((float)num5 / (float)width, (float)num6 / (float)height, (float)(num7 - num5 + 1) / (float)width, (float)(num8 - num6 + 1) / (float)height));
					TexBake.bandBoundsCache[text] = rect4;
					rect2 = rect4;
				}
			}
			return rect2;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000F56C File Offset: 0x0000D76C
		public static Rect MirrorX(Rect r)
		{
			return new Rect(1f - r.xMax, r.y, r.width, r.height);
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000F5A8 File Offset: 0x0000D7A8
		private static TexBake.RowExtents GetRowExtents(Texture2D readable)
		{
			int instanceID = readable.GetInstanceID();
			TexBake.RowExtents rowExtents;
			bool flag = TexBake.extentsCache.TryGetValue(instanceID, out rowExtents);
			TexBake.RowExtents rowExtents2;
			if (flag)
			{
				rowExtents2 = rowExtents;
			}
			else
			{
				Color32[] pixels = readable.GetPixels32();
				int width = readable.width;
				int height = readable.height;
				byte b = 25;
				TexBake.RowExtents rowExtents3 = new TexBake.RowExtents
				{
					left = new float[height],
					right = new float[height],
					any = new bool[height],
					rows = height
				};
				for (int i = 0; i < height; i++)
				{
					int num = i * width;
					int num2 = -1;
					int num3 = -1;
					for (int j = 0; j < width; j++)
					{
						bool flag2 = pixels[num + j].a < b;
						if (!flag2)
						{
							bool flag3 = num2 < 0;
							if (flag3)
							{
								num2 = j;
							}
							num3 = j;
						}
					}
					bool flag4 = num2 >= 0;
					if (flag4)
					{
						rowExtents3.any[i] = true;
						rowExtents3.left[i] = (float)num2 / (float)width;
						rowExtents3.right[i] = (float)(num3 + 1) / (float)width;
					}
				}
				TexBake.extentsCache[instanceID] = rowExtents3;
				rowExtents2 = rowExtents3;
			}
			return rowExtents2;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000F6F8 File Offset: 0x0000D8F8
		private static bool TryGetExtent(TexBake.RowExtents ext, float v, bool mirrored, out float left, out float right)
		{
			left = 0f;
			right = 1f;
			bool flag = ext == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				int num = Mathf.Clamp((int)(v * (float)ext.rows), 0, ext.rows - 1);
				bool flag3 = !ext.any[num];
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					float num2 = 0f;
					float num3 = 0f;
					int num4 = 0;
					for (int i = -1; i <= 1; i++)
					{
						int num5 = num + i;
						bool flag4 = num5 < 0 || num5 >= ext.rows || !ext.any[num5];
						if (!flag4)
						{
							num2 += ext.left[num5];
							num3 += ext.right[num5];
							num4++;
						}
					}
					num2 /= (float)num4;
					num3 /= (float)num4;
					if (mirrored)
					{
						left = 1f - num3;
						right = 1f - num2;
					}
					else
					{
						left = num2;
						right = num3;
					}
					flag2 = right > left + 0.001f;
				}
			}
			return flag2;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000F810 File Offset: 0x0000DA10
		public static Texture2D BakeFitted(string cacheKey, Texture src, Texture2D maskReadable, bool mirrorMask, TailorPatternDef manualDef, Rot4 rot, bool multiplyAlpha, Texture2D canvasReadable = null, bool mirrorCanvas = false, Vector2 adjustOffset = default(Vector2), float adjustScale = 1f, float adjustAngle = 0f, bool? unlockOverride = null, Vector2 fitBand = default(Vector2))
		{
			bool flag = fitBand.y > fitBand.x + 0.001f && (fitBand.x > 0.001f || fitBand.y < 0.999f);
			cacheKey = TexBake.Version.ToString() + ":" + cacheKey;
			Texture2D texture2D;
			bool flag2 = TexBake.bakeCache.TryGetValue(cacheKey, out texture2D) && texture2D != null;
			Texture2D texture2D2;
			if (flag2)
			{
				texture2D2 = texture2D;
			}
			else
			{
				bool flag3 = adjustOffset.x != 0f || adjustOffset.y != 0f || Mathf.Abs(adjustScale - 1f) > 0.0001f;
				float num = Mathf.Max(0.05f, adjustScale);
				bool flag4 = Mathf.Abs(adjustAngle) > 0.01f;
				float num2 = -adjustAngle * 0.017453292f;
				float num3 = Mathf.Cos(num2);
				float num4 = Mathf.Sin(num2);
				TailorMadeSettings settings = TailorMadeMod.Settings;
				Texture2D texture2D3 = TexBake.Readable(src);
				bool flag5 = texture2D3 == null;
				if (flag5)
				{
					texture2D2 = null;
				}
				else
				{
					int num5 = Mathf.Max(new int[]
					{
						texture2D3.width,
						(maskReadable != null) ? maskReadable.width : 0,
						64
					});
					num5 = Mathf.Min(num5, (unlockOverride ?? settings.unlockResolution) ? 4096 : settings.maxResolution);
					bool sharpResampling = settings.sharpResampling;
					Color32[] array = (sharpResampling ? TexBake.GetCachedPixels(texture2D3) : null);
					int width = texture2D3.width;
					int height = texture2D3.height;
					bool flag6 = manualDef != null && !manualDef.autoFit;
					bool flag7 = !flag6 && settings.autoFit && maskReadable != null;
					Rect rect = default(Rect);
					Rect rect2 = default(Rect);
					bool flag8 = flag7;
					if (flag8)
					{
						bool flag9 = canvasReadable != null;
						if (flag9)
						{
							rect = TexBake.AlphaBoundsUV(canvasReadable);
							if (mirrorCanvas)
							{
								rect = TexBake.MirrorX(rect);
							}
						}
						else
						{
							rect = TexBake.AlphaBoundsUV(texture2D3);
						}
						rect2 = (flag ? TexBake.AlphaBoundsUVInBand(maskReadable, fitBand.x, fitBand.y) : TexBake.AlphaBoundsUV(maskReadable));
						if (mirrorMask)
						{
							rect2 = TexBake.MirrorX(rect2);
						}
					}
					bool flag10 = flag7 && !flag && (rot == Rot4.North || rot == Rot4.South);
					if (flag10)
					{
						float num6 = ((rect2.height > 0.001f) ? (rect2.width / rect2.height) : 1f);
						bool flag11 = rect2.width < 0.18f || num6 < 0.3f;
						if (flag11)
						{
							Log.WarningOnce(string.Concat(new string[]
							{
								"[TailorMade] Degenerate body silhouette (w=",
								rect2.width.ToString("f3"),
								" h=",
								rect2.height.ToString("f3"),
								" aspect=",
								num6.ToString("f2"),
								") — rendering apparel native for ",
								cacheKey
							}), ("degen" + cacheKey).GetHashCode());
							flag7 = false;
							multiplyAlpha = false;
						}
					}
					bool flag12 = flag7 && canvasReadable != null && !settings.uniformScale && (rot == Rot4.North || rot == Rot4.South);
					TexBake.RowExtents rowExtents = (flag12 ? TexBake.GetRowExtents(maskReadable) : null);
					TexBake.RowExtents rowExtents2 = (flag12 ? TexBake.GetRowExtents(canvasReadable) : null);
					float num7 = 1f;
					float num8 = 0f;
					float num9 = 1f;
					float num10 = 0f;
					bool flag13 = flag7;
					if (flag13)
					{
						bool uniformScale = settings.uniformScale;
						if (uniformScale)
						{
							float num11 = Mathf.Min((rect2.width > 0f) ? (rect.width / rect2.width) : 1f, (rect2.height > 0f) ? (rect.height / rect2.height) : 1f);
							num7 = num11;
							num9 = num11;
							num8 = rect.center.x - rect2.center.x * num11;
							num10 = rect.center.y - rect2.center.y * num11;
						}
						else
						{
							num7 = ((rect2.width > 0f) ? (rect.width / rect2.width) : 1f);
							num9 = ((rect2.height > 0f) ? (rect.height / rect2.height) : 1f);
							num8 = rect.x - rect2.x * num7;
							num10 = rect.y - rect2.y * num9;
						}
					}
					Vector2 vector = (flag6 ? manualDef.GetOffset(rot) : Vector2.zero);
					float num12 = (flag6 ? Mathf.Max(0.0001f, manualDef.GetScale(rot)) : 1f);
					Texture2D texture2D4 = new Texture2D(num5, num5, 4, true);
					Color[] array2 = TexBake.BakeBuf(num5 * num5);
					float num13 = 1f / (float)num5;
					for (int i = 0; i < num5; i++)
					{
						float num14 = ((float)i + 0.5f) * num13;
						int num15 = i * num5;
						for (int j = 0; j < num5; j++)
						{
							float num16 = ((float)j + 0.5f) * num13;
							float num17 = num16;
							float num18 = num14;
							bool flag14 = flag4;
							if (flag14)
							{
								float num19 = num16 - 0.5f;
								float num20 = num14 - 0.5f;
								num17 = 0.5f + num19 * num3 - num20 * num4;
								num18 = 0.5f + num19 * num4 + num20 * num3;
							}
							bool flag15 = flag6;
							float num21;
							float num22;
							if (flag15)
							{
								num21 = (num17 - 0.5f - vector.x) / num12 + 0.5f;
								num22 = (num18 - 0.5f - vector.y) / num12 + 0.5f;
							}
							else
							{
								num22 = num9 * num18 + num10;
								float num23;
								float num24;
								float num25;
								float num26;
								bool flag16 = flag12 && TexBake.TryGetExtent(rowExtents, num18, mirrorMask, out num23, out num24) && TexBake.TryGetExtent(rowExtents2, num22, mirrorCanvas, out num25, out num26);
								if (flag16)
								{
									num21 = num25 + (num17 - num23) * (num26 - num25) / (num24 - num23);
								}
								else
								{
									num21 = num7 * num17 + num8;
								}
							}
							bool flag17 = flag3;
							if (flag17)
							{
								num21 = (num21 - 0.5f) / num + 0.5f - adjustOffset.x;
								num22 = (num22 - 0.5f) / num + 0.5f - adjustOffset.y;
							}
							bool flag18 = num21 < 0f || num21 > 1f || num22 < 0f || num22 > 1f;
							Color color;
							if (flag18)
							{
								color = Color.clear;
							}
							else
							{
								bool flag19 = sharpResampling;
								if (flag19)
								{
									color = TexBake.SampleBicubic(array, width, height, num21, num22);
								}
								else
								{
									color = texture2D3.GetPixelBilinear(num21, num22);
								}
							}
							bool flag20 = multiplyAlpha && maskReadable != null && color.a > 0f;
							if (flag20)
							{
								float num27 = (mirrorMask ? (1f - num17) : num17);
								color.a *= maskReadable.GetPixelBilinear(num27, num18).a;
							}
							array2[num15 + j] = color;
						}
					}
					bool flag21 = multiplyAlpha && settings.bodyMaskOutline && settings.outlinePixels > 0;
					if (flag21)
					{
						int num28 = settings.outlinePixels;
						bool flag22 = rot == Rot4.East || rot == Rot4.West;
						if (flag22)
						{
							num28 = Mathf.Max(num28, Mathf.RoundToInt((float)num28 * Mathf.Max(1f, settings.sideOutlineBoost)));
						}
						TexBake.ApplyOutline(array2, num5, num28);
					}
					texture2D4.SetPixels(array2);
					texture2D4.Apply(true, false);
					texture2D4.filterMode = (settings.trilinearFilter ? 2 : 1);
					texture2D4.wrapMode = 1;
					texture2D4.name = "TailorMade_" + cacheKey;
					TexBake.bakeCache[cacheKey] = texture2D4;
					texture2D2 = texture2D4;
				}
			}
			return texture2D2;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00010084 File Offset: 0x0000E284
		private static void ApplyOutline(Color[] px, int size, int thickness)
		{
			int num = size * size;
			bool flag = TexBake._outlineMask == null || TexBake._outlineMask.Length != num;
			if (flag)
			{
				TexBake._outlineMask = new bool[num];
				TexBake._outlineDilX = new bool[num];
			}
			bool[] outlineMask = TexBake._outlineMask;
			bool[] outlineDilX = TexBake._outlineDilX;
			for (int i = 0; i < num; i++)
			{
				outlineMask[i] = px[i].a >= 0.1f;
			}
			for (int j = 0; j < size; j++)
			{
				int num2 = j * size;
				for (int k = 0; k < size; k++)
				{
					bool flag2 = k < thickness || k >= size - thickness;
					bool flag3 = !flag2;
					if (flag3)
					{
						int num3 = k - thickness;
						int num4 = k + thickness;
						for (int l = num3; l <= num4; l++)
						{
							bool flag4 = !outlineMask[num2 + l];
							if (flag4)
							{
								flag2 = true;
								break;
							}
						}
					}
					outlineDilX[num2 + k] = flag2;
				}
			}
			Color color;
			color..ctor(0.06f, 0.06f, 0.07f, 1f);
			for (int m = 0; m < size; m++)
			{
				int num5 = m * size;
				for (int n = 0; n < size; n++)
				{
					int num6 = num5 + n;
					bool flag5 = !outlineMask[num6];
					if (!flag5)
					{
						bool flag6 = m < thickness || m >= size - thickness;
						bool flag7 = !flag6;
						if (flag7)
						{
							int num7 = m - thickness;
							int num8 = m + thickness;
							for (int num9 = num7; num9 <= num8; num9++)
							{
								bool flag8 = outlineDilX[num9 * size + n];
								if (flag8)
								{
									flag6 = true;
									break;
								}
							}
						}
						bool flag9 = flag6;
						if (flag9)
						{
							color.a = px[num6].a;
							px[num6] = color;
						}
					}
				}
			}
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000102AC File Offset: 0x0000E4AC
		private static Color32[] GetCachedPixels(Texture2D t)
		{
			int instanceID = t.GetInstanceID();
			Color32[] pixels;
			bool flag = TexBake.pixelCache.TryGetValue(instanceID, out pixels) && pixels != null;
			Color32[] array;
			if (flag)
			{
				array = pixels;
			}
			else
			{
				pixels = t.GetPixels32();
				TexBake.pixelCache[instanceID] = pixels;
				array = pixels;
			}
			return array;
		}

		// Token: 0x0600010D RID: 269 RVA: 0x000102F8 File Offset: 0x0000E4F8
		private static Color SampleBicubic(Color32[] px, int w, int h, float u, float v)
		{
			float num = u * (float)w - 0.5f;
			float num2 = v * (float)h - 0.5f;
			int num3 = Mathf.FloorToInt(num);
			int num4 = Mathf.FloorToInt(num2);
			float num5 = num - (float)num3;
			float num6 = num2 - (float)num4;
			float num7 = 0f;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = 0f;
			for (int i = -1; i <= 2; i++)
			{
				int num11 = Mathf.Clamp(num4 + i, 0, h - 1);
				float num12 = TexBake.CubicWeight((float)i - num6);
				for (int j = -1; j <= 2; j++)
				{
					int num13 = Mathf.Clamp(num3 + j, 0, w - 1);
					float num14 = num12 * TexBake.CubicWeight((float)j - num5);
					Color32 color = px[num11 * w + num13];
					num7 += (float)color.r * num14;
					num8 += (float)color.g * num14;
					num9 += (float)color.b * num14;
					num10 += (float)color.a * num14;
				}
			}
			return new Color(Mathf.Clamp01(num7 / 255f), Mathf.Clamp01(num8 / 255f), Mathf.Clamp01(num9 / 255f), Mathf.Clamp01(num10 / 255f));
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0001045C File Offset: 0x0000E65C
		private static float CubicWeight(float t)
		{
			t = Mathf.Abs(t);
			bool flag = t <= 1f;
			float num;
			if (flag)
			{
				num = 1.5f * t * t * t - 2.5f * t * t + 1f;
			}
			else
			{
				bool flag2 = t < 2f;
				if (flag2)
				{
					num = -0.5f * t * t * t + 2.5f * t * t - 4f * t + 2f;
				}
				else
				{
					num = 0f;
				}
			}
			return num;
		}

		// Token: 0x0600010F RID: 271 RVA: 0x000104DC File Offset: 0x0000E6DC
		public static Texture2D BakeScaled(string cacheKey, Texture src, float scale, bool? unlockOverride = null)
		{
			Texture2D texture2D;
			bool flag = TexBake.bakeCache.TryGetValue(cacheKey, out texture2D) && texture2D != null;
			Texture2D texture2D2;
			if (flag)
			{
				texture2D2 = texture2D;
			}
			else
			{
				TailorMadeSettings settings = TailorMadeMod.Settings;
				Texture2D texture2D3 = TexBake.Readable(src);
				bool flag2 = texture2D3 == null;
				if (flag2)
				{
					texture2D2 = null;
				}
				else
				{
					int num = Mathf.Min(Mathf.Max(texture2D3.width, 64), (unlockOverride ?? settings.unlockResolution) ? 4096 : settings.maxResolution);
					Texture2D texture2D4 = new Texture2D(num, num, 4, true);
					Color[] array = TexBake.BakeBuf(num * num);
					float num2 = 1f / (float)num;
					for (int i = 0; i < num; i++)
					{
						float num3 = ((float)i + 0.5f) * num2;
						float num4 = (num3 - 0.5f) / scale + 0.5f;
						int num5 = i * num;
						for (int j = 0; j < num; j++)
						{
							float num6 = ((float)j + 0.5f) * num2;
							float num7 = (num6 - 0.5f) / scale + 0.5f;
							array[num5 + j] = ((num7 < 0f || num7 > 1f || num4 < 0f || num4 > 1f) ? Color.clear : texture2D3.GetPixelBilinear(num7, num4));
						}
					}
					texture2D4.SetPixels(array);
					texture2D4.Apply(true, false);
					texture2D4.filterMode = (settings.trilinearFilter ? 2 : 1);
					texture2D4.wrapMode = 1;
					texture2D4.name = "TailorMade_" + cacheKey;
					TexBake.bakeCache[cacheKey] = texture2D4;
					texture2D2 = texture2D4;
				}
			}
			return texture2D2;
		}

		// Token: 0x06000110 RID: 272 RVA: 0x000106A8 File Offset: 0x0000E8A8
		public static void Clear()
		{
			foreach (KeyValuePair<string, Texture2D> keyValuePair in TexBake.bakeCache)
			{
				bool flag = keyValuePair.Value != null;
				if (flag)
				{
					Object.Destroy(keyValuePair.Value);
				}
			}
			TexBake.bakeCache.Clear();
			foreach (KeyValuePair<int, Texture2D> keyValuePair2 in TexBake.readableCache)
			{
				bool flag2 = keyValuePair2.Value != null;
				if (flag2)
				{
					Object.Destroy(keyValuePair2.Value);
				}
			}
			TexBake.readableCache.Clear();
			TexBake.boundsCache.Clear();
			TexBake.bandBoundsCache.Clear();
			TexBake.extentsCache.Clear();
			TexBake.pixelCache.Clear();
		}

		// Token: 0x06000112 RID: 274 RVA: 0x000107F2 File Offset: 0x0000E9F2
		[CompilerGenerated]
		internal static Texture2D <ResolveRot>g__L|16_0(string p, ref TexBake.<>c__DisplayClass16_0 A_1)
		{
			return GenText.NullOrEmpty(A_1.provider) ? TexBake.Find(p, A_1.coreOnly) : TexBake.FindFromProvider(p, A_1.provider);
		}

		// Token: 0x04000103 RID: 259
		private static readonly Dictionary<int, Texture2D> readableCache = new Dictionary<int, Texture2D>();

		// Token: 0x04000104 RID: 260
		private static readonly Dictionary<int, Rect> boundsCache = new Dictionary<int, Rect>();

		// Token: 0x04000105 RID: 261
		private static readonly Dictionary<string, Rect> bandBoundsCache = new Dictionary<string, Rect>();

		// Token: 0x04000106 RID: 262
		private static readonly Dictionary<string, Texture2D> bakeCache = new Dictionary<string, Texture2D>();

		// Token: 0x04000107 RID: 263
		public static int Version;

		// Token: 0x04000108 RID: 264
		private static Color[] _bakeBuf;

		// Token: 0x04000109 RID: 265
		private static readonly Dictionary<int, TexBake.RowExtents> extentsCache = new Dictionary<int, TexBake.RowExtents>();

		// Token: 0x0400010A RID: 266
		public const float AlphaThreshold = 0.1f;

		// Token: 0x0400010B RID: 267
		private static string bodyProviderName;

		// Token: 0x0400010C RID: 268
		private static List<ModContentPack> bodyProviders;

		// Token: 0x0400010D RID: 269
		private static bool[] _outlineMask;

		// Token: 0x0400010E RID: 270
		private static bool[] _outlineDilX;

		// Token: 0x0400010F RID: 271
		private static readonly Dictionary<int, Color32[]> pixelCache = new Dictionary<int, Color32[]>();

		// Token: 0x0200003A RID: 58
		private class RowExtents
		{
			// Token: 0x04000135 RID: 309
			public float[] left;

			// Token: 0x04000136 RID: 310
			public float[] right;

			// Token: 0x04000137 RID: 311
			public bool[] any;

			// Token: 0x04000138 RID: 312
			public int rows;
		}
	}
}
