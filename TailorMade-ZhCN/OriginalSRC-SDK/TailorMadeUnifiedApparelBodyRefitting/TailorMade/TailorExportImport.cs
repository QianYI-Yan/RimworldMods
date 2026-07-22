using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200000C RID: 12
	public static class TailorExportImport
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600003A RID: 58 RVA: 0x00005B1C File Offset: 0x00003D1C
		public static string Folder
		{
			get
			{
				string text = Path.Combine(GenFilePaths.SaveDataFolderPath, "TailorMade");
				Directory.CreateDirectory(text);
				return text;
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00005B46 File Offset: 0x00003D46
		private static string F(float v)
		{
			return v.ToString("0.####");
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00005B54 File Offset: 0x00003D54
		public static string BuildDefXml(string defName, string raceName, string bodyType, TailorAdjust adj, bool hide, bool keepHair, string indent)
		{
			bool flag = adj != null && (adj.offN != Vector2.zero || adj.offE != Vector2.zero || adj.offS != Vector2.zero || adj.offW != Vector2.zero || adj.scaleN != 1f || adj.scaleE != 1f || adj.scaleS != 1f || adj.scaleW != 1f || adj.angN != 0f || adj.angE != 0f || adj.angS != 0f || adj.angW != 0f);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(indent + "<TailorMade.TailorPatternDef>");
			stringBuilder.AppendLine(indent + "  <defName>Tailor_" + defName + "</defName>");
			stringBuilder.AppendLine(indent + "  <raceName>" + (GenText.NullOrEmpty(raceName) ? "*" : raceName) + "</raceName>");
			bool flag2 = !GenText.NullOrEmpty(bodyType);
			if (flag2)
			{
				stringBuilder.AppendLine(indent + "  <bodyType>" + bodyType + "</bodyType>");
			}
			bool flag3 = adj != null && !adj.conform;
			if (flag3)
			{
				stringBuilder.AppendLine(indent + "  <conform>false</conform>");
			}
			bool flag4 = adj != null && adj.renderOrder != 0f;
			if (flag4)
			{
				stringBuilder.AppendLine(indent + "  <renderOrder>" + TailorExportImport.F(adj.renderOrder) + "</renderOrder>");
			}
			if (hide)
			{
				stringBuilder.AppendLine(indent + "  <hide>true</hide>");
			}
			if (keepHair)
			{
				stringBuilder.AppendLine(indent + "  <keepHair>true</keepHair>");
			}
			bool flag5 = flag;
			if (flag5)
			{
				stringBuilder.AppendLine(string.Concat(new string[]
				{
					indent,
					"  <adjustNorth>(",
					TailorExportImport.F(adj.offN.x),
					", ",
					TailorExportImport.F(adj.offN.y),
					")</adjustNorth>"
				}));
				stringBuilder.AppendLine(string.Concat(new string[]
				{
					indent,
					"  <adjustEast>(",
					TailorExportImport.F(adj.offE.x),
					", ",
					TailorExportImport.F(adj.offE.y),
					")</adjustEast>"
				}));
				stringBuilder.AppendLine(string.Concat(new string[]
				{
					indent,
					"  <adjustSouth>(",
					TailorExportImport.F(adj.offS.x),
					", ",
					TailorExportImport.F(adj.offS.y),
					")</adjustSouth>"
				}));
				stringBuilder.AppendLine(string.Concat(new string[]
				{
					indent,
					"  <adjustWest>(",
					TailorExportImport.F(adj.offW.x),
					", ",
					TailorExportImport.F(adj.offW.y),
					")</adjustWest>"
				}));
				stringBuilder.AppendLine(indent + "  <adjustScaleNorth>" + TailorExportImport.F(adj.scaleN) + "</adjustScaleNorth>");
				stringBuilder.AppendLine(indent + "  <adjustScaleEast>" + TailorExportImport.F(adj.scaleE) + "</adjustScaleEast>");
				stringBuilder.AppendLine(indent + "  <adjustScaleSouth>" + TailorExportImport.F(adj.scaleS) + "</adjustScaleSouth>");
				stringBuilder.AppendLine(indent + "  <adjustScaleWest>" + TailorExportImport.F(adj.scaleW) + "</adjustScaleWest>");
				stringBuilder.AppendLine(indent + "  <adjustAngleNorth>" + TailorExportImport.F(adj.angN) + "</adjustAngleNorth>");
				stringBuilder.AppendLine(indent + "  <adjustAngleEast>" + TailorExportImport.F(adj.angE) + "</adjustAngleEast>");
				stringBuilder.AppendLine(indent + "  <adjustAngleSouth>" + TailorExportImport.F(adj.angS) + "</adjustAngleSouth>");
				stringBuilder.AppendLine(indent + "  <adjustAngleWest>" + TailorExportImport.F(adj.angW) + "</adjustAngleWest>");
			}
			stringBuilder.AppendLine(indent + "  <targetApparelDefs>");
			stringBuilder.AppendLine(indent + "    <li>^" + defName + "$</li>");
			stringBuilder.AppendLine(indent + "  </targetApparelDefs>");
			stringBuilder.AppendLine(indent + "</TailorMade.TailorPatternDef>");
			return stringBuilder.ToString();
		}

		// Token: 0x0600003D RID: 61 RVA: 0x0000601C File Offset: 0x0000421C
		private static HashSet<string> ExportKeys()
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			HashSet<string> hashSet = new HashSet<string>();
			bool flag = settings.adjustments != null;
			if (flag)
			{
				foreach (KeyValuePair<string, TailorAdjust> keyValuePair in settings.adjustments)
				{
					bool flag2 = keyValuePair.Value != null && !keyValuePair.Value.IsDefault;
					if (flag2)
					{
						hashSet.Add(keyValuePair.Key);
					}
				}
			}
			bool flag3 = settings.hiddenRenderDefs != null;
			if (flag3)
			{
				foreach (string text in settings.hiddenRenderDefs)
				{
					hashSet.Add(text);
				}
			}
			bool flag4 = settings.hairForceItems != null;
			if (flag4)
			{
				foreach (string text2 in settings.hairForceItems)
				{
					hashSet.Add(text2);
				}
			}
			return hashSet;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x0000616C File Offset: 0x0000436C
		public static int CountExportable()
		{
			return TailorExportImport.ExportKeys().Count;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00006178 File Offset: 0x00004378
		public static string BuildBulkXml()
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			stringBuilder.AppendLine("<!-- TailorMade adjustments export. Drop into a mod's Defs/ folder to ship it,");
			stringBuilder.AppendLine("     or load via TailorMade settings > Import. raceName \"*\" = applies to every race. -->");
			stringBuilder.AppendLine("<Defs>");
			foreach (string text in TailorExportImport.ExportKeys())
			{
				TailorAdjust adjust = settings.GetAdjust(text);
				bool flag = settings.hiddenRenderDefs != null && settings.hiddenRenderDefs.Contains(text);
				bool flag2 = settings.hairForceItems != null && settings.hairForceItems.Contains(text);
				stringBuilder.Append(TailorExportImport.BuildDefXml(text, "*", null, adjust, flag, flag2, "  "));
			}
			stringBuilder.AppendLine("</Defs>");
			return stringBuilder.ToString();
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00006278 File Offset: 0x00004478
		public static string Export()
		{
			string text = Path.Combine(TailorExportImport.Folder, "TailorMade_Adjustments_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml");
			File.WriteAllText(text, TailorExportImport.BuildBulkXml());
			return text;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000062C4 File Offset: 0x000044C4
		public static List<string> ListFiles()
		{
			List<string> list = new List<string>();
			try
			{
				list.AddRange(Directory.GetFiles(TailorExportImport.Folder, "*.xml"));
			}
			catch
			{
			}
			list.Sort();
			list.Reverse();
			return list;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x0000631C File Offset: 0x0000451C
		public static int Import(string file)
		{
			TailorMadeSettings settings = TailorMadeMod.Settings;
			int num = 0;
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(file);
			foreach (object obj in xmlDocument.GetElementsByTagName("TailorMade.TailorPatternDef"))
			{
				XmlNode xmlNode = (XmlNode)obj;
				TailorPatternDef tailorPatternDef;
				try
				{
					tailorPatternDef = DirectXmlToObject.ObjectFromXml<TailorPatternDef>(xmlNode, false);
				}
				catch (Exception ex)
				{
					Log.Warning("[TailorMade] Import skipped a def: " + ex.Message);
					continue;
				}
				string text = TailorExportImport.ApparelKeyFor(tailorPatternDef);
				bool flag = GenText.NullOrEmpty(text);
				if (!flag)
				{
					bool hide = tailorPatternDef.hide;
					if (hide)
					{
						settings.hiddenRenderDefs.Add(text);
					}
					bool keepHair = tailorPatternDef.keepHair;
					if (keepHair)
					{
						settings.hairForceItems.Add(text);
					}
					bool flag2 = !tailorPatternDef.conform || tailorPatternDef.adjustNorth != Vector2.zero || tailorPatternDef.adjustEast != Vector2.zero || tailorPatternDef.adjustSouth != Vector2.zero || tailorPatternDef.adjustWest != Vector2.zero || tailorPatternDef.adjustScaleNorth != 1f || tailorPatternDef.adjustScaleEast != 1f || tailorPatternDef.adjustScaleSouth != 1f || tailorPatternDef.adjustScaleWest != 1f || tailorPatternDef.adjustAngleNorth != 0f || tailorPatternDef.adjustAngleEast != 0f || tailorPatternDef.adjustAngleSouth != 0f || tailorPatternDef.adjustAngleWest != 0f || tailorPatternDef.renderOrder != 0f;
					bool flag3 = flag2;
					if (flag3)
					{
						TailorAdjust orAddAdjust = settings.GetOrAddAdjust(text);
						orAddAdjust.offN = tailorPatternDef.adjustNorth;
						orAddAdjust.offE = tailorPatternDef.adjustEast;
						orAddAdjust.offS = tailorPatternDef.adjustSouth;
						orAddAdjust.offW = tailorPatternDef.adjustWest;
						orAddAdjust.scaleN = tailorPatternDef.adjustScaleNorth;
						orAddAdjust.scaleE = tailorPatternDef.adjustScaleEast;
						orAddAdjust.scaleS = tailorPatternDef.adjustScaleSouth;
						orAddAdjust.scaleW = tailorPatternDef.adjustScaleWest;
						orAddAdjust.angN = tailorPatternDef.adjustAngleNorth;
						orAddAdjust.angE = tailorPatternDef.adjustAngleEast;
						orAddAdjust.angS = tailorPatternDef.adjustAngleSouth;
						orAddAdjust.angW = tailorPatternDef.adjustAngleWest;
						orAddAdjust.conform = tailorPatternDef.conform;
						orAddAdjust.renderOrder = tailorPatternDef.renderOrder;
					}
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x0000660C File Offset: 0x0000480C
		private static string ApparelKeyFor(TailorPatternDef def)
		{
			bool flag = !GenText.NullOrEmpty(def.defName) && def.defName.StartsWith("Tailor_");
			string text;
			if (flag)
			{
				text = def.defName.Substring("Tailor_".Length);
			}
			else
			{
				bool flag2 = def.targetApparelDefs != null && def.targetApparelDefs.Count == 1;
				if (flag2)
				{
					text = def.targetApparelDefs[0].Trim(new char[] { '^', '$' });
				}
				else
				{
					text = null;
				}
			}
			return text;
		}
	}
}
