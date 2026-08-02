using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 模组设置 —— 持久化到 Mod 配置 XML，游戏内"选项 → Mod 设置"界面修改
    // ═══════════════════════════════════════════════════
    public class ModernExpandMenuSettings : ModSettings
    {
        public bool modEnabled = true;                  // 总开关：完全关闭模组功能（使用原版菜单）
        public bool disableDevPseudoTranslation = false; // 禁用开发者模式的伪翻译字符（默认不干预）
        public bool enableAnimations = true;            // 模组动画总开关（关闭后停用所有动画效果：弹出/出现消失/展开/滚动跟随/高度/加载视觉，默认开启）
        public bool showLoadingAnimation = true;        // 显示加载动画（顶部缓冲条 + 覆盖层 + 逐条载入）
        public bool showHoverHighlightAndArrow = true;  // 悬停操作项时高亮目标物品并绘制发光箭头
        public bool showItemCount = true;               // 分组标题显示物品总数（×N）
        public int maxMenuHeight = 560;                 // 悬浮窗最大高度（像素，超出滚动）
        public int maxProcessedPerFrame = 6;            // 每帧处理的物品实例数（越大加载越快，卡顿风险越高）
        public float extraLoadingBarSeconds = 0.5f;     // 加载完成后进度条强制额外显示时长（秒，0 为不强制）

        // ── 动画（动画 tab 可调）──
        public float itemAppearDuration = 0.25f;        // 单条操作项 / 分组标题滑入动画时长（秒，上一项就位后下一项开始）
        public float itemAppearInterval = 0.03f;        // 相邻项就位后的间隔（秒，串行播放）
        public float popAnimationDuration = 0.18f;      // 窗口弹出动画时长（秒）
        public float expandAnimationSpeed = 10f;        // 分组展开 / 折叠动画速度（数值越大越快）
        public float scrollFollowSpeed = 80f;           // 加载时滚动跟随底部速度（越大越跟手）
        public float scrollReturnDuration = 0.6f;       // 加载结束后滚动返回顶端的时长（秒，时间设定而非固定速度）
        public float windowHeightAnimationSpeed = 200f;  // 窗口高度动态动画速度（加载插入/展开折叠时平滑过渡）

        // ── 颜色自定义（16 进制 RGB，带 # 前缀如 #00A8FF；空/非法时回退默认水影蓝）──
        public string colorPrimary = "#00A8FF";
        public string colorOnPrimary = "#001421";
        public string colorSurface = "#161821";
        public string colorSurfaceContainer = "#1E212D";
        public string colorSurfaceContainerHigh = "#262A3A";
        public string colorOnSurface = "#E6E6EC";
        public string colorOnSurfaceVariant = "#9A9BA6";
        public string colorOutline = "#636676";
        public string colorDisabledText = "#80808C";
        public string colorShadow = "#000000";
        public string colorScrollbarTrack = "#26262E";
        public string colorScrollbarThumb = "#525261";
        public string colorScrollbarThumbDragging = "#737385";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref modEnabled, "modEnabled", true);
            Scribe_Values.Look(ref disableDevPseudoTranslation, "disableDevPseudoTranslation", false);
            Scribe_Values.Look(ref enableAnimations, "enableAnimations", true);
            Scribe_Values.Look(ref showLoadingAnimation, "showLoadingAnimation", true);
            Scribe_Values.Look(ref showHoverHighlightAndArrow, "showHoverHighlightAndArrow", true);
            Scribe_Values.Look(ref showItemCount, "showItemCount", true);
            Scribe_Values.Look(ref maxMenuHeight, "maxMenuHeight", 560);
            Scribe_Values.Look(ref maxProcessedPerFrame, "maxProcessedPerFrame", 6);
            Scribe_Values.Look(ref extraLoadingBarSeconds, "extraLoadingBarSeconds", 0.5f);
            Scribe_Values.Look(ref itemAppearDuration, "itemAppearDuration", 0.25f);
            Scribe_Values.Look(ref itemAppearInterval, "itemAppearInterval", 0.03f);
            Scribe_Values.Look(ref popAnimationDuration, "popAnimationDuration", 0.18f);
            Scribe_Values.Look(ref expandAnimationSpeed, "expandAnimationSpeed", 10f);
            Scribe_Values.Look(ref scrollFollowSpeed, "scrollFollowSpeed", 80f);
            Scribe_Values.Look(ref scrollReturnDuration, "scrollReturnDuration", 0.6f);
            Scribe_Values.Look(ref windowHeightAnimationSpeed, "windowHeightAnimationSpeed", 200f);
            Scribe_Values.Look(ref colorPrimary, "colorPrimary", "00A8FF");
            Scribe_Values.Look(ref colorOnPrimary, "colorOnPrimary", "001421");
            Scribe_Values.Look(ref colorSurface, "colorSurface", "161821");
            Scribe_Values.Look(ref colorSurfaceContainer, "colorSurfaceContainer", "1E212D");
            Scribe_Values.Look(ref colorSurfaceContainerHigh, "colorSurfaceContainerHigh", "262A3A");
            Scribe_Values.Look(ref colorOnSurface, "colorOnSurface", "E6E6EC");
            Scribe_Values.Look(ref colorOnSurfaceVariant, "colorOnSurfaceVariant", "9A9BA6");
            Scribe_Values.Look(ref colorOutline, "colorOutline", "636676");
            Scribe_Values.Look(ref colorDisabledText, "colorDisabledText", "80808C");
            Scribe_Values.Look(ref colorShadow, "colorShadow", "000000");
            Scribe_Values.Look(ref colorScrollbarTrack, "colorScrollbarTrack", "26262E");
            Scribe_Values.Look(ref colorScrollbarThumb, "colorScrollbarThumb", "525261");
            Scribe_Values.Look(ref colorScrollbarThumbDragging, "colorScrollbarThumbDragging", "737385");
        }
    }
}
