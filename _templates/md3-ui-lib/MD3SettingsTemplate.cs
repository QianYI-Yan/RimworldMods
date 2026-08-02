using ModernExpandMenu.Theme;   // ← 换成你的命名空间：using YourMod.Theme;
using UnityEngine;
using Verse;

// ═══════════════════════════════════════════════════════════════════
// MD3 设置界面参考模板（复制到你的 Mod 后按需填充）
//
// 结构总览（对应游戏"选项 → Mod 设置"）：
//   整窗 MD3 卡片背景（DrawCard）
//   ├─ 左侧固定预览栏（可选）：所有 tab 共用、不随 tab 切换、不滚动
//   └─ 右侧：
//        ├─ 主 tab 栏（MD3Button 胶囊按钮，选中主色填充）
//        └─ 内容滚动区（MD3BeginScrollView / MD3EndScrollView，内容超高可滚动）
//             └─ 主 tab 内可再分子 tab（全局样式 / 颜色 等）
//                 └─ 卡片（DrawCard + 标题）内放各行控件：
//                      开关行 / 滑块行（含数值输入）/ 颜色行 / 按钮行
//
// 依赖：_templates/md3-ui-lib/ 的 MD3Theme.cs + MD3Widgets.cs（先复制它们）。
// ═══════════════════════════════════════════════════════════════════
public class YourModSettingsTemplate : Verse.Mod
{
    // ── 设置实例（你的 Settings : ModSettings 类）──
    public static YourModSettings Settings;

    // ── 设置界面状态 ──
    private static int mainTab;            // 主 tab：0=常规，1=动画，2=杂项
    private static int miscSubTab;         // 杂项内子 tab：0=全局样式，1=颜色
    private static Vector2 scrollPosition; // 内容滚动位置

    public YourModSettingsTemplate(ModContentPack content) : base(content)
    {
        Settings = GetSettings<YourModSettings>();
        // 若支持"从设置注入主题色"：MD3Theme.CustomPrimaryHex = Settings.colorPrimary; ...
    }

    /// <summary>游戏内"选项 → Mod 设置"界面（MD3 风格）。</summary>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        // ── 整窗 MD3 表面背景 ──
        MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

        // ── 左侧固定预览栏（可选）：所有 tab 共用、不随 tab 切换、不滚动 ──
        // 参考 ModernExpandMenu 的 UI/MenuPreviewWidget.cs（模拟游戏菜单的可交互预览）
        float previewWidth = inRect.width * 0.36f;
        var previewRect = new Rect(inRect.x + 18f, inRect.y + 16f, previewWidth, inRect.height - 32f);
        // DrawYourPreview(previewRect);   // 你的预览（如菜单/控件模拟）

        // ── 右侧：主 tab 栏 + 内容滚动区 ──
        float contentX = previewRect.xMax + 12f;
        float contentWidth = inRect.xMax - contentX - 18f;
        float y = inRect.y + 16f;

        // 主 tab 栏（MD3 胶囊按钮，选中主色填充）
        const float tabBarHeight = 34f;
        const float tabGap = 8f;
        float tabWidth = (contentWidth - tabGap * 2f) / 3f;
        if (MD3Widgets.MD3Button(new Rect(contentX, y, tabWidth, tabBarHeight), "TabGeneral".Translate(), mainTab == 0))
        {
            mainTab = 0;
        }
        if (MD3Widgets.MD3Button(new Rect(contentX + tabWidth + tabGap, y, tabWidth, tabBarHeight), "TabAnimations".Translate(), mainTab == 1))
        {
            mainTab = 1;
        }
        if (MD3Widgets.MD3Button(new Rect(contentX + (tabWidth + tabGap) * 2f, y, tabWidth, tabBarHeight), "TabMisc".Translate(), mainTab == 2))
        {
            mainTab = 2;
        }
        float contentTop = y + tabBarHeight + 12f;

        // 内容滚动区（内容超高时可滚动，MD3 细滚动条）
        float contentTotal = ComputeContentHeight(contentWidth, mainTab);
        var scrollRect = new Rect(contentX, contentTop, contentWidth, inRect.yMax - contentTop);
        var contentRect = new Rect(0f, 0f, contentWidth, contentTotal);
        MD3Widgets.MD3BeginScrollView(scrollRect, ref scrollPosition, contentRect);

        // 各主 tab 内容从视口局部 y=0 开始绘制
        if (mainTab == 0)
        {
            DrawGeneralTab(0f, contentWidth, 0f);
        }
        else if (mainTab == 1)
        {
            DrawAnimationTab(0f, contentWidth, 0f);
        }
        else
        {
            DrawMiscTab(0f, contentWidth, 0f);
        }

