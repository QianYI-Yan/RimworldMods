using ModernExpandMenu.Theme;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // 扩展菜单独立预览窗口（不在设置菜单内）：
    // 独立弹出的可交互模拟右键分组菜单，实时反映扩展菜单配色与动画速度设置。
    // 从设置界面"扩展菜单 → 预览"子 tab 的按钮打开。
    // ═══════════════════════════════════════════════════
    public class Dialog_MenuPreview : Window
    {
        public override Vector2 InitialSize => new Vector2(720f, 580f);

        public Dialog_MenuPreview()
        {
            doWindowBackground = false;
            drawShadow = false;
            layer = WindowLayer.Dialog;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 整窗 MD3 表面背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 标题
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 12f, inRect.width - 40f, 30f), "ModernExpandMenu_MenuPreviewTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 可交互菜单预览（MD3 主题，模拟游戏操作：点击组标题展开/收起）
            var previewRect = new Rect(inRect.x + 20f, inRect.y + 52f, inRect.width - 40f, inRect.height - 72f);
            MenuPreviewWidget.Draw(previewRect);
        }
    }
}
