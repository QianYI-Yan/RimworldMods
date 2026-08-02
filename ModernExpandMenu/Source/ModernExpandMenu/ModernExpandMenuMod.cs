using System;
using System.Reflection;
using HarmonyLib;
using ModernExpandMenu.Theme;
using ModernExpandMenu.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 模组入口 —— 安装 Harmony 补丁
    // 通过 PatchAll 自动注册本程序集内所有 HarmonyPatch
    // ═══════════════════════════════════════════════════
    public class ModernExpandMenuMod : Mod
    {
        // 全局设置实例（窗口 / 补丁各处读取）
        public static ModernExpandMenuSettings Settings;

        // 设置界面：当前正在编辑的数值滑块（点击数值框进入输入态）
        private static int editingSliderId = -1;
        private static string editingBuffer = "";
        private static Rect editingValueRect;
        private static int focusNumericFieldId = -1;   // 需要自动聚焦的数值输入框（进入编辑态后保证键盘可直接输入）

        // 设置界面：当前 tab（0=常规，1=动画，2=杂项）
        private static int settingsTab;
        private static int miscSubTab;                   // 杂项 tab 内子 tab：0=全局样式，1=颜色
        private static Vector2 settingsScrollPosition;   // 设置内容滚动位置（内容超出窗口高度时可滚动）

        // 伪翻译开关的上次值（退出设置时检测变化并强制刷新 UI）
        public static bool lastPseudoTranslationSetting;

        // Harmony 唯一标识，与 About.xml 的 packageId 一致
        public const string HarmonyId = "yintx.deepseek.modernexpandmenu";

        public ModernExpandMenuMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ModernExpandMenuSettings>();
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>游戏内"选项 → Mod 设置"界面（MD3 风格：tab 三页 + 深色卡片 + 安卓控件 + 16 进制颜色自定义；内容超出时可滚动）。</summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 整窗 MD3 表面背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // ── 左侧固定预览栏（独立于设置页范围：所有 tab 共用，不随 tab 切换、不滚动）──
            // 颜色与动画速度设置共用同一个可交互菜单预览（模拟游戏操作）
            float previewWidth = inRect.width * 0.36f;
            var previewRect = new Rect(inRect.x + 18f, inRect.y + 16f, previewWidth, inRect.height - 32f);
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(previewRect.x, previewRect.y, previewRect.width, 20f), "ModernExpandMenu_AnimationPreview".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true
            GUI.color = Color.white;
            MenuPreviewWidget.Draw(new Rect(previewRect.x, previewRect.y + 24f, previewRect.width, previewRect.height - 24f));

            // ── 右侧：Tab 栏 + 内容滚动区 ──
            float contentX = previewRect.xMax + 12f;
            float contentWidth = inRect.xMax - contentX - 18f;
            float y = inRect.y + 16f;

            // ── Tab 栏（固定顶部，MD3 风格胶囊按钮，选中主色填充）──
            const float tabBarHeight = 34f;
            const float tabGap = 8f;
            float tabWidth = (contentWidth - tabGap * 2f) / 3f;
            var tabGeneralRect = new Rect(contentX, y, tabWidth, tabBarHeight);
            var tabAnimationRect = new Rect(contentX + (tabWidth + tabGap), y, tabWidth, tabBarHeight);
            var tabMiscRect = new Rect(contentX + (tabWidth + tabGap) * 2f, y, tabWidth, tabBarHeight);
            if (MD3Widgets.MD3Button(tabGeneralRect, "ModernExpandMenu_TabGeneral".Translate(), settingsTab == 0))
            {
                settingsTab = 0;
                editingSliderId = -1;
            }
            if (MD3Widgets.MD3Button(tabAnimationRect, "ModernExpandMenu_TabAnimations".Translate(), settingsTab == 1))
            {
                settingsTab = 1;
                editingSliderId = -1;
            }
            if (MD3Widgets.MD3Button(tabMiscRect, "ModernExpandMenu_TabMisc".Translate(), settingsTab == 2))
            {
                settingsTab = 2;
                editingSliderId = -1;
            }
            float contentTop = y + tabBarHeight + 12f;

            // ── 内容滚动区（各 tab 内容可能超出窗口高度，包 MD3 滚动视口）──
            float contentTotal = ComputeSettingsContentHeight(contentWidth, settingsTab);
            var scrollRect = new Rect(contentX, contentTop, contentWidth, inRect.yMax - contentTop);
            var contentRect = new Rect(0f, 0f, contentWidth, contentTotal);
            MD3Widgets.MD3BeginScrollView(scrollRect, ref settingsScrollPosition, contentRect);

            // 点击编辑框外部时退出输入态（未回车视为取消；视口局部坐标）
            if (editingSliderId >= 0 && Event.current.type == EventType.MouseDown && !Mouse.IsOver(editingValueRect))
            {
                editingSliderId = -1;
            }

            // 各 tab 内容从视口局部 y=0 开始绘制
            if (settingsTab == 0)
            {
                DrawGeneralTab(0f, contentWidth, 0f);
            }
            else if (settingsTab == 1)
            {
                DrawAnimationTab(0f, contentWidth, 0f);
            }
            else
            {
                DrawMiscTab(0f, contentWidth, 0f);
            }

            MD3Widgets.MD3EndScrollView(scrollRect, ref settingsScrollPosition, contentTotal, 3000, MD3Theme.CardCornerRadius);

            Settings.Write();
        }

        /// <summary>估算各 tab 内容总高度（用于滚动视口内容区高度）。</summary>
        private static float ComputeSettingsContentHeight(float contentWidth, int tab)
        {
            const float rowHeight = 30f;
            switch (tab)
            {
                case 1: // 动画：总开关卡片 + 速度卡片(7 行)
                    return (30f + rowHeight + 12f) + (30f + rowHeight * 7f + 12f) + 48f;
                case 2: // 杂项：子 tab（全局样式 / 颜色），取较高者
                    float globalStyle = (30f + rowHeight * 3f + 12f) + 42f;
                    float colorSub = ComputeColorSettingsHeight(contentWidth) + 42f;
                    return Mathf.Max(globalStyle, colorSub) + 48f;
                default: // 常规：外观(5) + 性能(2) + 加载(1) + 恢复默认 + 分享
                    float appearance = 30f + rowHeight * 5f + 12f;
                    float performance = 30f + rowHeight * 2f + 12f;
                    float loading = 30f + rowHeight + 12f;
                    return appearance + performance + loading + (34f + 12f) + (30f + rowHeight + 12f) + 48f;
            }
        }

        /// <summary>常规 tab：外观 / 性能 / 加载动画 / 恢复默认。</summary>
        private static void DrawGeneralTab(float contentX, float contentWidth, float y)
        {
            const float rowHeight = 30f;

            // ===== 外观卡片 =====
            float appearanceHeight = 30f + rowHeight * 5f + 12f;
            var appearanceCard = new Rect(contentX, y, contentWidth, appearanceHeight);
            MD3Widgets.DrawCard(appearanceCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(appearanceCard, "ModernExpandMenu_SectionAppearance".Translate());
            float cy = appearanceCard.y + 34f;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ModEnabled".Translate(), ref Settings.modEnabled, 0); cy += rowHeight;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_DisablePseudoTranslation".Translate(), ref Settings.disableDevPseudoTranslation, 4); cy += rowHeight;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowLoadingAnimation".Translate(), ref Settings.showLoadingAnimation, 1); cy += rowHeight;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowHoverHighlight".Translate(), ref Settings.showHoverHighlightAndArrow, 2); cy += rowHeight;
            DrawCheckboxRow(appearanceCard, cy, "ModernExpandMenu_ShowItemCount".Translate(), ref Settings.showItemCount, 3); cy += rowHeight;
            y = appearanceCard.yMax + 12f;

            // ===== 性能卡片 =====
            float performanceHeight = 30f + rowHeight * 2f + 12f;
            var performanceCard = new Rect(contentX, y, contentWidth, performanceHeight);
            MD3Widgets.DrawCard(performanceCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(performanceCard, "ModernExpandMenu_SectionPerformance".Translate());
            cy = performanceCard.y + 34f;
            float maxHeightValue = Settings.maxMenuHeight;
            DrawSliderRow(performanceCard, cy, "ModernExpandMenu_MaxMenuHeight".Translate(), ref maxHeightValue, 300f, 900f, 0, "0");
            Settings.maxMenuHeight = Mathf.RoundToInt(maxHeightValue);
            cy += rowHeight;
            float perFrameValue = Settings.maxProcessedPerFrame;
            DrawSliderRow(performanceCard, cy, "ModernExpandMenu_MaxProcessedPerFrame".Translate(), ref perFrameValue, 2f, 30f, 1, "0");
            Settings.maxProcessedPerFrame = Mathf.RoundToInt(perFrameValue);
            cy += rowHeight;
            y = performanceCard.yMax + 12f;

            // ===== 加载动画卡片 =====
            float loadingHeight = 30f + rowHeight + 12f;
            var loadingCard = new Rect(contentX, y, contentWidth, loadingHeight);
            MD3Widgets.DrawCard(loadingCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(loadingCard, "ModernExpandMenu_SectionLoading".Translate());
            cy = loadingCard.y + 34f;
            float extraSecondsValue = Settings.extraLoadingBarSeconds;
            DrawSliderRow(loadingCard, cy, "ModernExpandMenu_ExtraLoadingBarSeconds".Translate(), ref extraSecondsValue, 0f, 5f, 2, "0.0");
            Settings.extraLoadingBarSeconds = extraSecondsValue;
            cy += rowHeight;
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

        /// <summary>杂项 tab：子 tab（全局样式 / 颜色），放与右键菜单无关的功能。</summary>
        private static void DrawMiscTab(float contentX, float contentWidth, float y)
        {
            // 子 tab 栏（全局样式 / 颜色）
            const float subTabHeight = 30f;
            const float subTabGap = 6f;
            float subTabWidth = (contentWidth - subTabGap) / 2f;
            var subStyleRect = new Rect(contentX, y, subTabWidth, subTabHeight);
            var subColorsRect = new Rect(contentX + subTabWidth + subTabGap, y, subTabWidth, subTabHeight);
            if (MD3Widgets.MD3Button(subStyleRect, "ModernExpandMenu_MiscGlobalStyle".Translate(), miscSubTab == 0))
            {
                miscSubTab = 0;
            }
            if (MD3Widgets.MD3Button(subColorsRect, "ModernExpandMenu_TabColors".Translate(), miscSubTab == 1))
            {
                miscSubTab = 1;
            }
            float cy = y + subTabHeight + 12f;
            if (miscSubTab == 0)
            {
                DrawGlobalStyleTab(contentX, contentWidth, cy);
            }
            else
            {
                DrawColorTab(contentX, contentWidth, cy);
            }
        }

        /// <summary>杂项 → 全局样式：把原版输入框 / 按钮 / 复选框 / tab / 滚动条等全局替换为 MD3 样式的开关（与右键菜单无关）。</summary>
        private static void DrawGlobalStyleTab(float contentX, float contentWidth, float y)
        {
            const float rowHeight = 30f;
            float styleHeight = 30f + rowHeight * 3f + 12f;
            var styleCard = new Rect(contentX, y, contentWidth, styleHeight);
            MD3Widgets.DrawCard(styleCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(styleCard, "ModernExpandMenu_MiscGlobalStyle".Translate());
            float cy = styleCard.y + 34f;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleAllInputs".Translate(), ref Settings.md3StyleAllInputs, 21, "ModernExpandMenu_Md3StyleAllInputsDesc".Translate()); cy += rowHeight;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_Md3StyleAllButtons".Translate(), ref Settings.md3StyleAllButtons, 22, "ModernExpandMenu_Md3StyleAllButtonsDesc".Translate()); cy += rowHeight;
            DrawCheckboxRow(styleCard, cy, "ModernExpandMenu_SkipUploadWait".Translate(), ref Settings.skipUploadWait, 23, "ModernExpandMenu_SkipUploadWaitDesc".Translate()); cy += rowHeight;
        }

        /// <summary>动画 tab：动画速度设置（串行滑入时长 / 间隔 / 弹出 / 展开 / 滚动速度）+ 实时预览区域。</summary>
        private static void DrawAnimationTab(float contentX, float contentWidth, float y)
        {
            const float rowHeight = 30f;

            // ===== 动画总开关卡片（关闭后停用所有动画效果）=====
            float masterHeight = 30f + rowHeight + 12f;
            var masterCard = new Rect(contentX, y, contentWidth, masterHeight);
            MD3Widgets.DrawCard(masterCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(masterCard, "ModernExpandMenu_SectionAnimationMaster".Translate());
            DrawCheckboxRow(masterCard, masterCard.y + 34f, "ModernExpandMenu_EnableAnimations".Translate(), ref Settings.enableAnimations, 20);
            y = masterCard.yMax + 12f;

            // ===== 动画速度卡片（7 行滑块）=====
            float speedHeight = 30f + rowHeight * 7f + 12f;
            var speedCard = new Rect(contentX, y, contentWidth, speedHeight);
            MD3Widgets.DrawCard(speedCard, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
            DrawSettingsTitle(speedCard, "ModernExpandMenu_SectionAnimation".Translate());
            float cy = speedCard.y + 34f;
            float appearDuration = Settings.itemAppearDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ItemAppearDuration".Translate(), ref appearDuration, 0.05f, 1f, 10, "0.00"); cy += rowHeight;
            Settings.itemAppearDuration = appearDuration;
            float appearInterval = Settings.itemAppearInterval;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ItemAppearInterval".Translate(), ref appearInterval, 0f, 0.5f, 11, "0.00"); cy += rowHeight;
            Settings.itemAppearInterval = appearInterval;
            float popDuration = Settings.popAnimationDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_PopAnimationDuration".Translate(), ref popDuration, 0.05f, 1f, 12, "0.00"); cy += rowHeight;
            Settings.popAnimationDuration = popDuration;
            float expandSpeed = Settings.expandAnimationSpeed;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ExpandAnimationSpeed".Translate(), ref expandSpeed, 1f, 20f, 13, "0.0"); cy += rowHeight;
            Settings.expandAnimationSpeed = expandSpeed;
            float followSpeed = Settings.scrollFollowSpeed;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ScrollFollowSpeed".Translate(), ref followSpeed, 5f, 120f, 14, "0"); cy += rowHeight;
            Settings.scrollFollowSpeed = followSpeed;
            float returnDuration = Settings.scrollReturnDuration;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_ScrollReturnDuration".Translate(), ref returnDuration, 0.2f, 2f, 15, "0.00"); cy += rowHeight;
            Settings.scrollReturnDuration = returnDuration;
            float heightSpeed = Settings.windowHeightAnimationSpeed;
            DrawSliderRow(speedCard, cy, "ModernExpandMenu_WindowHeightAnimationSpeed".Translate(), ref heightSpeed, 30f, 400f, 16, "0"); cy += rowHeight;
            Settings.windowHeightAnimationSpeed = heightSpeed;
        }

        /// <summary>恢复默认颜色（16 进制，带 # 前缀）。</summary>
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
        }

        /// <summary>颜色 tab：调色板 + 按分类卡片列出所有可自定义颜色（16 进制输入）。</summary>
        private static void DrawColorTab(float contentX, float contentWidth, float y)
        {
            float sy = y;
            sy = DrawPaletteCard(contentX, sy, contentWidth);

            // 主色
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorPrimary".Translate(), 2, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorPrimary".Translate(), ref Settings.colorPrimary, MD3Theme.DefaultPrimary, MD3Theme.Primary);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorOnPrimary".Translate(), ref Settings.colorOnPrimary, MD3Theme.DefaultOnPrimary, MD3Theme.OnPrimary);
            });

            // 表面
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorSurface".Translate(), 3, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorSurface".Translate(), ref Settings.colorSurface, MD3Theme.DefaultSurface, MD3Theme.Surface);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorSurfaceContainer".Translate(), ref Settings.colorSurfaceContainer, MD3Theme.DefaultSurfaceContainer, MD3Theme.SurfaceContainer);
                DrawColorRow(card, card.y + 94f, "ModernExpandMenu_ColorSurfaceContainerHigh".Translate(), ref Settings.colorSurfaceContainerHigh, MD3Theme.DefaultSurfaceContainerHigh, MD3Theme.SurfaceContainerHigh);
            });

            // 文本
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorText".Translate(), 4, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorOnSurface".Translate(), ref Settings.colorOnSurface, MD3Theme.DefaultOnSurface, MD3Theme.OnSurface);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorOnSurfaceVariant".Translate(), ref Settings.colorOnSurfaceVariant, MD3Theme.DefaultOnSurfaceVariant, MD3Theme.OnSurfaceVariant);
                DrawColorRow(card, card.y + 94f, "ModernExpandMenu_ColorOutline".Translate(), ref Settings.colorOutline, MD3Theme.DefaultOutline, MD3Theme.Outline);
                DrawColorRow(card, card.y + 124f, "ModernExpandMenu_ColorDisabledText".Translate(), ref Settings.colorDisabledText, MD3Theme.DefaultDisabledText, MD3Theme.DisabledText);
            });

            // 滚动条
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorScrollbar".Translate(), 3, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorScrollbarTrack".Translate(), ref Settings.colorScrollbarTrack, MD3Theme.DefaultScrollbarTrack, MD3Theme.ScrollbarTrack);
                DrawColorRow(card, card.y + 64f, "ModernExpandMenu_ColorScrollbarThumb".Translate(), ref Settings.colorScrollbarThumb, MD3Theme.DefaultScrollbarThumb, MD3Theme.ScrollbarThumb);
                DrawColorRow(card, card.y + 94f, "ModernExpandMenu_ColorScrollbarThumbDragging".Translate(), ref Settings.colorScrollbarThumbDragging, MD3Theme.DefaultScrollbarThumbDragging, MD3Theme.ScrollbarThumbDragging);
            });

            // 阴影
            sy = DrawColorCard(contentX, sy, contentWidth, "ModernExpandMenu_SectionColorShadow".Translate(), 1, card =>
            {
                DrawColorRow(card, card.y + 34f, "ModernExpandMenu_ColorShadow".Translate(), ref Settings.colorShadow, MD3Theme.DefaultShadow, MD3Theme.Shadow);
            });
        }

        /// <summary>颜色 tab 右侧设置内容总高（左预览区与其对齐）。</summary>
        private static float ComputeColorSettingsHeight(float contentWidth)
        {
            const float rowHeight = 30f;
            float palette = 30f + 34f * 3f + 6f * 2f + 12f;
            float cards = (30f + rowHeight * 2f + 12f) + (30f + rowHeight * 3f + 12f) + (30f + rowHeight * 4f + 12f) + (30f + rowHeight * 3f + 12f) + (30f + rowHeight + 12f);
            return palette + cards + 12f;
        }

        /// <summary>调色板：预设配色一键应用（2 行 2 列，MD3 风格卡片）。</summary>
        private static float DrawPaletteCard(float contentX, float y, float contentWidth)
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
            float buttonWidth = (contentWidth - 28f - 12f) / 2f;
            for (int i = 0; i < paletteCount; i++)
            {
                float bx = card.x + 14f + (i % paletteColumns) * (buttonWidth + 12f);
                float by = cy + (i / paletteColumns) * (rowHeight + 6f);
                if (MD3Widgets.MD3Button(new Rect(bx, by, buttonWidth, rowHeight), paletteKeys[i].Translate(), false))
                {
                    ApplyPalette(i);
                }
            }
            return card.yMax + 12f;
        }

        /// <summary>应用预设调色板（全套 13 色 16 进制）。</summary>
        private static void ApplyPalette(int index)
        {
            string[] palette;
            switch (index)
            {
                case 1: // 赛博紫
                    palette = new[] { "#B388FF", "#1B1030", "#1B1124", "#26163A", "#2E1B47", "#E8E6F0", "#B3AEC4", "#7E7A8C", "#9A94A8", "#000000", "#2A2436", "#6E6680", "#8F86A6" };
                    break;
                case 2: // 翡翠绿
                    palette = new[] { "#00C853", "#00291A", "#0E2118", "#143327", "#1A3F2E", "#E0F2E9", "#A8C9B8", "#6E8F7F", "#8FAD9F", "#000000", "#1C2E24", "#5E7E6E", "#7FA093" };
                    break;
                case 3: // 熔岩橙
                    palette = new[] { "#FF6D00", "#2B1500", "#221812", "#33221A", "#402A1F", "#F2E8E2", "#C4B5AC", "#8A7B72", "#AA968A", "#000000", "#2E221C", "#7A665A", "#9C8577" };
                    break;
                case 4: // 粉彩玫瑰（FFC0CE）
                    palette = new[] { "#FFC0CE", "#3A1C24", "#2A1A1F", "#352027", "#412A32", "#F5E6EA", "#C5A9B1", "#8A6E76", "#A08890", "#000000", "#332027", "#8A5D68", "#B07A86" };
                    break;
                case 5: // 冰雪蓝（C0F3FF）
                    palette = new[] { "#C0F3FF", "#0A2A33", "#16222A", "#1D2C36", "#263A47", "#E3F2F8", "#A8C3CE", "#6E8A96", "#8DA3AD", "#000000", "#1E2E38", "#4E7687", "#6F9CAD" };
                    break;
                default: // 水影蓝
                    palette = new[] { "#00A8FF", "#001421", "#161821", "#1E212D", "#262A3A", "#E6E6EC", "#9A9BA6", "#636676", "#80808C", "#000000", "#26262E", "#525261", "#737385" };
                    break;
            }
            Settings.colorPrimary = palette[0];
            Settings.colorOnPrimary = palette[1];
            Settings.colorSurface = palette[2];
            Settings.colorSurfaceContainer = palette[3];
            Settings.colorSurfaceContainerHigh = palette[4];
            Settings.colorOnSurface = palette[5];
            Settings.colorOnSurfaceVariant = palette[6];
            Settings.colorOutline = palette[7];
            Settings.colorDisabledText = palette[8];
            Settings.colorShadow = palette[9];
            Settings.colorScrollbarTrack = palette[10];
            Settings.colorScrollbarThumb = palette[11];
            Settings.colorScrollbarThumbDragging = palette[12];
            Settings.Write();
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

        /// <summary>绘制单行颜色设置：色块（点击复制）+ 名称 + 粘贴按钮 + 16 进制输入框（非法时红色边框）。</summary>
        private static void DrawColorRow(Rect card, float y, string label, ref string hex, Color fallback, Color preview)
        {
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
            const float rightAreaWidth = 142f;
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x + 32f, rowRect.y, rowRect.width - 32f - rightAreaWidth, rowRect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 粘贴按钮（从剪贴板读取 hex）
            var pasteRect = new Rect(rowRect.xMax - 134f, rowRect.y + 3f, 40f, 24f);
            if (MD3Widgets.MD3Button(pasteRect, "ModernExpandMenu_ColorPaste".Translate(), false))
            {
                string clip = GUIUtility.systemCopyBuffer;
                if (!clip.NullOrEmpty())
                {
                    hex = clip.Trim();
                }
            }

            // 16 进制输入框（MD3 自实现外观：深色背景 + 主色描边环，非法时红色描边；内部原版可靠输入）
            var hexRect = new Rect(rowRect.xMax - 92f, rowRect.y + 3f, 92f, 24f);
            hex = MD3Widgets.MD3TextField(hexRect, hex, label.GetHashCode(), TryParseHex(hex, out _));
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

        /// <summary>绘制带右侧安卓滑动开关的一行设置。</summary>
        private static void DrawCheckboxRow(Rect card, float y, string label, ref bool value, int switchId, string tooltip = null)
        {
            var rowRect = new Rect(card.x + 14f, y, card.width - 28f, 30f);
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rowRect, tooltip);
            }
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - 50f, rowRect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            var switchRect = new Rect(rowRect.xMax - 44f, rowRect.y + (rowRect.height - 24f) / 2f, 44f, 24f);
            value = MD3Widgets.MD3ToggleSwitch(switchRect, value, switchId);
        }

        /// <summary>
        /// 绘制单行紧凑滑块设置：标签（左）+ 滑块（中）+ 数值按钮（右，可点击自由输入）。
        /// 数值输入提交时自动限制在安全范围（非负 / 不低于下限，防止产生 bug 的取值）。
        /// </summary>
        private static void DrawSliderRow(Rect card, float y, string label, ref float value, float min, float max, int sliderId, string valueFormat)
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

            // 标签
            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true，否则 RimWorld 报错
            GUI.color = Color.white;

            // 数值区域：编辑态显示 MD3 边框数值输入框（原版可靠输入），否则显示可点击数值按钮
            if (editingSliderId == sliderId)
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
                if (Mouse.IsOver(valueRect))
                {
                    MD3Widgets.DrawHoverState(valueRect, 4f);
                }
                GUI.color = MD3Theme.Primary;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(valueRect, value.ToString(valueFormat));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(valueRect))
                {
                    editingSliderId = sliderId;
                    editingBuffer = value.ToString(valueFormat);
                    editingValueRect = valueRect;
                    focusNumericFieldId = sliderId;   // 进入编辑态后自动聚焦输入框
                }
            }

            // 滑块（点击轨道跳转 / 按住拖动）
            value = MD3Widgets.MD3Slider(sliderRect, value, min, max, sliderId);
        }

        public override string SettingsCategory()
        {
            return "Modern Expand Menu";
        }
    }
}
