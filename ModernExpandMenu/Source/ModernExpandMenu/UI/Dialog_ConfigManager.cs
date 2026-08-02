using System;
using System.Collections.Generic;
using System.IO;
using ModernExpandMenu.Theme;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // 配置管理对话框（类 Windows 资源管理器）：
    //   列出分享文件夹中的配置文件（名称 / 大小 / 修改时间），
    //   支持选择、导入所选、删除所选、刷新。
    // ═══════════════════════════════════════════════════
    public class Dialog_ConfigManager : Window
    {
        private readonly List<FileInfo> files = new List<FileInfo>();
        private int selectedIndex = -1;
        private Vector2 scrollPosition;
        private const float RowHeight = 32f;

        public Dialog_ConfigManager()
        {
            doWindowBackground = false;
            drawShadow = false;
            layer = WindowLayer.Dialog;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnClickedOutside = false;
            RefreshFiles();
        }

        public override Vector2 InitialSize => new Vector2(640f, 520f);

        /// <summary>重新扫描分享文件夹中的配置文件（按名称倒序，最新在前）。</summary>
        private void RefreshFiles()
        {
            files.Clear();
            if (Directory.Exists(SettingsShare.ShareFolderPath))
            {
                // 扫描所有 xml（不限 ModernExpandMenu_ 前缀），保证重命名后的文件仍能在列表中显示
                foreach (string path in Directory.GetFiles(SettingsShare.ShareFolderPath, "*.xml"))
                {
                    try
                    {
                        files.Add(new FileInfo(path));
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("[ModernExpandMenu] 读取配置文件失败 " + path + "：" + ex.Message);
                    }
                }
            }
            files.Sort((a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));
            selectedIndex = files.Count > 0 ? 0 : -1;
        }

        public override void DoWindowContents(Rect inRect)
        {
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 标题
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 12f, inRect.width - 40f, 30f), "ModernExpandMenu_ConfigManagerTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 文件列表
            float listTop = inRect.y + 52f;
            float listHeight = inRect.height - 52f - 60f;
            if (files.Count == 0)
            {
                // 空状态提示
                GUI.color = MD3Theme.OnSurfaceVariant;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = true;
                Widgets.Label(new Rect(inRect.x + 30f, listTop, inRect.width - 60f, listHeight), "ModernExpandMenu_ConfigManagerEmpty".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
            }
            else
            {
                var viewRect = new Rect(0f, 0f, inRect.width - 44f, files.Count * RowHeight);
                var listRect = new Rect(inRect.x + 14f, listTop, inRect.width - 28f, listHeight);
                MD3Widgets.MD3BeginScrollView(listRect, ref scrollPosition, viewRect);
                for (int i = 0; i < files.Count; i++)
                {
                    DrawFileRow(inRect, i);
                }
                MD3Widgets.MD3EndScrollView(listRect, ref scrollPosition, files.Count * RowHeight, 2001, MD3Theme.CardCornerRadius);
            }

            // 底部按钮：刷新 / 重命名所选 / 删除所选 / 导入所选 / 关闭
            float buttonY = inRect.yMax - 48f;
            float buttonWidth = (inRect.width - 40f - 16f) / 5f;
            var refreshRect = new Rect(inRect.x + 20f, buttonY, buttonWidth, 34f);
            var renameRect = new Rect(inRect.x + 20f + (buttonWidth + 4f), buttonY, buttonWidth, 34f);
            var deleteRect = new Rect(inRect.x + 20f + (buttonWidth + 4f) * 2f, buttonY, buttonWidth, 34f);
            var importRect = new Rect(inRect.x + 20f + (buttonWidth + 4f) * 3f, buttonY, buttonWidth, 34f);
            var closeRect = new Rect(inRect.x + 20f + (buttonWidth + 4f) * 4f, buttonY, buttonWidth, 34f);
            bool hasSelection = selectedIndex >= 0;
            if (MD3Widgets.MD3Button(refreshRect, "ModernExpandMenu_ConfigManagerRefresh".Translate(), emphasized: false))
            {
                RefreshFiles();
            }
            if (MD3Widgets.MD3Button(renameRect, "ModernExpandMenu_ConfigManagerRename".Translate(), hasSelection) && hasSelection)
            {
                Find.WindowStack.Add(new Dialog_RenameConfig(files[selectedIndex].FullName, RefreshFiles));
            }
            if (MD3Widgets.MD3Button(deleteRect, "ModernExpandMenu_ConfigManagerDelete".Translate(), hasSelection) && hasSelection)
            {
                DeleteSelectedFile();
            }
            if (MD3Widgets.MD3Button(importRect, "ModernExpandMenu_ConfigManagerImport".Translate(), hasSelection) && hasSelection)
            {
                ImportSelectedFile();
            }
            if (MD3Widgets.MD3Button(closeRect, "ModernExpandMenu_ConfigManagerClose".Translate(), emphasized: false))
            {
                Close();
            }
        }

        /// <summary>绘制单行文件信息（名称 / 大小 / 修改时间），点击选中。</summary>
        private void DrawFileRow(Rect inRect, int index)
        {
            var rowRect = new Rect(10f, index * RowHeight, inRect.width - 44f, RowHeight);
            FileInfo file = files[index];
            bool selected = index == selectedIndex;

            // 选中高亮（主色半透明层）
            if (selected)
            {
                MD3Widgets.DrawRoundedRect(rowRect, MD3Theme.HoverStateLayer, 6f);
            }
            if (Mouse.IsOver(rowRect))
            {
                MD3Widgets.DrawHoverState(rowRect, 6f);
            }

            // 文件名
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x + 10f, rowRect.y, rowRect.width - 220f, rowRect.height), file.Name);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 大小（右侧）
            GUI.color = MD3Theme.OnSurfaceVariant;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.xMax - 210f, rowRect.y, 100f, rowRect.height), FormatSize(file.Length));
            // 修改时间（最右）
            Widgets.Label(new Rect(rowRect.xMax - 105f, rowRect.y, 100f, rowRect.height), file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 点击选中
            if (Widgets.ButtonInvisible(rowRect))
            {
                selectedIndex = index;
            }
        }

        /// <summary>导入当前选中的配置文件并应用。</summary>
        private void ImportSelectedFile()
        {
            try
            {
                string content = File.ReadAllText(files[selectedIndex].FullName);
                bool ok = SettingsShare.ImportFromString(content);
                ShowFeedback(ok ? "ModernExpandMenu_ShareImported".Translate() : "ModernExpandMenu_ShareImportFailed".Translate());
            }
            catch (Exception ex)
            {
                Log.Warning("[ModernExpandMenu] 导入配置失败：" + ex.Message);
                ShowFeedback("ModernExpandMenu_ShareImportFailed".Translate());
            }
        }

        /// <summary>删除当前选中的配置文件并刷新列表。</summary>
        private void DeleteSelectedFile()
        {
            try
            {
                File.Delete(files[selectedIndex].FullName);
                RefreshFiles();
            }
            catch (Exception ex)
            {
                Log.Warning("[ModernExpandMenu] 删除配置失败：" + ex.Message);
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + " B";
            }
            return (bytes / 1024f).ToString("0.0") + " KB";
        }

        private static void ShowFeedback(string text)
        {
            if (Find.CurrentMap != null)
            {
                Messages.Message(text, MessageTypeDefOf.NeutralEvent, historical: false);
            }
            else
            {
                Log.Message("[ModernExpandMenu] " + text);
            }
        }
    }
}
