using System;
using ModernExpandMenu.Theme;
using ModernExpandMenu.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 设置界面（独立类）：从 ModernExpandMenuMod 分离，便于单独维护 / 重构 / 发送给
    // Gemini 等外部工具重写。
    //   入口：DrawSettings(inRect) —— 由 ModernExpandMenuMod.DoSettingsWindowContents 调用
    //   布局：顶部两大类 tab（扩展菜单 | 其他）+ 左侧 36% 实时预览区 + 右侧子 tab 内容滚动区
    //   状态：大类 / 子 tab / 滚动位置 / 滑块数值编辑输入态 全部集中在此类
    //   样式：MD3（MD3Theme 扩展菜单配色 / MiscTheme 杂项配色）+ 开关风格三切
    // ═══════════════════════════════════════════════════
    public static class SettingsUI
    {
        // 全局设置实例的便捷访问（原类内直接用 Settings，分离后经此属性）
        private static ModernExpandMenuSettings Settings => ModernExpandMenuMod.Settings;

        // 设置界面：当前正在编辑的数值滑块（点击数值框进入输入态）
        private static int editingSliderId = -1;
        private static string editingBuffer = "";
        private static Rect editingValueRect;
        private static int focusNumericFieldId = -1;   // 需要自动聚焦的数值输入框（进入编辑态后保证键盘可直接输入）

        // 设置界面：当前大类（0=扩展菜单，1=其他）与各类的子 tab
        private static int settingsCategory;
        private static int menuSubTab;      // 扩展菜单类子 tab：0=常规，1=动画，2=颜色，3=预览
        private static int miscSubTab;      // 其他类子 tab：0=全局样式，1=颜色，2=预览
        private static int previewTab;      // 左侧实时预览区页签：0=扩展菜单，1=杂项
        private static Vector2 settingsScrollPosition;   // 设置内容滚动位置（内容超出窗口高度时可滚动）

        /// <summary>设置界面主入口（两大类 + 子 tab，MD3 风格；左侧实时预览区 + 右侧设置内容）。</summary>
        public static void DrawSettings(Rect inRect)
        {
            // 整窗 MD3 表面背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // ── 顶部：两大类 tab（扩展菜单 | 其他）──
            const float categoryBarHeight = 36f;
            const float categoryGap = 8f;
            float categoryWidth = (inRect.width - 40f - categoryGap) / 2f;
            var categoryMenuRect = new Rect(inRect.x + 20f, inRect.y + 14f, categoryWidth, categoryBarHeight);
            var categoryMiscRect = new Rect(categoryMenuRect.xMax + categoryGap, inRect.y + 14f, categoryWidth, categoryBarHeight);
            if (MD3Widgets.MD3Button(categoryMenuRect, "ModernExpandMenu_SettingsCategoryMenu".Translate(), settingsCategory == 0))
            {
                settingsCategory = 0;
                previewTab = 0;   // 自动联动左侧预览为“扩展菜单”
                editingSliderId = -1;
            }
            if (MD3Widgets.MD3Button(categoryMiscRect, "ModernExpandMenu_SettingsCategoryMisc".Translate(), settingsCategory == 1))
            {
                settingsCategory = 1;
                previewTab = 1;   // 自动联动左侧预览为“杂项”
                editingSliderId = -1;
            }

            // ── 左侧实时预览区（独立区域，不单独开页面，实时联动当前配色 / 动画设置）──
            float previewWidth = inRect.width * 0.36f;
            var previewRect = new Rect(inRect.x + 20f, categoryMenuRect.yMax + 10f, previewWidth, inRect.yMax - categoryMenuRect.yMax - 24f);
            DrawLivePreview(previewRect);

            // ── 右侧设置区：子 tab 栏 + 滚动区 ──
            float contentX = previewRect.xMax + 12f;
            float contentWidth = inRect.xMax - contentX - 20f;
            float y = categoryMenuRect.yMax + 10f;

            // 子 tab 栏（扩展菜单类 4 个 / 其他类 3 个）
            const float subTabHeight = 32f;
            const float subTabGap = 6f;
            int subTabCount = settingsCategory == 0 ? 4 : 3;
            float subTabWidth = (contentWidth - subTabGap * (subTabCount - 1)) / subTabCount;
            DrawSubTabBar(contentX, y, subTabWidth, subTabHeight, subTabGap);
            float contentTop = y + subTabHeight + 12f;

            // ── 内容滚动区（各子 tab 内容可能超出窗口高度，包 MD3 滚动视口）──
            float contentTotal = ComputeSettingsContentHeight(contentWidth, settingsCategory);
            var scrollRect = new Rect(contentX, contentTop, contentWidth, inRect.yMax - contentTop - 12f);
            var contentRect = new Rect(0f, 0f, contentWidth, contentTotal);
            MD3Widgets.MD3BeginScrollView(scrollRect, ref settingsScrollPosition, contentRect);

            // 点击编辑框外部时退出输入态（未回车视为取消；视口局部坐标）
            if (editingSliderId >= 0 && Event.current.type == EventType.MouseDown && !Mouse.IsOver(editingValueRect))
            {
                editingSliderId = -1;
            }

            // 各子 tab 内容从视口局部 y=0 开始绘制
            if (settingsCategory == 0)
            {
                DrawMenuCategoryContent(0f, contentWidth, 0f);
            }
            else
            {
                DrawMiscCategoryContent(0f, contentWidth, 0f);
            }

            MD3Widgets.MD3EndScrollView(scrollRect, ref settingsScrollPosition, contentTotal, 3000, MD3Theme.CardCornerRadius);

            Settings.Write();
        }

        /// <summary>
        /// 左侧实时预览区：标题 + 两个页签（扩展菜单预览 / 杂项预览）。
        /// 扩展菜单预览 = 可交互模拟右键分组菜单（实时反映扩展菜单配色与动画速度）；
        /// 杂项预览 = 模拟原版操作界面被 MD3 化后的样子（实时反映杂项配色）。
        /// </summary>
        private static void DrawLivePreview(Rect rect)
        {
            // 标题
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "ModernExpandMenu_LivePreviewTitle".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 两个页签（扩展菜单 | 杂项）
            const float tabHeight = 26f;
            const float tabGap = 6f;
            float tabWidth = (rect.width - tabGap) / 2f;
            var tabMenu = new Rect(rect.x, rect.y + 24f, tabWidth, tabHeight);
            var tabMisc = new Rect(tabMenu.xMax + tabGap, rect.y + 24f, tabWidth, tabHeight);
            if (MD3Widgets.MD3Button(tabMenu, "ModernExpandMenu_PreviewMenuTab".Translate(), previewTab == 0))
            {
                previewTab = 0;
            }
            if (MD3Widgets.MD3Button(tabMisc, "ModernExpandMenu_PreviewMiscTab".Translate(), previewTab == 1))
            {
                previewTab = 1;
            }

            // 预览内容（菜单预览或杂项预览，实时读取对应配色 / 动画设置）
            var contentRect = new Rect(rect.x, rect.y + 24f + tabHeight + 10f, rect.width, rect.yMax - (rect.y + 24f + tabHeight + 10f));
            if (previewTab == 0)
            {
                MenuPreviewWidget.Draw(contentRect);
            }
            else
            {
                MiscPreviewWidget.Draw(contentRect);
            }
        }

        /// <summary>绘制子 tab 栏（按大类显示对应的子 tab 名称）。</summary>
        private static void DrawSubTabBar(float x, float y, float subTabWidth, float subTabHeight, float gap)
        {
            if (settingsCategory == 0)
            {
                // 扩展菜单类：常规 / 动画 / 颜色 / 预览
                string[] labels =
                {
                    "ModernExpandMenu_TabGeneral".Translate(),
                    "ModernExpandMenu_TabAnimations".Translate(),
                    "ModernExpandMenu_TabColors".Translate(),
                    "ModernExpandMenu_SubTabPreview".Translate()
                };
                for (int i = 0; i < labels.Length; i++)
                {
                    var rect = new Rect(x + (subTabWidth + gap) * i, y, subTabWidth, subTabHeight);
                    if (MD3Widgets.MD3Button(rect, labels[i], menuSubTab == i))
                    {
                        menuSubTab = i;
                        editingSliderId = -1;
                    }
                }
            }
            else
            {
                // 其他类：全局样式 / 颜色 / 预览
                string[] labels =
                {
                    "ModernExpandMenu_MiscGlobalStyle".Translate(),
                    "ModernExpandMenu_TabColors".Translate(),
                    "ModernExpandMenu_SubTabPreview".Translate()
                };
                for (int i = 0; i < labels.Length; i++)
                {
                    var rect = new Rect(x + (subTabWidth + gap) * i, y, subTabWidth, subTabHeight);
                    if (MD3Widgets.MD3Button(rect, labels[i], miscSubTab == i))
                    {
                        miscSubTab = i;
                        editingSliderId = -1;
                    }
                }
            }
        }

        /// <summary>扩展菜单类：按子 tab 分发内容。</summary>
        private static void DrawMenuCategoryContent(float contentX, float contentWidth, float y)
        {
            switch (menuSubTab)
            {
                case 1: DrawAnimationTab(contentX, contentWidth, y); break;
                case 2: DrawColorTab(contentX, contentWidth, y, miscPalette: false); break;
                case 3: DrawMenuPreviewTab(contentX, contentWidth, y); break;
                default: DrawGeneralTab(contentX, contentWidth, y); break;
            }
        }

        /// <summary>其他类：按子 tab 分发内容。</summary>
        private static void DrawMiscCategoryContent(float contentX, float contentWidth, float y)
        {
            switch (miscSubTab)
            {
                case 1: DrawColorTab(contentX, contentWidth, y, miscPalette: true); break;
                case 2: DrawMiscPreviewTab(contentX, contentWidth, y); break;
                default: DrawGlobalStyleTab(contentX, contentWidth, y); break;
            }
        }

        /// <summary>扩展菜单类 → 预览子 tab：打开独立预览窗口（不在设置菜单内）。</summary>
        private static void DrawMenuPreviewTab(float contentX, float contentWidth, float y)
        {
            const float rowHeight = 34f;
            float height = 30f + rowHeight + 12f;
            var card = new Rect(contentX, y, contentWidth, height);
            MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(card, "ModernExpandMenu_SubTabPreview".Translate());
            var openRect = new Rect(card.x + 14f, card.y + 40f, Mathf.Min(300f, card.width - 28f), rowHeight);
            if (MD3Widgets.MD3Button(openRect, "ModernExpandMenu_OpenMenuPreviewWindow".Translate(), true))
            {
                Find.WindowStack.Add(new Dialog_MenuPreview());
            }
        }

        /// <summary>其他类 → 预览子 tab：打开杂项样式独立预览窗口（不在设置菜单内）。</summary>
        private static void DrawMiscPreviewTab(float contentX, float contentWidth, float y)
        {
            const float rowHeight = 34f;
            float height = 30f + rowHeight + 12f;
            var card = new Rect(contentX, y, contentWidth, height);
            MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(card, "ModernExpandMenu_SubTabPreview".Translate());
            var openRect = new Rect(card.x + 14f, card.y + 40f, Mathf.Min(300f, card.width - 28f), rowHeight);
            if (MD3Widgets.MD3Button(openRect, "ModernExpandMenu_OpenMiscPreviewWindow".Translate(), true))
            {
                Find.WindowStack.Add(new Dialog_MiscPreview());
            }
        }

        /// <summary>估算各子 tab 内容总高度（用于滚动视口内容区高度，取当前大类下各子 tab 的最大值）。</summary>
        private static float ComputeSettingsContentHeight(float contentWidth, int category)
        {
            float cbRow = CheckboxRowHeight;   // 赛博模式开关行为 56px 完整卡片
            if (category == 1) // 其他类：全局样式 / 颜色 / 预览
            {
                // 全局样式卡：三个分段选择器 + 14 行开关
                float globalStyleHeight = (30f + 46f * 3f + cbRow * 14f + 12f) + 42f;
                float colorSubHeight = ComputeColorSettingsHeight(contentWidth) + 42f;
                float miscPreviewHeight = (30f + 34f + 12f) + 42f;
                return Mathf.Max(Mathf.Max(globalStyleHeight, colorSubHeight), miscPreviewHeight) + 48f;
            }
            // 扩展菜单类：常规 / 动画 / 颜色 / 预览
            float appearance = 30f + cbRow * 5f + 12f;
            float performance = 30f + 30f * 2f + 12f;
            float loading = 30f + cbRow + 30f * 2f + 12f;
            float general = appearance + performance + loading + (34f + 12f) + (30f + 30f + 12f) + 48f;
            float animation = (30f + cbRow + 12f) + (30f + 46f + 30f + 12f) + (30f + 30f * 8f + 12f) + 48f;
            float color = ComputeColorSettingsHeight(contentWidth) + 48f;
            float menuPreviewHeight = (30f + 34f + 12f) + 48f;
            return Mathf.Max(Mathf.Max(general, animation), Mathf.Max(color, menuPreviewHeight));
        }

        /// <summary>常规 tab：外观 / 性能 / 加载动画 / 恢复默认。</summary>
        private static void DrawGeneralTab(float contentX, float contentWidth, float y)
        {
            float cbRow = CheckboxRowHeight;   // 赛博模式开关行为 56px 完整卡片
            const float sliderRow = 30f;

            // ===== 外观卡片 =====
            float appearanceHeight = 30f + cbRow * 5f + 12f;
            var appearanceCard = new Rect(contentX, y, contentWidth, appearanceHeight);
            MD3Widgets.DrawCard(appearanceCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(appearanceCard, "ModernExpandMenu_SectionAppearance".Translate());
            float cy = appearanceCard.y + 34f;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ModEnabled".Translate(), ref Settings.modEnabled, 0); cy += cbRow;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_DisablePseudoTranslation".Translate(), ref Settings.disableDevPseudoTranslation, 4); cy += cbRow;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowLoadingAnimation".Translate(), ref Settings.showLoadingAnimation, 1); cy += cbRow;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowHoverHighlight".Translate(), ref Settings.showHoverHighlightAndArrow, 2); cy += cbRow;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowItemCount".Translate(), ref Settings.showItemCount, 3); cy += cbRow;
            y = appearanceCard.yMax + 12f;

            // ===== 性能卡片 =====
            float performanceHeight = 30f + sliderRow * 2f + 12f;
            var performanceCard = new Rect(contentX, y, contentWidth, performanceHeight);
            MD3Widgets.DrawCard(performanceCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(performanceCard, "ModernExpandMenu_SectionPerformance".Translate());
            cy = performanceCard.y + 34f;
            float maxHeightValue = Settings.maxMenuHeight;
            DrawSliderRow(performanceCard, cy, "ModernExpandMenu_MaxMenuHeight".Translate(), ref maxHeightValue, 300f, 900f, 0, "0");
            Settings.maxMenuHeight = Mathf.RoundToInt(maxHeightValue);
            cy += sliderRow;
            float perFrameValue = Settings.maxProcessedPerFrame;
            DrawSliderRow(performanceCard, cy, "ModernExpandMenu_MaxProcessedPerFrame".Translate(), ref perFrameValue, 2f, 30f, 1, "0");
            Settings.maxProcessedPerFrame = Mathf.RoundToInt(perFrameValue);
            cy += sliderRow;
            y = performanceCard.yMax + 12f;

            // ===== 加载动画卡片 =====
            float loadingHeight = 30f + cbRow + sliderRow * 2f + 12f;
            var loadingCard = new Rect(contentX, y, contentWidth, loadingHeight);
            MD3Widgets.DrawCard(loadingCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(loadingCard, "ModernExpandMenu_SectionLoading".Translate());
            cy = loadingCard.y + 34f;
            DrawCheckboxRow(loadingCard, cy, "ModernExpandMenu_LoadingBarMarquee".Translate(), ref Settings.loadingBarMarquee, 40, "ModernExpandMenu_LoadingBarMarqueeDesc".Translate()); cy += cbRow;
            float extraSecondsValue = Settings.extraLoadingBarSeconds;
            DrawSliderRow(loadingCard, cy, "ModernExpandMenu_ExtraLoadingBarSeconds".Translate(), ref extraSecondsValue, 0f, 5f, 2, "0.0");
            Settings.extraLoadingBarSeconds = extraSecondsValue;
            cy += sliderRow;
            float maskOpacityValue = Settings.loadingMaskOpacity;
            DrawSliderRow(loadingCard, cy, "ModernExpandMenu_LoadingMaskOpacity".Translate(), ref maskOpacityValue, 0f, 0.6f, 18, "0.00");
            Settings.loadingMaskOpacity = maskOpacityValue;
            cy += sliderRow;
            y = loadingCard.yMax + 12f;

            // ===== 恢复默认设置（差异对比对话框，可勾选要重置的项）=====
            var resetRect = new Rect(contentX, y, contentWidth, 34f);
            if (MD3Widgets.MD3Button(resetRect, "ModernExpandMenu_ResetDefaults".Translate(), emphasized: false))
            {
                Find.WindowStack.Add(new Dialog_ResetDefaults());
            }
            y = resetRect.yMax + 12f;

            // ===== 配置分享卡片（导出/导入到独立文件或剪贴板）=====
            const float shareRowHeight = 30f;
            float shareHeight = 30f + shareRowHeight + 12f;
            var shareCard = new Rect(contentX, y, contentWidth, shareHeight);
            MD3Widgets.DrawCard(shareCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(shareCard, "ModernExpandMenu_SectionShare".Translate());
            float shareButtonY = shareCard.y + 38f;
            float shareButtonWidth = (contentWidth - 28f - 16f) / 5f;
            var exportFileRect = new Rect(shareCard.x + 14f, shareButtonY, shareButtonWidth, shareRowHeight);
            var copyRect = new Rect(shareCard.x + 14f + shareButtonWidth + 4f, shareButtonY, shareButtonWidth, shareRowHeight);
            var importClipboardRect = new Rect(shareCard.x + 14f + (shareButtonWidth + 4f) * 2f, shareButtonY, shareButtonWidth, shareRowHeight);
            var importFileRect = new Rect(shareCard.x + 14f + (shareButtonWidth + 4f) * 3f, shareButtonY, shareButtonWidth, shareRowHeight);
            var manageRect = new Rect(shareCard.x + 14f + (shareButtonWidth + 4f) * 4f, shareButtonY, shareButtonWidth, shareRowHeight);
            if (MD3Widgets.MD3Button(exportFileRect, "ModernExpandMenu_ExportToFile".Translate(), emphasized: false))
            {
                string path = SettingsShare.SaveToFile();
                ShowShareFeedback("ModernExpandMenu_ShareExported".Translate(path), force: true);
            }
            if (MD3Widgets.MD3Button(copyRect, "ModernExpandMenu_CopyConfig".Translate(), emphasized: false))
            {
                GUIUtility.systemCopyBuffer = SettingsShare.ExportToString();
                ShowShareFeedback("ModernExpandMenu_ShareCopied".Translate(), force: true);
            }
            if (MD3Widgets.MD3Button(importClipboardRect, "ModernExpandMenu_ImportClipboard".Translate(), emphasized: false))
            {
                bool ok = SettingsShare.ImportFromString(GUIUtility.systemCopyBuffer);
                ShowShareFeedback(ok ? "ModernExpandMenu_ShareImported".Translate() : "ModernExpandMenu_ShareImportFailed".Translate(), force: true);
            }
            if (MD3Widgets.MD3Button(importFileRect, "ModernExpandMenu_ImportFromFile".Translate(), emphasized: false))
            {
                string content = SettingsShare.LoadLatestFileContent();
                bool ok = content != null && SettingsShare.ImportFromString(content);
                ShowShareFeedback(ok ? "ModernExpandMenu_ShareImported".Translate() : "ModernExpandMenu_ShareImportFailed".Translate(), force: true);
            }
            if (MD3Widgets.MD3Button(manageRect, "ModernExpandMenu_ManageConfigs".Translate(), emphasized: false))
            {
                Find.WindowStack.Add(new Dialog_ConfigManager());
            }
        }

        /// <summary>显示配置分享操作反馈（无地图时退化为日志）。</summary>
        private static void ShowShareFeedback(string text, bool force)
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

        /// <summary>其他类 → 全局样式：把原版输入框 / 按钮 / 复选框 / tab / 滚动条等全局替换为 MD3 样式的开关（与右键菜单无关）。</summary>
        private static void DrawGlobalStyleTab(float contentX, float contentWidth, float y)
        {
            float cbRow = CheckboxRowHeight;   // 赛博模式开关行为 56px 完整卡片
            const float segmentRowHeight = 46f;   // 分段选择器行（比普通开关行高）
            // 布局：开关风格选择器 + 展开菜单接管范围选择器 + 边框样式选择器 + 14 行开关（12 个原版 MD3 开关 + 医药 + 强制上传）
            float styleHeight = 30f + segmentRowHeight * 3f + cbRow * 14f + 12f;
            var styleCard = new Rect(contentX, y, contentWidth, styleHeight);
            MD3Widgets.DrawCard(styleCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(styleCard, "ModernExpandMenu_MiscGlobalStyle".Translate());
            float cy = styleCard.y + 34f;

            // 开关风格：三段选择器（原版 / 滑动开关 / 赛博炫酷）
            // 切换到赛博炫酷时，选择器自身也变为赛博风格（流光边框 + 网格 + 发光指示器）
            var segmentRect = new Rect(styleCard.x + 14f, cy + 6f, styleCard.width - 28f, 34f);
            string[] switchStyleLabels =
            {
                "ModernExpandMenu_SwitchStyleVanilla".Translate(),
                "ModernExpandMenu_SwitchStyleSlider".Translate(),
                "ModernExpandMenu_SwitchStyleCyber".Translate()
            };
            bool cyberSegment = Settings.switchStyle == ModernExpandMenuSettings.SwitchStyle.Cyber;
            int newStyleIndex = MD3Widgets.MD3SegmentedControl(segmentRect, (int)Settings.switchStyle, switchStyleLabels, 1000, cyberSegment);
            if (newStyleIndex != (int)Settings.switchStyle)
            {
                Settings.switchStyle = (ModernExpandMenuSettings.SwitchStyle)newStyleIndex;
            }
            TooltipHandler.TipRegion(segmentRect, "ModernExpandMenu_SwitchStyleDesc".Translate());
            cy += segmentRowHeight;

            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleAllInputs".Translate(), ref Settings.md3StyleAllInputs, 21, "ModernExpandMenu_Md3StyleAllInputsDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleAllButtons".Translate(), ref Settings.md3StyleAllButtons, 22, "ModernExpandMenu_Md3StyleAllButtonsDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_SkipUploadWait".Translate(), ref Settings.skipUploadWait, 23, "ModernExpandMenu_SkipUploadWaitDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_SpaceOnlyPauses".Translate(), ref Settings.spaceOnlyPauses, 24, "ModernExpandMenu_SpaceOnlyPausesDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleWindows".Translate(), ref Settings.md3StyleWindows, 25, "ModernExpandMenu_Md3StyleWindowsDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleCommands".Translate(), ref Settings.md3StyleCommands, 26, "ModernExpandMenu_Md3StyleCommandsDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleMenuSections".Translate(), ref Settings.md3StyleMenuSections, 27, "ModernExpandMenu_Md3StyleMenuSectionsDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleSchedule".Translate(), ref Settings.md3StyleSchedule, 28, "ModernExpandMenu_Md3StyleScheduleDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleInspectPane".Translate(), ref Settings.md3StyleInspectPane, 29, "ModernExpandMenu_Md3StyleInspectPaneDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleStatistics".Translate(), ref Settings.md3StyleStatistics, 30, "ModernExpandMenu_Md3StyleStatisticsDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleIdeo".Translate(), ref Settings.md3StyleIdeo, 31, "ModernExpandMenu_Md3StyleIdeoDesc".Translate()); cy += cbRow;

            // 展开菜单 MD3 接管：介入范围（全部 / 仅对话框下拉 / 关闭）
            var scopeRect = new Rect(styleCard.x + 14f, cy, styleCard.width - 28f, 34f);
            string[] scopeLabels =
            {
                "ModernExpandMenu_ScopeAll".Translate(),
                "ModernExpandMenu_ScopeDialogDropdowns".Translate(),
                "ModernExpandMenu_ScopeOff".Translate()
            };
            int newScopeIndex = MD3Widgets.MD3SegmentedControl(scopeRect, (int)Settings.floatMenuTakeoverScope, scopeLabels, 1001, cyberSegment);
            if (newScopeIndex != (int)Settings.floatMenuTakeoverScope)
            {
                Settings.floatMenuTakeoverScope = (ModernExpandMenuSettings.FloatMenuTakeoverScope)newScopeIndex;
            }
            TooltipHandler.TipRegion(scopeRect, "ModernExpandMenu_ScopeDesc".Translate());
            cy += segmentRowHeight;

            // 菜单边框样式（普通描边 / 主色跑马灯 / 彩色流光；与右键菜单共用同一种）
            var borderRect = new Rect(styleCard.x + 14f, cy + 6f, styleCard.width - 28f, 34f);
            string[] borderLabels =
            {
                "ModernExpandMenu_BorderOutline".Translate(),
                "ModernExpandMenu_BorderMarquee".Translate(),
                "ModernExpandMenu_BorderRainbow".Translate()
            };
            int newBorder = MD3Widgets.MD3SegmentedControl(borderRect, (int)Settings.menuBorderStyle, borderLabels, 1003, cyberSegment);
            if (newBorder != (int)Settings.menuBorderStyle)
            {
                Settings.menuBorderStyle = (ModernExpandMenuSettings.MenuBorderStyle)newBorder;
            }
            TooltipHandler.TipRegion(borderRect, "ModernExpandMenu_BorderStyleDesc".Translate());
            cy += segmentRowHeight;

            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleMedicalCare".Translate(), ref Settings.md3StyleMedicalCare, 35, "ModernExpandMenu_Md3StyleMedicalCareDesc".Translate()); cy += cbRow;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_ForceWorkshopUpload".Translate(), ref Settings.forceWorkshopUpload, 32, "ModernExpandMenu_ForceWorkshopUploadDesc".Translate()); cy += cbRow;
        }

        /// <summary>动画 tab：速度控制模式（整体倍率 / 手动自定义互斥）+ 动画速度设置 + 实时预览区域。</summary>
        private static void DrawAnimationTab(float contentX, float contentWidth, float y)
        {
            float cbRow = CheckboxRowHeight;   // 赛博模式总开关行为 56px 完整卡片
            const float sliderRow = 30f;
            const float segmentRowHeight = 46f;
            bool cyberSegment = Settings.switchStyle == ModernExpandMenuSettings.SwitchStyle.Cyber;

            // ===== 动画总开关卡片（关闭后停用所有动画效果）=====
            float masterHeight = 30f + cbRow + 12f;
            var masterCard = new Rect(contentX, y, contentWidth, masterHeight);
            MD3Widgets.DrawCard(masterCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(masterCard, "ModernExpandMenu_SectionAnimationMaster".Translate());
            DrawCheckboxRow(masterCard, masterCard.y + 34f, "ModernExpandMenu_EnableAnimations".Translate(), ref Settings.enableAnimations, 20);
            y = masterCard.yMax + 12f;

            // ===== 动画速度模式卡片（倍率 / 自定义切换 + 全局倍率滑块）=====
            float modeHeight = 30f + segmentRowHeight + sliderRow + 12f;
            var modeCard = new Rect(contentX, y, contentWidth, modeHeight);
            MD3Widgets.DrawCard(modeCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(modeCard, "ModernExpandMenu_SectionAnimationSpeedMode".Translate());
            float cy = modeCard.y + 34f;

            // 模式切换：倍率模式（整体倍率，下方单项灰显禁用）/ 自定义模式（单项可调，倍率灰显禁用）
            var modeRect = new Rect(modeCard.x + 14f, cy + 6f, modeCard.width - 28f, 34f);
            string[] modeLabels =
            {
                "ModernExpandMenu_SpeedModeMultiplier".Translate(),
                "ModernExpandMenu_SpeedModeCustom".Translate()
            };
            int newMode = MD3Widgets.MD3SegmentedControl(modeRect, (int)Settings.animationSpeedMode, modeLabels, 1002, cyberSegment);
            if (newMode != (int)Settings.animationSpeedMode)
            {
                Settings.animationSpeedMode = (ModernExpandMenuSettings.AnimationSpeedMode)newMode;
            }
            TooltipHandler.TipRegion(modeRect, "ModernExpandMenu_SpeedModeDesc".Translate());
            cy += segmentRowHeight;

            // 全局倍率滑块：自定义模式下禁用（灰显）
            bool multiplierActive = Settings.animationSpeedMode == ModernExpandMenuSettings.AnimationSpeedMode.Multiplier;
            float multiplier = Settings.animationSpeedMultiplier;
            DrawSliderRow(modeCard, cy, "ModernExpandMenu_AnimationSpeedMultiplier".Translate(), ref multiplier, 0.2f, 3f, 17, "0.0x", disabled: !multiplierActive);
            Settings.animationSpeedMultiplier = multiplier;
            y = modeCard.yMax + 12f;

            // ===== 动画速度自定义卡片（8 行滑块；倍率模式下全部灰显禁用）=====
            bool customActive = Settings.animationSpeedMode == ModernExpandMenuSettings.AnimationSpeedMode.Custom;
            float speedHeight = 30f + sliderRow * 8f + 12f;
            var speedCard = new Rect(contentX, y, contentWidth, speedHeight);
            MD3Widgets.DrawCard(speedCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(speedCard, "ModernExpandMenu_SectionAnimation".Translate());
            cy = speedCard.y + 34f;
            float appearDuration = Settings.itemAppearDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ItemAppearDuration".Translate(), ref appearDuration, 0.05f, 1f, 10, "0.00", disabled: !customActive); cy += sliderRow;
            Settings.itemAppearDuration = appearDuration;
            float appearInterval = Settings.itemAppearInterval;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ItemAppearInterval".Translate(), ref appearInterval, 0f, 0.5f, 11, "0.00", disabled: !customActive); cy += sliderRow;
            Settings.itemAppearInterval = appearInterval;
            float popDuration = Settings.popAnimationDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_PopAnimationDuration".Translate(), ref popDuration, 0.05f, 1f, 12, "0.00", disabled: !customActive); cy += sliderRow;
            Settings.popAnimationDuration = popDuration;
            float expandSpeed = Settings.expandAnimationSpeed;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ExpandAnimationSpeed".Translate(), ref expandSpeed, 1f, 20f, 13, "0.0", disabled: !customActive); cy += sliderRow;
            Settings.expandAnimationSpeed = expandSpeed;
            float followDuration = Settings.scrollFollowDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ScrollFollowDuration".Translate(), ref followDuration, 0.05f, 2f, 14, "0.00", disabled: !customActive); cy += sliderRow;
            Settings.scrollFollowDuration = followDuration;
            float returnDuration = Settings.scrollReturnDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ScrollReturnDuration".Translate(), ref returnDuration, 0.2f, 2f, 15, "0.00", disabled: !customActive); cy += sliderRow;
            Settings.scrollReturnDuration = returnDuration;
            float heightSpeed = Settings.windowHeightAnimationSpeed;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_WindowHeightAnimationSpeed".Translate(), ref heightSpeed, 30f, 400f, 16, "0", disabled: !customActive); cy += sliderRow;
            Settings.windowHeightAnimationSpeed = heightSpeed;
            float returnWait = Settings.scrollReturnWaitSeconds;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ScrollReturnWaitSeconds".Translate(), ref returnWait, 0f, 2f, 19, "0.00", disabled: !customActive); cy += sliderRow;
            Settings.scrollReturnWaitSeconds = returnWait;
        }

        /// <summary>恢复默认颜色（扩展菜单配色 + 杂项配色，16 进制，带 # 前缀）。</summary>
        private static void ResetColorSettings()
        {
            Settings.colorPrimary = "#00A8FF";
            Settings.colorOnPrimary = "#001421";
            Settings.colorSurface = "#161821";
            Settings.colorSurfaceContainer = "#1E212D";
            Settings.colorSurfaceContainerHigh = "#262A3A";
            Settings.colorOnSurface = "#E6E6EC";
            Settings.colorOnSurfaceVariant = "#9A9BA6";
            Settings.colorOutline = "#636676";
            Settings.colorDisabledText = "#80808C";
            Settings.colorShadow = "#000000";
            Settings.colorScrollbarTrack = "#26262E";
            Settings.colorScrollbarThumb = "#525261";
            Settings.colorScrollbarThumbDragging = "#737385";
            Settings.miscColorPrimary = "#00A8FF";
            Settings.miscColorOnPrimary = "#001421";
            Settings.miscColorSurface = "#161821";
            Settings.miscColorSurfaceContainer = "#1E212D";
            Settings.miscColorSurfaceContainerHigh = "#262A3A";
            Settings.miscColorOnSurface = "#E6E6EC";
            Settings.miscColorOnSurfaceVariant = "#9A9BA6";
            Settings.miscColorOutline = "#636676";
            Settings.miscColorDisabledText = "#80808C";
            Settings.miscColorShadow = "#000000";
            Settings.miscColorScrollbarTrack = "#26262E";
            Settings.miscColorScrollbarThumb = "#525261";
            Settings.miscColorScrollbarThumbDragging = "#737385";
        }

        /// <summary>读取当前配色的 13 个 16 进制值（miscPalette=true 读杂项配色，否则读扩展菜单配色）。</summary>
        private static string[] GetColorValues(bool miscPalette)
        {
            return miscPalette
                ? new[] { Settings.miscColorPrimary, Settings.miscColorOnPrimary, Settings.miscColorSurface, Settings.miscColorSurfaceContainer, Settings.miscColorSurfaceContainerHigh, Settings.miscColorOnSurface, Settings.miscColorOnSurfaceVariant, Settings.miscColorOutline, Settings.miscColorDisabledText, Settings.miscColorShadow, Settings.miscColorScrollbarTrack, Settings.miscColorScrollbarThumb, Settings.miscColorScrollbarThumbDragging }
                : new[] { Settings.colorPrimary, Settings.colorOnPrimary, Settings.colorSurface, Settings.colorSurfaceContainer, Settings.colorSurfaceContainerHigh, Settings.colorOnSurface, Settings.colorOnSurfaceVariant, Settings.colorOutline, Settings.colorDisabledText, Settings.colorShadow, Settings.colorScrollbarTrack, Settings.colorScrollbarThumb, Settings.colorScrollbarThumbDragging };
        }

        /// <summary>把 13 个 16 进制值写回当前配色的设置字段。</summary>
        private static void SetColorValues(bool miscPalette, string[] v)
        {
            if (miscPalette)
            {
                Settings.miscColorPrimary = v[0]; Settings.miscColorOnPrimary = v[1]; Settings.miscColorSurface = v[2];
                Settings.miscColorSurfaceContainer = v[3]; Settings.miscColorSurfaceContainerHigh = v[4]; Settings.miscColorOnSurface = v[5];
                Settings.miscColorOnSurfaceVariant = v[6]; Settings.miscColorOutline = v[7]; Settings.miscColorDisabledText = v[8];
                Settings.miscColorShadow = v[9]; Settings.miscColorScrollbarTrack = v[10]; Settings.miscColorScrollbarThumb = v[11];
                Settings.miscColorScrollbarThumbDragging = v[12];
            }
            else
            {
                Settings.colorPrimary = v[0]; Settings.colorOnPrimary = v[1]; Settings.colorSurface = v[2];
                Settings.colorSurfaceContainer = v[3]; Settings.colorSurfaceContainerHigh = v[4]; Settings.colorOnSurface = v[5];
                Settings.colorOnSurfaceVariant = v[6]; Settings.colorOutline = v[7]; Settings.colorDisabledText = v[8];
                Settings.colorShadow = v[9]; Settings.colorScrollbarTrack = v[10]; Settings.colorScrollbarThumb = v[11];
                Settings.colorScrollbarThumbDragging = v[12];
            }
        }

        /// <summary>读取当前配色的 13 个预览色（色块用，杂项配色用 MiscTheme）。</summary>
        private static Color[] GetColorPreviews(bool miscPalette)
        {
            return miscPalette
                ? new[] { MiscTheme.Primary, MiscTheme.OnPrimary, MiscTheme.Surface, MiscTheme.SurfaceContainer, MiscTheme.SurfaceContainerHigh, MiscTheme.OnSurface, MiscTheme.OnSurfaceVariant, MiscTheme.Outline, MiscTheme.DisabledText, MiscTheme.Shadow, MiscTheme.ScrollbarTrack, MiscTheme.ScrollbarThumb, MiscTheme.ScrollbarThumbDragging }
                : new[] { MD3Theme.Primary, MD3Theme.OnPrimary, MD3Theme.Surface, MD3Theme.SurfaceContainer, MD3Theme.SurfaceContainerHigh, MD3Theme.OnSurface, MD3Theme.OnSurfaceVariant, MD3Theme.Outline, MD3Theme.DisabledText, MD3Theme.Shadow, MD3Theme.ScrollbarTrack, MD3Theme.ScrollbarThumb, MD3Theme.ScrollbarThumbDragging };
        }

        /// <summary>颜色子 tab：调色板 + 按分类卡片列出所有可自定义颜色（16 进制输入）。
        /// miscPalette=true 编辑杂项配色（全局 MD3 替换功能用），否则编辑扩展菜单配色。</summary>
        private static void DrawColorTab(float contentX, float contentWidth, float y, bool miscPalette)
        {
            string[] values = GetColorValues(miscPalette);
            Color[] previews = GetColorPreviews(miscPalette);

            float sy = y;
            sy = DrawPaletteCard(contentX, sy, contentWidth, miscPalette, values);

            // 主色
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorPrimary".Translate(), 2, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorPrimary".Translate(), values, 0, MD3Theme.DefaultPrimary, previews[0]);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorOnPrimary".Translate(), values, 1, MD3Theme.DefaultOnPrimary, previews[1]);
            });

            // 表面
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorSurface".Translate(), 3, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorSurface".Translate(), values, 2, MD3Theme.DefaultSurface, previews[2]);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorSurfaceContainer".Translate(), values, 3, MD3Theme.DefaultSurfaceContainer, previews[3]);
                DrawColorRow(card, card.y + 94f, "ModernExpandMenu_ColorSurfaceContainerHigh".Translate(), values, 4, MD3Theme.DefaultSurfaceContainerHigh, previews[4]);
            });

            // 文本
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorText".Translate(), 4, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorOnSurface".Translate(), values, 5, MD3Theme.DefaultOnSurface, previews[5]);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorOnSurfaceVariant".Translate(), values, 6, MD3Theme.DefaultOnSurfaceVariant, previews[6]);
                DrawColorRow(card, card.y + 94f, "ModernExpandMenu_ColorDisabledText".Translate(), values, 8, MD3Theme.DefaultDisabledText, previews[8]);
            });

            // 边框（Outline 描边色，非文本色）
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorBorder".Translate(), 1, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorOutline".Translate(), values, 7, MD3Theme.DefaultOutline, previews[7]);
            });

            // 滚动条
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorScrollbar".Translate(), 3, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorScrollbarTrack".Translate(), values, 10, MD3Theme.DefaultScrollbarTrack, previews[10]);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorScrollbarThumb".Translate(), values, 11, MD3Theme.DefaultScrollbarThumb, previews[11]);
                DrawColorRow(card, card.y + 94f, "ModernExpandMenu_ColorScrollbarThumbDragging".Translate(), values, 12, MD3Theme.DefaultScrollbarThumbDragging, previews[12]);
            });

            // 阴影
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorShadow".Translate(), 1, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorShadow".Translate(), values, 9, MD3Theme.DefaultShadow, previews[9]);
            });

            // 写回设置（保存编辑结果）
            SetColorValues(miscPalette, values);
        }

        /// <summary>颜色 tab 右侧设置内容总高（左预览区与其对齐）。</summary>
        private static float ComputeColorSettingsHeight(float contentWidth)
        {
            const float rowHeight = 30f;
            float palette = 30f + 34f * 3f + 6f * 2f + 12f;
            // 主色2 + 表面3 + 文本3 + 边框1 + 滚动条3 + 阴影1
            float cards = (30f + rowHeight * 2f + 12f) + (30f + rowHeight * 3f + 12f) + (30f + rowHeight * 3f + 12f) + (30f + rowHeight + 12f) + (30f + rowHeight * 3f + 12f) + (30f + rowHeight + 12f);
            return palette + cards + 12f;
        }

        /// <summary>调色板：预设配色一键应用（2 行 2 列，MD3 风格卡片；应用到当前编辑的配色）。</summary>
        private static float DrawPaletteCard(float contentX, float y, float contentWidth, bool miscPalette, string[] values)
        {
            const float rowHeight = 34f;
            const int paletteCount = 6;
            const int paletteColumns = 2;
            const int paletteRows = 3;
            float height = 30f + rowHeight * paletteRows + 6f * (paletteRows - 1) + 12f;
            var card = new Rect(contentX, y, contentWidth, height);
            MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(card, "ModernExpandMenu_PaletteTitle".Translate());
            float cy = card.y + 34f;
            string[] paletteKeys =
            {
                "ModernExpandMenu_PaletteAquaBlue",
                "ModernExpandMenu_PaletteCyberPurple",
                "ModernExpandMenu_PaletteEmerald",
                "ModernExpandMenu_PaletteLavaOrange",
                "ModernExpandMenu_PaletteRose",
                "ModernExpandMenu_PaletteIceBlue"
            };
            // 按钮宽与坐标取整到像素：非整数宽度会让 9-slice 右角拼接处出现 1px 竖线
            float buttonWidth = Mathf.Floor((contentWidth - 28f - 12f) / 2f);
            float buttonGap = Mathf.Max(12f, contentWidth - 28f - buttonWidth * 2f);   // 剩余间隙吸收取整余量
            for (int i = 0; i < paletteCount; i++)
            {
                float bx = Mathf.Floor(card.x + 14f) + (i % paletteColumns) * (buttonWidth + buttonGap);
                float by = Mathf.Floor(cy) + (i / paletteColumns) * (rowHeight + 6f);
                if (MD3Widgets.MD3Button(new Rect(bx, by, buttonWidth, rowHeight), paletteKeys[i].Translate(), false))
                {
                    ApplyPalette(miscPalette, values, i);
                }
            }
            return card.yMax + 12f;
        }

        /// <summary>应用预设调色板（全套 13 色 16 进制，写入当前编辑的配色）。</summary>
        private static void ApplyPalette(bool miscPalette, string[] values, int index)
        {
            string[] palette = GetPaletteColors(index);
            for (int i = 0; i < 13 && i < values.Length; i++)
            {
                values[i] = palette[i];
            }
            SetColorValues(miscPalette, values);
            Settings.Write();
        }

        /// <summary>返回指定索引的预设调色板（13 色，顺序同 GetColorValues）。</summary>
        private static string[] GetPaletteColors(int index)
        {
            switch (index)
            {
                case 1: // 赛博紫
                    return new[] { "#B388FF", "#1B1030", "#1B1124", "#26163A", "#2E1B47", "#E8E6F0", "#B3AEC4", "#7E7A8C", "#9A94A8", "#000000", "#2A2436", "#6E6680", "#8F86A6" };
                case 2: // 翡翠绿
                    return new[] { "#00C853", "#00291A", "#0E2118", "#143327", "#1A3F2E", "#E0F2E9", "#A8C9B8", "#6E8F7F", "#8FAD9F", "#000000", "#1C2E24", "#5E7E6E", "#7FA093" };
                case 3: // 熔岩橙
                    return new[] { "#FF6D00", "#2B1500", "#221812", "#33221A", "#402A1F", "#F2E8E2", "#C4B5AC", "#8A7B72", "#AA968A", "#000000", "#2E221C", "#7A665A", "#9C8577" };
                case 4: // 粉彩玫瑰（FFC0CE）
                    return new[] { "#FFC0CE", "#3A1C24", "#2A1A1F", "#352027", "#412A32", "#F5E6EA", "#C5A9B1", "#8A6E76", "#A08890", "#000000", "#332027", "#8A5D68", "#B07A86" };
                case 5: // 冰雪蓝（C0F3FF）
                    return new[] { "#C0F3FF", "#0A2A33", "#16222A", "#1D2C36", "#263A47", "#E3F2F8", "#A8C3CE", "#6E8A96", "#8DA3AD", "#000000", "#1E2E38", "#4E7687", "#6F9CAD" };
                default: // 水影蓝
                    return new[] { "#00A8FF", "#001421", "#161821", "#1E212D", "#262A3A", "#E6E6EC", "#9A9BA6", "#636676", "#80808C", "#000000", "#26262E", "#525261", "#737385" };
            }
        }

        /// <summary>绘制颜色分类卡片，返回下一行的 y 坐标。</summary>
        private static float DrawColorCard(float contentX, float y, float contentWidth, string title, int rowCount, Action<Rect> drawRows)
        {
            const float rowHeight = 30f;
            float height = 30f + rowHeight * rowCount + 12f;
            var card = new Rect(contentX, y, contentWidth, height);
            MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(card, title);
            drawRows(card);
            return card.yMax + 12f;
        }

        /// <summary>绘制单行颜色设置：色块（点击复制）+ 名称 + 粘贴按钮 + 16 进制输入框（非法时红色边框）。
        /// 通过 values[index] 读写当前编辑配色的对应颜色。</summary>
        private static void DrawColorRow(Rect card, float y, string label, string[] values, int index, Color fallback, Color preview)
        {
            string hex = values[index];
            var rowRect = new Rect(card.x + 14f, y, card.width - 28f, 30f);

            // 色块预览：点击一键复制 hex 到剪贴板
            var swatchRect = new Rect(rowRect.x, rowRect.y + 3f, 24f, 24f);
            MD3Widgets.DrawRoundedRect(swatchRect, preview, 4f);
            MD3Widgets.DrawRoundedRect(swatchRect.ContractedBy(1f), MD3Theme.Surface, 3f);
            if (Mouse.IsOver(swatchRect))
            {
                MD3Widgets.DrawHoverState(swatchRect, 4f);
            }
            TooltipHandler.TipRegion(swatchRect, "ModernExpandMenu_ColorCopyHint".Translate());
            if (Widgets.ButtonInvisible(swatchRect))
            {
                GUIUtility.systemCopyBuffer = hex;   // 复制到剪贴板
            }

            // 名称（预留色块 + 粘贴 + 输入框空间）
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x + 32f, rowRect.y, rowRect.width - 220f, rowRect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true
            GUI.color = Color.white;

            // 粘贴按钮（点击从剪贴板导入 hex）
            var pasteRect = new Rect(rowRect.xMax - 124f, rowRect.y + 3f, 28f, 24f);
            if (MD3Widgets.MD3Button(pasteRect, "ModernExpandMenu_ColorPaste".Translate(), false))
            {
                string clipboard = GUIUtility.systemCopyBuffer;
                if (TryParseHex(clipboard, out Color _))
                {
                    values[index] = clipboard;
                }
            }

            // 16 进制输入框（MD3 自实现外观：深色背景 + 主色描边环，非法时红色描边；内部原版可靠输入）
            var hexRect = new Rect(rowRect.xMax - 92f, rowRect.y + 3f, 92f, 24f);
            hex = MD3Widgets.MD3TextField(hexRect, hex, label.GetHashCode(), TryParseHex(hex, out _));

            // 写回数组（随编辑更新当前配色的对应值）
            values[index] = hex;
        }

        /// <summary>解析 16 进制颜色（可带 # 前缀，如 #RRGGBB 或 RRGGBB）。</summary>
        private static bool TryParseHex(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(hex))
            {
                return false;
            }
            string clean = hex.TrimStart('#').Trim();
            if (clean.Length < 6)
            {
                return false;
            }
            try
            {
                int r = Convert.ToInt32(clean.Substring(0, 2), 16);
                int g = Convert.ToInt32(clean.Substring(2, 2), 16);
                int b = Convert.ToInt32(clean.Substring(4, 2), 16);
                color = new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>绘制设置分组卡片标题（主色高亮）。</summary>
        private static void DrawSettingsTitle(Rect card, string title)
        {
            GUI.color = MD3Theme.Primary;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(card.x + 14f, card.y + 8f, card.width - 28f, 20f), title);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
        }

        /// <summary>设置行高：赛博模式下每行是完整 demo 卡片（56px 高），其余 30px。</summary>
        private static float CheckboxRowHeight => Settings.switchStyle == ModernExpandMenuSettings.SwitchStyle.Cyber ? 56f : 30f;

        /// <summary>绘制带开关的一行设置（按「开关风格」设置渲染：原版 / MD3 滑动开关 / 赛博炫酷卡片）。</summary>
        private static void DrawCheckboxRow(Rect card, float y, string label, ref bool value, int switchId, string tooltip = null)
        {
            var rowRect = new Rect(card.x + 14f, y, card.width - 28f, CheckboxRowHeight);
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rowRect, tooltip);
            }

            var style = Settings.switchStyle;
            if (style == ModernExpandMenuSettings.SwitchStyle.Cyber)
            {
                // 赛博炫酷：完整 demo 卡片（图标 + 标题 + 描述 + 徽章 + 流光边框，点击整行切换）
                value = MD3Widgets.MD3CyberSwitch(rowRect, label, tooltip, value, switchId);
                return;
            }

            // 原版 / 滑动开关：标签在左、开关在右
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - 50f, rowRect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            var switchRect = new Rect(rowRect.xMax - 44f, rowRect.y + (rowRect.height - 24f) / 2f, 44f, 24f);
            if (style == ModernExpandMenuSettings.SwitchStyle.Vanilla)
            {
                // 原版复选框（对齐到开关区域中央）
                bool newValue = value;
                Widgets.Checkbox(new Vector2(switchRect.x + 4f, switchRect.y), ref newValue);
                value = newValue;
            }
            else
            {
                value = MD3Widgets.MD3ToggleSwitch(switchRect, value, switchId);
            }
        }

        /// <summary>
        /// 绘制单行紧凑滑块设置：标签（左）+ 滑块（中）+ 数值按钮（右，可点击自由输入）。
        /// 数值输入提交时自动限制在安全范围（非负 / 不低于下限，防止产生 bug 的取值）。
        /// </summary>
        private static void DrawSliderRow(Rect card, float y, string label, ref float value, float min, float max, int sliderId, string valueFormat, bool disabled = false)
        {
            float rowHeight = 30f;
            float rowX = card.x + 14f;
            float rowWidth = card.width - 28f;

            // 数值按钮（右端 64px，可点击编辑）
            var valueRect = new Rect(rowX + rowWidth - 64f, y, 64f, rowHeight);
            // 标签（左侧固定宽度，过长截断）
            float labelWidth = Mathf.Min(160f, rowWidth * 0.42f);
            var labelRect = new Rect(rowX, y, labelWidth, rowHeight);
            // 滑块（标签与数值之间的弹性区域）
            var sliderRect = new Rect(labelRect.xMax + 8f, y, valueRect.x - labelRect.xMax - 16f, rowHeight);

            // 标签（disabled 低饱和度灰显）
            GUI.color = disabled ? MD3Theme.DisabledText : MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            // 数值区域：编辑态显示 MD3 边框数值输入框（原版可靠输入），否则显示可点击数值按钮
            if (!disabled && editingSliderId == sliderId)
            {
                editingValueRect = valueRect;
                // 设置控件名并自动聚焦，保证键盘可直接输入指定数字
                string controlName = "ModernExpandMenuNumericField" + sliderId;
                GUI.SetNextControlName(controlName);
                MD3Widgets.MD3NumberField(valueRect, ref value, ref editingBuffer, min, out bool submitted, out bool cancelled);
                if (focusNumericFieldId == sliderId)
                {
                    GUI.FocusControl(controlName);
                    focusNumericFieldId = -1;
                }
                if (submitted)
                {
                    // 无上限自由输入，仅保下限（防止负数 / 0 等严重 bug 取值）
                    value = Mathf.Max(min, value);
                    editingSliderId = -1;
                }
                else if (cancelled)
                {
                    editingSliderId = -1;   // 取消编辑
                }
            }
            else
            {
                MD3Widgets.DrawRoundedRect(valueRect, MD3Theme.SurfaceContainerHigh, 4f);
                if (!disabled && Mouse.IsOver(valueRect))
                {
                    MD3Widgets.DrawHoverState(valueRect, 4f);
                }
                GUI.color = disabled ? MD3Theme.DisabledText : MD3Theme.Primary;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(valueRect, value.ToString(valueFormat));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                if (!disabled && Widgets.ButtonInvisible(valueRect))
                {
                    editingSliderId = sliderId;
                    editingBuffer = value.ToString(valueFormat);
                    editingValueRect = valueRect;
                    focusNumericFieldId = sliderId;   // 进入编辑态后自动聚焦输入框
                }
            }

            // 滑块：禁用时静态灰化（低饱和度轨道 + 圆点，不响应交互）；正常时点击轨道跳转 / 按住拖动
            if (!disabled)
            {
                value = MD3Widgets.MD3Slider(sliderRect, value, min, max, sliderId);
            }
            else
            {
                float t = Mathf.InverseLerp(min, max, value);
                var track = new Rect(sliderRect.x, sliderRect.y + (sliderRect.height - 4f) / 2f, sliderRect.width, 4f);
                MD3Widgets.DrawRoundedRect(track, MD3Theme.SurfaceContainerHigh, 2f);
                var fill = new Rect(track.x, track.y, track.width * t, track.height);
                if (fill.width > 2f)
                {
                    MD3Widgets.DrawRoundedRect(fill, MD3Theme.DisabledText, 2f);
                }
                float knobSize = 14f;
                var knob = new Rect(track.x + t * track.width - knobSize / 2f, sliderRect.y + (sliderRect.height - knobSize) / 2f, knobSize, knobSize);
                MD3Widgets.DrawRoundedRect(knob, MD3Theme.DisabledText, knobSize / 2f);
            }
        }
    }
}
