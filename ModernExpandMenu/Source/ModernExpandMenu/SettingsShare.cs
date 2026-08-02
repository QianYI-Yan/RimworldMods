using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 配置分享：把 Mod 设置导出为独立 XML 文件 / 剪贴板文本，
    // 便于备份与分享给其他玩家。
    // 说明：RimWorld 的标准 ModSettings 由游戏自动保存（ExposeData/Write），
    // 这里额外提供"独立文件 + 剪贴板"的导出导入，不改动标准配置机制。
    // ═══════════════════════════════════════════════════
    public static class SettingsShare
    {
        public const string ShareFolderName = "ModernExpandMenuShare";

        /// <summary>分享文件存放目录（存档数据目录下，RimWorld 官方数据路径接口）。</summary>
        public static string ShareFolderPath => Path.Combine(GenFilePaths.SaveDataFolderPath, ShareFolderName);

        /// <summary>把当前设置导出为 XML 字符串（只含本模组自定义字段，不含游戏内部字段）。</summary>
        public static string ExportToString()
        {
            ModernExpandMenuSettings s = ModernExpandMenuMod.Settings;
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<ModernExpandMenuSettings>");
            foreach (FieldInfo field in typeof(ModernExpandMenuSettings).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                string text = ConvertFieldToString(field.FieldType, field.GetValue(s));
                if (text == null)
                {
                    continue;
                }
                sb.Append("  <").Append(field.Name).Append('>')
                  .Append(EscapeXml(text))
                  .Append("</").Append(field.Name).AppendLine(">");
            }
            sb.AppendLine("</ModernExpandMenuSettings>");
            return sb.ToString();
        }

        /// <summary>把设置导出为 XML 并保存到分享文件夹（文件名带时间戳），返回文件路径。</summary>
        public static string SaveToFile()
        {
            if (!Directory.Exists(ShareFolderPath))
            {
                Directory.CreateDirectory(ShareFolderPath);
            }
            string fileName = "ModernExpandMenu_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml";
            string path = Path.Combine(ShareFolderPath, fileName);
            File.WriteAllText(path, ExportToString(), new UTF8Encoding(false));
            return path;
        }

        /// <summary>从 XML 字符串导入设置并应用（失败返回 false）。</summary>
        public static bool ImportFromString(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return false;
            }
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlElement root = doc.DocumentElement;
                if (root == null || root.Name != "ModernExpandMenuSettings")
                {
                    return false;
                }
                ModernExpandMenuSettings s = ModernExpandMenuMod.Settings;
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }
                    FieldInfo field = typeof(ModernExpandMenuSettings).GetField(node.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (field == null)
                    {
                        continue;
                    }
                    try
                    {
                        object parsed = ParseFieldValue(field.FieldType, node.InnerText.Trim());
                        if (parsed != null)
                        {
                            field.SetValue(s, parsed);
                        }
                    }
                    catch
                    {
                        // 单个字段解析失败不影响其余字段
                    }
                }
                s.Write();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>读取分享文件夹中最新的配置文件内容（无则返回 null；扫描所有 xml，兼容重命名后的文件）。</summary>
        public static string LoadLatestFileContent()
        {
            if (!Directory.Exists(ShareFolderPath))
            {
                return null;
            }
            string[] files = Directory.GetFiles(ShareFolderPath, "*.xml");
            if (files.Length == 0)
            {
                return null;
            }
            Array.Sort(files, (a, b) => string.Compare(b, a, StringComparison.Ordinal));
            return File.ReadAllText(files[0]);
        }

        /// <summary>按字段类型把值转为导出文本（支持 bool/int/float/string；浮点用 InvariantCulture 保证跨语言一致）。</summary>
        private static string ConvertFieldToString(Type type, object value)
        {
            if (type == typeof(bool) || type == typeof(int) || type == typeof(string))
            {
                return value.ToString();
            }
            if (type == typeof(float))
            {
                return ((float)value).ToString("R", CultureInfo.InvariantCulture);
            }
            return null;
        }

        /// <summary>按字段类型解析导入文本。</summary>
        private static object ParseFieldValue(Type type, string text)
        {
            if (type == typeof(bool))
            {
                return bool.Parse(text);
            }
            if (type == typeof(int))
            {
                return int.Parse(text, CultureInfo.InvariantCulture);
            }
            if (type == typeof(float))
            {
                return float.Parse(text, CultureInfo.InvariantCulture);
            }
            if (type == typeof(string))
            {
                return text;
            }
            return null;
        }

        /// <summary>XML 特殊字符转义。</summary>
        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
