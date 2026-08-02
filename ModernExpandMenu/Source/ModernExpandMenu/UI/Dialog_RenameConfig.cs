using System;
using System.IO;
using ModernExpandMenu.Theme;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // 重命名配置文件对话框（MD3 风格）：
    //   输入新文件名（自动保留 .xml 扩展名），校验非法字符与重名后重命名。
    // ═══════════════════════════════════════════════════
    public class Dialog_RenameConfig : Window
    {
        private readonly string filePath;
        private readonly Action onRenamed;   // 重命名成功后回调（刷新上级列表）
        private string buffer;
        private bool focusedOnce;

        public Dialog_RenameConfig(string filePath, Action onRenamed)
        {
            this.filePath = filePath;
            this.onRenamed = onRenamed;
            buffer = Path.GetFileNameWithoutExtension(filePath);

            doWindowBackground = false;
            drawShadow = false;
            layer = WindowLayer.Dialog;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(440f, 200f);

        public override void DoWindowContents(Rect inRect)
        {
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 标题
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 14f, inRect.width - 40f, 30f), "ModernExpandMenu_RenameConfigTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 输入框（MD3：深色背景 + 主色描边环，不覆盖文字）
            var inputRect = new Rect(inRect.x + 20f, inRect.y + 60f, inRect.width - 40f, 34f);
            MD3Widgets.DrawRoundedRect(inputRect, MD3Theme.SurfaceContainerHigh, 6f);
            MD3Widgets.DrawRoundedRectOutline(inputRect, MD3Theme.Primary, 6f, 1.5f, MD3Theme.SurfaceContainerHigh);
            string controlName = "ModernExpandMenuRenameField";
            GUI.SetNextControlName(controlName);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            buffer = Widgets.TextField(inputRect.ContractedBy(6f), buffer);
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;
            // 打开对话框后自动聚焦输入框
            if (!focusedOnce)
            {
                GUI.FocusControl(controlName);
                focusedOnce = true;
            }

            // 扩展名提示（右侧）
            GUI.color = MD3Theme.OnSurfaceVariant;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inputRect.x, inputRect.y, inputRect.width, inputRect.height), ".xml");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // 底部按钮：取消 / 确定
            float buttonY = inRect.yMax - 52f;
            var cancelRect = new Rect(inRect.x + 20f, buttonY, 120f, 36f);
            var confirmRect = new Rect(inRect.xMax - 140f, buttonY, 120f, 36f);
            if (MD3Widgets.MD3Button(cancelRect, "Cancel".Translate(), emphasized: false))
            {
                Close();
            }
            if (MD3Widgets.MD3Button(confirmRect, "ModernExpandMenu_RenameConfigConfirm".Translate(), emphasized: true) || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                ApplyRename();
            }
        }

        /// <summary>校验并执行重命名（保留 .xml 扩展名）。</summary>
        private void ApplyRename()
        {
            string newBaseName = buffer.Trim();
            if (newBaseName.Length == 0)
            {
                ShowFeedback("ModernExpandMenu_RenameConfigInvalid".Translate());
                return;
            }
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (newBaseName.IndexOfAny(invalidChars) >= 0)
            {
                ShowFeedback("ModernExpandMenu_RenameConfigInvalid".Translate());
                return;
            }
            string directory = Path.GetDirectoryName(filePath);
            string newPath = Path.Combine(directory, newBaseName + ".xml");
            if (string.Equals(Path.GetFullPath(newPath), Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
            {
                // 名称未变，直接关闭
                Close();
                return;
            }
            if (File.Exists(newPath))
            {
                ShowFeedback("ModernExpandMenu_RenameConfigExists".Translate());
                return;
            }
            try
            {
                File.Move(filePath, newPath);
                onRenamed?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                Log.Warning("[ModernExpandMenu] 重命名配置失败：" + ex.Message);
                ShowFeedback("ModernExpandMenu_RenameConfigInvalid".Translate());
            }
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
