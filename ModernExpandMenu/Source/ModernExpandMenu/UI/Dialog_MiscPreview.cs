using ModernExpandMenu.Theme;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // 杂项样式独立预览窗口（不在设置菜单内）：
    // 展示"全局 MD3 替换功能"所使用的杂项控件样式（按钮 / 滑动开关 / 滑块 / 输入框 / 滚动条），
    // 全部使用杂项配色（MiscTheme），与扩展菜单配色独立。从设置界面"其他 → 预览"子 tab 的按钮打开。
    // ═══════════════════════════════════════════════════
    public class Dialog_MiscPreview : Window
    {
        // 预览交互状态
        private bool switchValue;
        private float sliderValue = 0.6f;
        private string inputText;
        private int scheduleSelectedIndex = 1;   // 管制栏（时间表）预览选中的档位
        private Vector2 scrollPosition;              // 主预览视口滚动位置
        private Vector2 scrollbarPreviewPosition;    // 滚动条演示视口滚动位置

        public override Vector2 InitialSize => new Vector2(720f, 680f);

        public Dialog_MiscPreview()
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
            // 整窗 MD3 表面背景（用杂项表面色）
            MD3Widgets.DrawCard(inRect, MiscTheme.Surface, MD3Theme.WindowCornerRadius);

            // 标题
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(inRect.x + 20f, inRect.y + 12f, inRect.width - 40f, 30f), "ModernExpandMenu_MiscPreviewTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 滚动视口（内容超高一屏，MD3 细滚动条）
            float contentWidth = inRect.width - 40f;
            float contentHeight = ComputeContentHeight(contentWidth);
            var viewRect = new Rect(inRect.x + 20f, inRect.y + 52f, contentWidth, inRect.height - 72f);
            var contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
            MD3Widgets.MD3BeginScrollView(viewRect, ref scrollPosition, contentRect);

            float x = 0f;
            float width = contentWidth;
            float y = 0f;

            // 1) 按钮区（普通 + 强调）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewButtons".Translate());
            y += 26f;
            float buttonWidth = (width - 12f) / 2f;
            if (MD3Widgets.MD3Button(new Rect(x, y, buttonWidth, 32f), "ModernExpandMenu_MiscPreviewButtonNormal".Translate(), false)) { }
            if (MD3Widgets.MD3Button(new Rect(x + buttonWidth + 12f, y, buttonWidth, 32f), "ModernExpandMenu_MiscPreviewButtonEmphasized".Translate(), true)) { }
            y += 44f;

            // 2) 滑动开关
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewSwitch".Translate());
            y += 26f;
            switchValue = MD3Widgets.MD3ToggleSwitch(new Rect(x, y, 120f, 28f), switchValue, 501, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh, MiscTheme.Surface);
            y += 40f;

            // 3) 滑块
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewSlider".Translate());
            y += 26f;
            sliderValue = MD3Widgets.MD3Slider(new Rect(x, y, width, 28f), sliderValue, 0f, 1f, 502, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh);
            y += 40f;

            // 4) 输入框
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewInput".Translate());
            y += 26f;
            inputText = MD3Widgets.MD3TextField(new Rect(x, y, width, 30f), inputText, 503, true, MiscTheme.Primary, MiscTheme.SurfaceContainerHigh, MiscTheme.OnSurface);
            y += 42f;

            // 5) 窗口边框（md3StyleWindows）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewWindows".Translate());
            y += 26f;
            y = DrawWindowBorderPreview(x, y, width) + 12f;

            // 6) 命令按钮（md3StyleCommands）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewCommands".Translate());
            y += 26f;
            y = DrawCommandPreview(x, y, width) + 12f;

            // 7) 菜单区块 / 列表行（md3StyleMenuSections）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewMenuSections".Translate());
            y += 26f;
            y = DrawMenuSectionPreview(x, y, width) + 12f;

            // 8) 管制栏（md3StyleSchedule）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewSchedule".Translate());
            y += 26f;
            y = DrawSchedulePreview(x, y, width) + 12f;

            // 9) 信息卡（md3StyleInspectPane）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewInspectPane".Translate());
            y += 26f;
            y = DrawInspectPanePreview(x, y, width) + 12f;

            // 10) 统计卡片（md3StyleStatistics）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewStatistics".Translate());
            y += 26f;
            y = DrawStatisticsPreview(x, y, width) + 12f;

            // 11) 文化菜单（md3StyleIdeo）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewIdeo".Translate());
            y += 26f;
            y = DrawIdeoPreview(x, y, width) + 12f;

            // 12) 滚动条区（模拟内容超高时的 MD3 细滚动条）
            DrawSectionLabel(x, y, "ModernExpandMenu_MiscPreviewScrollbar".Translate());
            y += 26f;
            DrawScrollbarPreview(x, y, width);

            MD3Widgets.MD3EndScrollView(viewRect, ref scrollPosition, contentHeight, 504, MD3Theme.CardCornerRadius);
        }

        /// <summary>估算预览内容总高度（滚动视口内容区用）。</summary>
        private static float ComputeContentHeight(float contentWidth)
        {
            // 各分区：标题(26) + 内容高 + 间隔(12)
            float h = 0f;
            h += 26f + 32f + 12f;                                          // 1 按钮
            h += 26f + 20f + 12f;                                          // 2 开关
            h += 26f + 20f + 12f;                                          // 3 滑块
            h += 26f + 30f + 12f;                                          // 4 输入框
            h += 26f + 130f + 12f;                                         // 5 窗口
            h += 26f + 75f + 12f;                                          // 6 命令
            h += 26f + 110f + 12f;                                         // 7 菜单区块
            h += 26f + 32f + 12f;                                          // 8 管制栏
            h += 26f + 120f + 12f;                                         // 9 信息卡
            h += 26f + 92f + 12f;                                          // 10 统计
            h += 26f + 152f + 12f;                                         // 11 文化
            h += 26f + 116f + 12f;                                         // 12 滚动条
            return h + 24f;
        }

        /// <summary>窗口边框预览：MD3 圆角卡片 + 主色描边（md3StyleWindows 效果）。</summary>
        private static float DrawWindowBorderPreview(float x, float y, float width)
        {
            var winRect = new Rect(x, y, Mathf.Min(280f, width), 130f);
            MD3Widgets.DrawRoundedRect(winRect, MiscTheme.Surface, 8f);
            MD3Widgets.DrawRoundedRectOutline(winRect, MiscTheme.Outline, 8f, 1.5f, MiscTheme.Surface);
            // 假标题栏
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(winRect.x + 12f, winRect.y + 8f, winRect.width - 24f, 20f), "ModernExpandMenu_MiscPreviewWindowSampleTitle".Translate());
            // 假内容区
            MD3Widgets.DrawRoundedRect(new Rect(winRect.x + 12f, winRect.y + 34f, winRect.width - 24f, winRect.height - 46f), MiscTheme.SurfaceContainer, 6f);
            GUI.color = MiscTheme.OnSurfaceVariant;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(winRect.x, winRect.y + 62f, winRect.width, 20f), "ModernExpandMenu_MiscPreviewWindowSample".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            return winRect.yMax;
        }

        /// <summary>命令按钮预览：一排 MD3 圆角命令按钮（md3StyleCommands 效果）。</summary>
        private static float DrawCommandPreview(float x, float y, float width)
        {
            const float size = 75f;
            const float gap = 8f;
            string[] labels =
            {
                "ModernExpandMenu_MiscPreviewCmdDraft".Translate(),
                "ModernExpandMenu_MiscPreviewCmdResign".Translate(),
                "ModernExpandMenu_MiscPreviewCmdAttack".Translate(),
                "ModernExpandMenu_MiscPreviewCmdPrioritize".Translate()
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var rect = new Rect(x + i * (size + gap), y, size, size);
                // MD3 圆角背景 + 细描边（同 Command.BGTexture 替换后的效果）
                MD3Widgets.DrawRoundedRect(rect, MiscTheme.SurfaceContainerHigh, 8f);
                MD3Widgets.DrawRoundedRectOutline(rect, MiscTheme.Outline, 8f, 1f, MiscTheme.SurfaceContainerHigh);
                if (Mouse.IsOver(rect))
                {
                    MD3Widgets.DrawHoverState(rect, 8f, MiscTheme.HoverStateLayer);
                }
                // 图标占位（主色小方块）
                MD3Widgets.DrawRoundedRect(new Rect(rect.x + (rect.width - 30f) / 2f, rect.y + 12f, 30f, 30f), MiscTheme.Primary, 8f);
                // 标签
                GUI.color = MiscTheme.OnSurface;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(new Rect(rect.x, rect.y + rect.height - 26f, rect.width, 20f), labels[i]);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
            }
            return y + size;
        }

        /// <summary>菜单区块 / 列表行预览：MD3 圆角区块 + 列表行（md3StyleMenuSections 效果）。</summary>
        private static float DrawMenuSectionPreview(float x, float y, float width)
        {
            var block = new Rect(x, y, Mathf.Min(380f, width), 110f);
            MD3Widgets.DrawRoundedRect(block, MiscTheme.SurfaceContainer, 6f);
            MD3Widgets.DrawRoundedRectOutline(block, MiscTheme.Outline, 6f, 1f, MiscTheme.SurfaceContainer);
            for (int i = 0; i < 3; i++)
            {
                var row = new Rect(block.x + 8f, block.y + 8f + i * 32f, block.width - 16f, 26f);
                MD3Widgets.DrawRoundedRect(row, MiscTheme.SurfaceContainerHigh, 4f);
                GUI.color = MiscTheme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Widgets.Label(new Rect(row.x + 8f, row.y, row.width - 16f, row.height), "ModernExpandMenu_MiscPreviewMenuRow".Translate(i + 1));
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
            }
            return block.yMax;
        }

        /// <summary>管制栏（时间表）预览：MD3 胶囊行，可点击切换（md3StyleSchedule 效果）。</summary>
        private float DrawSchedulePreview(float x, float y, float width)
        {
            string[] labels =
            {
                "Anything".Translate(),
                "Work".Translate(),
                "Joy".Translate(),
                "Sleep".Translate(),
                "Meditate".Translate()
            };
            const float gap = 6f;
            float bw = Mathf.Min(100f, (width - gap * (labels.Length - 1)) / labels.Length);
            for (int i = 0; i < labels.Length; i++)
            {
                var rect = new Rect(x + i * (bw + gap), y, bw, 32f);
                bool selected = scheduleSelectedIndex == i;
                MD3Widgets.DrawRoundedRect(rect, selected ? MiscTheme.Primary : MiscTheme.SurfaceContainerHigh, 6f);
                if (!selected && Mouse.IsOver(rect))
                {
                    MD3Widgets.DrawHoverState(rect, 6f, MiscTheme.HoverStateLayer);
                }
                GUI.color = selected ? MiscTheme.OnPrimary : MiscTheme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(rect, labels[i]);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(rect))
                {
                    scheduleSelectedIndex = i;
                }
            }
            return y + 32f;
        }

        /// <summary>信息卡预览：标题 + 状态条 + 文本卡片（md3StyleInspectPane 效果）。</summary>
        private static float DrawInspectPanePreview(float x, float y, float width)
        {
            var pane = new Rect(x, y, Mathf.Min(360f, width), 120f);
            MD3Widgets.DrawRoundedRect(pane, MiscTheme.SurfaceContainer, 8f);
            // 标题
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(pane.x + 12f, pane.y + 8f, pane.width - 24f, 20f), "ModernExpandMenu_MiscPreviewInspectTitle".Translate());
            // 状态条（MD3 圆角：轨道 + 主色填充）
            var barTrack = new Rect(pane.x + 12f, pane.y + 32f, pane.width - 24f, 8f);
            MD3Widgets.DrawRoundedRect(barTrack, MiscTheme.SurfaceContainerHigh, 4f);
            MD3Widgets.DrawRoundedRect(new Rect(barTrack.x, barTrack.y, barTrack.width * 0.72f, barTrack.height), MiscTheme.Primary, 4f);
            // 文本卡片（模拟 inspect string）
            MD3Widgets.DrawRoundedRect(new Rect(pane.x + 12f, pane.y + 48f, pane.width - 24f, pane.height - 60f), MiscTheme.Surface, 6f);
            GUI.color = MiscTheme.OnSurfaceVariant;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            Widgets.Label(new Rect(pane.x + 20f, pane.y + 54f, pane.width - 40f, pane.height - 66f), "ModernExpandMenu_MiscPreviewInspectText".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            return pane.yMax;
        }

        /// <summary>统计卡片预览：一张 MD3 分组卡片（md3StyleStatistics 效果）。</summary>
        private static float DrawStatisticsPreview(float x, float y, float width)
        {
            var card = new Rect(x, y, Mathf.Min(360f, width), 92f);
            MD3Widgets.DrawCard(card, MiscTheme.SurfaceContainer, 6f);
            GUI.color = MiscTheme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(card.x + 12f, card.y + 8f, card.width - 24f, 20f), "ModernExpandMenu_StatsGroupWealth".Translate());
            string[] lines =
            {
                "ThisMapColonyWealthTotal".Translate() + ": 486,300",
                "ThisMapColonyWealthItems".Translate() + ": 96,500",
                "ThisMapColonyWealthBuildings".Translate() + ": 220,800"
            };
            float ly = card.y + 32f;
            for (int i = 0; i < lines.Length; i++)
            {
                GUI.color = MiscTheme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = false;
                Widgets.Label(new Rect(card.x + 16f, ly, card.width - 32f, 20f), lines[i]);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
                ly += 20f;
            }
            return card.yMax;
        }

        /// <summary>文化菜单预览：文化行 + 模因大方块（md3StyleIdeo 效果）。</summary>
        private static float DrawIdeoPreview(float x, float y, float width)
        {
            // 文化行（选中态主色描边）
            var row = new Rect(x, y, Mathf.Min(360f, width), 40f);
            MD3Widgets.DrawRoundedRect(row, MiscTheme.SurfaceContainerHigh, 6f);
            MD3Widgets.DrawRoundedRectOutline(row, MiscTheme.Primary, 6f, 1.5f, MiscTheme.SurfaceContainerHigh);
            MD3Widgets.DrawRoundedRect(new Rect(row.x + 6f, row.y + 6f, 28f, 28f), MiscTheme.Primary, 6f);
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(row.x + 42f, row.y, row.width - 50f, row.height), "ModernExpandMenu_MiscPreviewIdeoName".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            // 模因大方块
            var meme = new Rect(row.x, row.yMax + 8f, 90f, 104f);
            MD3Widgets.DrawRoundedRect(meme, MiscTheme.SurfaceContainerHigh, 8f);
            MD3Widgets.DrawRoundedRectOutline(meme, MiscTheme.Outline, 8f, 1f, MiscTheme.SurfaceContainerHigh);
            MD3Widgets.DrawRoundedRect(new Rect(meme.x + (meme.width - 32f) / 2f, meme.y + 10f, 32f, 32f), MiscTheme.Primary, 8f);
            GUI.color = MiscTheme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            Widgets.Label(new Rect(meme.x, meme.yMax - 30f, meme.width, 20f), "ModernExpandMenu_MiscPreviewMemeName".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            return meme.yMax;
        }

        /// <summary>滚动条区预览：模拟内容超高时的 MD3 细滚动条（MD3Scrollbar 效果）。</summary>
        private void DrawScrollbarPreview(float x, float y, float width)
        {
            var viewRect = new Rect(x, y, Mathf.Min(380f, width), 116f);
            var contentRect = new Rect(0f, 0f, viewRect.width - 24f, 400f);
            MD3Widgets.MD3BeginScrollView(viewRect, ref scrollbarPreviewPosition, contentRect);
            for (int i = 0; i < 10; i++)
            {
                var itemRect = new Rect(10f, i * 38f, viewRect.width - 44f, 30f);
                MD3Widgets.DrawRoundedRect(itemRect, MiscTheme.SurfaceContainerHigh, 6f);
                GUI.color = MiscTheme.OnSurface;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Widgets.Label(new Rect(itemRect.x + 10f, itemRect.y, itemRect.width - 20f, itemRect.height), "ModernExpandMenu_MiscPreviewScrollItem".Translate(i + 1));
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
            }
            MD3Widgets.MD3EndScrollView(viewRect, ref scrollbarPreviewPosition, 400f, 505, MD3Theme.CardCornerRadius);
        }

        /// <summary>绘制分区小标题（杂项主色）。</summary>
        private static void DrawSectionLabel(float x, float y, string label)
        {
            GUI.color = MiscTheme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(x, y, 400f, 20f), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
        }
    }
}