        MD3Widgets.MD3EndScrollView(scrollRect, ref scrollPosition, contentTotal, 3000, MD3Theme.CardCornerRadius);

        Settings.Write();
    }

    // ════════ 常规 tab：外观卡片 + 性能卡片 ════════
    private static void DrawGeneralTab(float contentX, float contentWidth, float y)
    {
        const float rowHeight = 30f;
        // 卡片：表面容器色 + 圆角
        float cardHeight = 30f + rowHeight * 2f + 12f;
        var card = new Rect(contentX, y, contentWidth, cardHeight);
        MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
        DrawSettingsTitle(card, "SectionAppearance".Translate());

        float cy = card.y + 34f;
        // 开关行（MD3 滑动开关）
        DrawCheckboxRow(card, cy, "EnableSomething".Translate(), ref Settings.enableSomething, 0);
        cy += rowHeight;
        // 滑块行（MD3 滑块 + 右侧可点击数值输入）
        float sliderValue = Settings.someSpeed;
        DrawSliderRow(card, cy, "SomeSpeed".Translate(), ref sliderValue, 0f, 100f, 10, "0.0");
        Settings.someSpeed = sliderValue;
        cy += rowHeight;
    }

    // ════════ 动画 tab ════════
    private static void DrawAnimationTab(float contentX, float contentWidth, float y)
    {
        // 与 DrawGeneralTab 类似：总开关卡片 + 速度卡片
    }

    // ════════ 杂项 tab：子 tab（全局样式 / 颜色） ════════
    private static void DrawMiscTab(float contentX, float contentWidth, float y)
    {
        // 子 tab 栏（全局样式 / 颜色）
        const float subTabHeight = 30f;
        const float subTabGap = 6f;
        float subTabWidth = (contentWidth - subTabGap) / 2f;
        if (MD3Widgets.MD3Button(new Rect(contentX, y, subTabWidth, subTabHeight), "GlobalStyle".Translate(), miscSubTab == 0))
        {
            miscSubTab = 0;
        }
        if (MD3Widgets.MD3Button(new Rect(contentX + subTabWidth + subTabGap, y, subTabWidth, subTabHeight), "Colors".Translate(), miscSubTab == 1))
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

    // ════════ 全局样式子 tab：MD3 全局替换开关 ════════
    private static void DrawGlobalStyleTab(float contentX, float contentWidth, float y)
    {
        const float rowHeight = 30f;
        float cardHeight = 30f + rowHeight * 2f + 12f;
        var card = new Rect(contentX, y, contentWidth, cardHeight);
        MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
        DrawSettingsTitle(card, "GlobalStyle".Translate());
        float cy = card.y + 34f;
        DrawCheckboxRow(card, cy, "StyleAllInputs".Translate(), ref Settings.styleAllInputs, 21);
        cy += rowHeight;
        DrawCheckboxRow(card, cy, "StyleAllButtons".Translate(), ref Settings.styleAllButtons, 22);
    }

    // ════════ 颜色子 tab：调色板 + 颜色卡片（16 进制输入） ════════
    private static void DrawColorTab(float contentX, float contentWidth, float y)
    {
        const float rowHeight = 30f;
        float cardHeight = 30f + rowHeight * 2f + 12f;
        var card = new Rect(contentX, y, contentWidth, cardHeight);
        MD3Widgets.DrawCard(card, MD3Theme.SurfaceContainer, MD3Theme.CardCornerRadius);
        DrawSettingsTitle(card, "Colors".Translate());
        float cy = card.y + 34f;
        // 颜色行：色块（点击复制）+ 名称 + 粘贴按钮 + 16 进制 MD3 输入框（非法红色描边）
        string hex = Settings.colorPrimary;
        DrawColorRow(card, cy, "ColorPrimary".Translate(), ref hex, MD3Theme.DefaultPrimary, MD3Theme.Primary);
        Settings.colorPrimary = hex;
        cy += rowHeight;
    }

    // ════════ 卡片标题（主色高亮） ════════
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

    // ════════ 开关行：标签（左）+ MD3 滑动开关（右） ════════
    private static void DrawCheckboxRow(Rect card, float y, string label, ref bool value, int switchId)
    {
        var rowRect = new Rect(card.x + 14f, y, card.width - 28f, 30f);
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

    // ════════ 滑块行：标签（左）+ MD3 滑块（中）+ 可点击数值输入（右） ════════
    private static void DrawSliderRow(Rect card, float y, string label, ref float value, float min, float max, int sliderId, string valueFormat)
    {
        float rowHeight = 30f;
        float rowX = card.x + 14f;
        float rowWidth = card.width - 28f;
        var valueRect = new Rect(rowX + rowWidth - 64f, y, 64f, rowHeight);
        float labelWidth = Mathf.Min(160f, rowWidth * 0.42f);
        var labelRect = new Rect(rowX, y, labelWidth, rowHeight);
        var sliderRect = new Rect(labelRect.xMax + 8f, y, valueRect.x - labelRect.xMax - 16f, rowHeight);

        GUI.color = MD3Theme.OnSurface;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        Widgets.Label(labelRect, label);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
        GUI.color = Color.white;

        // 数值按钮（点击进入编辑态，用原版 TextFieldNumeric 或 MD3TextField）
        MD3Widgets.DrawRoundedRect(valueRect, MD3Theme.SurfaceContainerHigh, 4f);
        if (Mouse.IsOver(valueRect))
        {
            MD3Widgets.DrawHoverState(valueRect, 4f);
        }
        GUI.color = MD3Theme.Primary;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(valueRect, value.ToString(valueFormat));
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // MD3 滑块（hover/拖动圆点放大、点击轨道跳转、拖动跟随）
        value = MD3Widgets.MD3Slider(sliderRect, value, min, max, sliderId);
    }

    // ════════ 颜色行：色块（点击复制）+ 名称 + 粘贴 + 16 进制 MD3 输入框 ════════
    private static void DrawColorRow(Rect card, float y, string label, ref string hex, Color fallback, Color preview)
    {
        var rowRect = new Rect(card.x + 14f, y, card.width - 28f, 30f);
        // 色块（点击复制 hex 到剪贴板）
        var swatchRect = new Rect(rowRect.x, rowRect.y + 3f, 24f, 24f);
        MD3Widgets.DrawRoundedRect(swatchRect, preview, 4f);
        MD3Widgets.DrawRoundedRect(swatchRect.ContractedBy(1f), MD3Theme.Surface, 3f);
        if (Widgets.ButtonInvisible(swatchRect))
        {
            GUIUtility.systemCopyBuffer = hex;
        }
        // 名称
        GUI.color = MD3Theme.OnSurface;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        Widgets.Label(new Rect(rowRect.x + 32f, rowRect.y, rowRect.width - 32f - 140f, rowRect.height), label);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
        GUI.color = Color.white;
        // 16 进制输入框（非法时红色描边）
        var hexRect = new Rect(rowRect.xMax - 92f, rowRect.y + 3f, 92f, 24f);
        hex = MD3Widgets.MD3TextField(hexRect, hex, label.GetHashCode(), TryParseHex(hex));
    }

    private static bool TryParseHex(string hex)
    {
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
            System.Convert.ToInt32(clean.Substring(0, 2), 16);
            System.Convert.ToInt32(clean.Substring(2, 2), 16);
            System.Convert.ToInt32(clean.Substring(4, 2), 16);
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    // ════════ 各 tab 内容高度估算（用于滚动视口内容区高度） ════════
    private static float ComputeContentHeight(float contentWidth, int tab)
    {
        const float rowHeight = 30f;
        switch (tab)
        {
            case 1: // 动画
                return (30f + rowHeight + 12f) + (30f + rowHeight * 2f + 12f) + 48f;
            case 2: // 杂项：子 tab（全局样式 / 颜色），取较高者
                float globalStyle = (30f + rowHeight * 2f + 12f) + 42f;
                float colors = (30f + rowHeight * 2f + 12f) + 42f;
                return Mathf.Max(globalStyle, colors) + 48f;
            default: // 常规
                return (30f + rowHeight * 2f + 12f) + 48f;
        }
    }

    public override string SettingsCategory()
    {
        return "Your Mod Name";
    }
}

/// <summary>占位设置类（换成你的 ModSettings 子类）。</summary>
public class YourModSettings : Verse.ModSettings
{
    public bool enableSomething = true;
    public float someSpeed = 50f;
    public bool styleAllInputs;
    public bool styleAllButtons;
    public string colorPrimary = "#00A8FF";

    public override void ExposeData()
    {
        Scribe_Values.Look(ref enableSomething, "enableSomething", true);
        Scribe_Values.Look(ref someSpeed, "someSpeed", 50f);
        Scribe_Values.Look(ref styleAllInputs, "styleAllInputs", false);
        Scribe_Values.Look(ref styleAllButtons, "styleAllButtons", false);
        Scribe_Values.Look(ref colorPrimary, "colorPrimary", "#00A8FF");
    }
}
