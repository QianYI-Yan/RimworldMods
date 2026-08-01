using Verse;

namespace ModernExpandMenu
{
    // ═══════════════════════════════════════════════════
    // 模组设置 —— 持久化到 Mod 配置 XML，游戏内"选项 → Mod 设置"界面修改
    // ═══════════════════════════════════════════════════
    public class ModernExpandMenuSettings : ModSettings
    {
        public bool showLoadingAnimation = true;        // 显示加载动画（顶部缓冲条 + 环形进度 + 覆盖层 + 逐条载入）
        public bool showHoverHighlightAndArrow = true;  // 悬停操作项时高亮目标物品并绘制发光箭头
        public bool showItemCount = true;               // 分组标题显示物品总数（×N）
        public int maxMenuHeight = 560;                 // 悬浮窗最大高度（像素，超出滚动）
        public int maxProcessedPerFrame = 6;            // 每帧处理的物品实例数（越大加载越快，卡顿风险越高）
        public float extraLoadingBarSeconds = 0.5f;     // 加载完成后进度条强制额外显示时长（秒，0 为不强制）

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showLoadingAnimation, "showLoadingAnimation", true);
            Scribe_Values.Look(ref showHoverHighlightAndArrow, "showHoverHighlightAndArrow", true);
            Scribe_Values.Look(ref showItemCount, "showItemCount", true);
            Scribe_Values.Look(ref maxMenuHeight, "maxMenuHeight", 560);
            Scribe_Values.Look(ref maxProcessedPerFrame, "maxProcessedPerFrame", 6);
            Scribe_Values.Look(ref extraLoadingBarSeconds, "extraLoadingBarSeconds", 0.5f);
        }
    }
}
