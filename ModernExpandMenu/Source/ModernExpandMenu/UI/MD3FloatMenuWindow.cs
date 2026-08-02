using System.Collections.Generic;
using System.Linq;
using ModernExpandMenu.Theme;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // MD3 风格分组悬浮窗：
    //   - 跟随鼠标弹出
    //   - 支持滚动（内容超过最大高度时出现滚动条）
    //   - 按物品大类分组，每组标题下展开子菜单操作
    // ═══════════════════════════════════════════════════
    public class MD3FloatMenuWindow : Window
    {
        private readonly List<StoredItemGroup> groups;
        private readonly FloatMenuContext savedContext;        // 保存右键上下文，供分帧生成操作
        private readonly Building_Storage highlightStorage;    // 右键的容器（用于高亮，不选中）
        private readonly List<Thing> highlightedItems;         // 右键命中的物品实例（持续高亮，原版右键目标白框效果）
        private string hoveredTooltipText;                     // 当前悬停项的完整文本（MD3 自绘 tooltip）
        private float hoverStartTime = -1f;                    // 悬停开始时间（tooltip 延迟显示）
        private const int Md3TooltipWindowId = 78123401;       // MD3 tooltip 的 ImmediateWindow 唯一 id
        private const float TooltipDelaySeconds = 0.4f;        // tooltip 延迟显示秒数
        private Vector2 scrollPosition;
        private bool draggingScrollbar;   // 是否正在拖动滚动条滑块
        private bool isLoading = true;    // 是否仍在分帧生成操作（显示加载动画）
        private float loadingFinishedAt = -1f;   // 分帧生成完成的时间（用于"强制额外显示进度条"）
        private readonly float loadStartTime;    // 分帧生成开始的时间（用于"额外时间计入总时长"的百分比计算）
        private int totalPendingCount;    // 待生成物品实例总数（用于计算加载进度）

        // 出现动画串行调度：每组独立（组间并行、组内串行），
        // 上一项就位（动画播完）后该组下一项才开始，保证同一组内不重叠
        private readonly Dictionary<StoredItemGroup, float> groupNextAppearEndTime = new Dictionary<StoredItemGroup, float>();

        // 环形加载进度纹理（用户要求保留环形本身，仅去掉发光/呼吸/亮点效果）
        private Texture2D ringTexture;                       // 环形进度纹理（逐像素生成，平滑圆弧）
        private Color[] ringPixels;                          // 环形纹理像素缓冲（复用，避免每帧 GC 分配）
        private float lastRingRebuildTime = -1f;             // 上次重建环形纹理的时间（节流用）
        private const float RingRebuildIntervalSeconds = 0.05f;  // 环形重建节流间隔（秒）

        // 加载统计（悬停加载条 tooltip 用，加载完成后开放）
        private float loadDurationSeconds;    // 分帧生成实际耗时（秒）
        private int finalActionCount;         // 加载完成后操作项总数

        // 顶部加载条（常驻显示：加载中显示进度动画，加载完成后显示满条）
        private const float LoadingBarHeight = 5f;   // 加载条高度
        private const float LoadingBarGap = 4f;      // 加载条与内容间距

        // 加载时逐组展开：第一组就位后扩大菜单，再显示下一组（组从上到下依次出现）
        private int revealedGroupCount;            // 已排定出现动画的组数（随逐组展开递增）
        private float nextGroupRevealTime;         // 下一组排定出现的时间
        private bool RevealComplete => revealedGroupCount >= groups.Count;   // 是否全部组已展开

        // 额外显示结束时间（loadingFinishedAt + extraSeconds）：加载条跑完即进入回顶步骤
        private float extraEndTime = -1f;

        // 弹出动画：窗口从鼠标锚点缩放展开（MD3 / 安卓式弹出）
        private float popScale;                                // 弹出动画进度（0~1）
        private Rect finalWindowRect;                          // 弹出完成的最终窗口位置与大小
        private Vector2 popPivot;                              // 弹出锚点（右键时的鼠标位置）
        private bool returnToTopPending = true;                // 加载视觉结束后需要平滑返回顶端
        private float scrollReturnStartTime = -1f;             // 回顶动画开始时间（时间设定用）
        private float scrollReturnStartPos;                    // 回顶动画开始的滚动位置

        // 以下各项读取模组设置（游戏内"选项 → Mod 设置"可调）
        private static int CurrentMaxProcessedPerFrame => Mathf.Max(1, ModernExpandMenuMod.Settings.maxProcessedPerFrame);
        private static float CurrentMaxMenuHeight => ModernExpandMenuMod.Settings.maxMenuHeight;
        private static float CurrentItemAppearDuration => Mathf.Max(0.02f, ModernExpandMenuMod.Settings.itemAppearDuration);
        private static float CurrentItemAppearInterval => Mathf.Max(0f, ModernExpandMenuMod.Settings.itemAppearInterval);
        private static float CurrentPopAnimationDuration => Mathf.Max(0.02f, ModernExpandMenuMod.Settings.popAnimationDuration);
        private static float CurrentExpandAnimationSpeed => Mathf.Max(0.1f, ModernExpandMenuMod.Settings.expandAnimationSpeed);
        private static float CurrentScrollFollowSpeed => Mathf.Max(1f, ModernExpandMenuMod.Settings.scrollFollowSpeed);
        private static float CurrentScrollReturnDuration => Mathf.Max(0.05f, ModernExpandMenuMod.Settings.scrollReturnDuration);
        private static float CurrentWindowHeightAnimationSpeed => Mathf.Max(10f, ModernExpandMenuMod.Settings.windowHeightAnimationSpeed);

        /// <summary>模组动画总开关（关闭后所有动画效果停用，界面直接呈现最终状态）。</summary>
        private static bool AnimationsEnabled => ModernExpandMenuMod.Settings.enableAnimations;

        /// <summary>
        /// 是否显示加载视觉：分帧生成中 → 额外显示（进度条续展到 100%）。
        /// 加载条跑完（extraEndTime 到）即结束，进入回顶步骤。期间保持覆盖层与点击锁定。
        /// </summary>
        private bool ShowLoadingVisual
        {
            get
            {
                float now = Time.realtimeSinceStartup;
                return isLoading || (extraEndTime >= 0f && now < extraEndTime);
            }
        }

        // 展开/折叠动画：目标状态 + 当前进度（0=收起，1=展开），key 为 ThingDef（"其他"组为 null）
        // 展开/折叠状态用 StoredItemGroup 对象作 key —— 对象引用永不为 null，
        // 避免"其他"组（representativeThing 为 null）导致 Dictionary null key 异常
        private readonly HashSet<StoredItemGroup> expandedTargets = new HashSet<StoredItemGroup>();
        private readonly Dictionary<StoredItemGroup, float> expandProgress = new Dictionary<StoredItemGroup, float>();

        // 内容区不额外留 Margin：Window 默认 18px 会让内容区缩小 36px，
        // 导致内容被截断、未达最大高度就出现滚动。我们用自己的 Padding 控制边距。
        protected override float Margin => 0f;

        public MD3FloatMenuWindow(List<StoredItemGroup> groups, FloatMenuContext savedContext, Building_Storage highlightStorage = null, List<Thing> highlightedItems = null)
        {
            this.groups = groups;
            this.savedContext = savedContext;
            this.highlightStorage = highlightStorage;
            this.highlightedItems = highlightedItems ?? new List<Thing>();

            // 默认收起所有子项（ClickGUI 风格：点击标题展开）
            foreach (StoredItemGroup group in groups)
            {
                expandProgress[group] = 0f;
            }

            // 记录待生成物品总数（用于环形加载进度）与加载开始时间（用于总时长百分比）
            totalPendingCount = groups.Sum(group => group.pendingItems.Count);
            loadStartTime = Time.realtimeSinceStartup;

            // 逐组展开：初始即排定第一组出现，之后按"上一组就位 → 扩大菜单 → 下一组"节奏推进
            revealedGroupCount = 0;
            nextGroupRevealTime = Time.realtimeSinceStartup;

            // 悬浮窗行为配置
            doWindowBackground = false;        // 背景由 MD3 卡片自绘
            drawShadow = false;
            absorbInputAroundWindow = true;    // 吸收窗口周围输入
            closeOnCancel = true;              // ESC 关闭
            closeOnClickedOutside = true;      // 点击外部关闭
            forcePause = false;                // 不暂停游戏
            preventCameraMotion = true;
            // 用 SubSuper 层：tooltip 是 Super 层 ImmediateWindow，
            // 若同为 Super 会被后绘制的窗口遮挡
            layer = WindowLayer.SubSuper;
        }

        /// <summary>
        /// 窗口初始尺寸：内容高度与最大高度取小值。
        /// </summary>
        public override Vector2 InitialSize =>
            new Vector2(MD3Theme.MenuWidth, Mathf.Min(TotalViewHeight, CurrentMaxMenuHeight));

        /// <summary>内容高度 + 底部边距（顶部边距与加载条空间已计入 ComputeContentHeight）。</summary>
        private float TotalViewHeight => ComputeContentHeight() + MD3Theme.Padding;

        /// <summary>
        /// 定位到鼠标附近（右键位置），并防止超出屏幕边界。
        /// 注意：windowRect 使用 GUI 空间坐标（左上原点，y 向下），
        /// 必须用 MousePositionOnUIInverted（原版 FloatMenu 同款），
        /// 不能用 MousePositionOnUI（那是左下原点，会导致窗口 y 镜像）。
        /// </summary>
        protected override void SetInitialSizeAndPosition()
        {
            Vector2 size = InitialSize;
            Vector2 mousePosition = Verse.UI.MousePositionOnUIInverted;
            float x = Mathf.Clamp(mousePosition.x + 8f, 4f, Verse.UI.screenWidth - size.x - 4f);
            float y = Mathf.Clamp(mousePosition.y + 8f, 4f, Verse.UI.screenHeight - size.y - 4f);
            // 记录最终窗口尺寸与弹出锚点（鼠标位置），供弹出动画使用
            finalWindowRect = new Rect(x, y, size.x, size.y);
            popPivot = mousePosition;
            windowRect = finalWindowRect;
        }

        /// <summary>
        /// 弹出动画：窗口从鼠标锚点缩放展开（ease-out-cubic）。
        /// MD3 / 安卓式弹出：从点击点向外放大到最终尺寸。
        /// </summary>
        private void AnimatePopIn()
        {
            popScale = Mathf.Min(1f, popScale + Time.deltaTime / CurrentPopAnimationDuration);
            // ease-out-cubic：先快后慢的缓动，避免生硬
            float scale = 1f - Mathf.Pow(1f - popScale, 3f);
            windowRect = new Rect(
                popPivot.x - (popPivot.x - finalWindowRect.x) * scale,
                popPivot.y - (popPivot.y - finalWindowRect.y) * scale,
                finalWindowRect.width * scale,
                finalWindowRect.height * scale);
            if (popScale >= 1f)
            {
                windowRect = finalWindowRect;   // 动画结束，锚定最终位置
            }
        }

        /// <summary>每帧驱动展开/折叠动画，并随动画更新窗口高度。</summary>
        public override void WindowUpdate()
        {
            base.WindowUpdate();

            // 分帧生成操作项：每帧处理有限数量的物品实例，避免大量物品时一帧卡死
            ProcessPendingActions();

            // 弹出动画：从鼠标锚点缩放展开（ease-out），完成后恢复最终尺寸；
            // 动画总开关关闭时直接显示最终位置
            if (popScale < 1f)
            {
                if (AnimationsEnabled)
                {
                    AnimatePopIn();
                    return;   // 弹出期间暂停展开高度刷新，避免高度刷新干扰缩放
                }
                popScale = 1f;
                windowRect = finalWindowRect;
            }

            // ── 逐组展开（首次加载：只有组标题，控制台式连续出现）──
            // 加载中：组标题按加载节奏均匀出现（控制台式）；加载条跑完（加载完成）时剩余组立即全部排定，
            // 避免加载过快时剩余组被 0.05s/组 拖很久（覆盖层长时间不结束）
            float now = Time.realtimeSinceStartup;
            if (revealedGroupCount < groups.Count)
            {
                if (!isLoading)
                {
                    // 加载已完成（加载条跑完）：剩余组立即全部排定，让覆盖层尽快进入底部流程
                    while (revealedGroupCount < groups.Count)
                    {
                        StoredItemGroup group = groups[revealedGroupCount];
                        if (group.appearTime < 0f)
                        {
                            group.appearTime = now;
                        }
                        revealedGroupCount++;
                    }
                }
                else if (now >= nextGroupRevealTime)
                {
                    // 加载中：组标题出现动画在 reveal 时刻排定（控制台式连续输出）
                    StoredItemGroup group = groups[revealedGroupCount];
                    group.appearTime = now;
                    // 下一组：均匀分布到"预估加载视觉结束前最后一条开始动画"
                    int remaining = groups.Sum(g => g.pendingItems.Count);
                    float itemProgress = totalPendingCount <= 0 ? 1f : Mathf.Clamp01((totalPendingCount - remaining) / (float)totalPendingCount);
                    float totalEstimate = (now - loadStartTime) / Mathf.Max(0.001f, itemProgress) + ModernExpandMenuMod.Settings.extraLoadingBarSeconds;
                    float visualEnd = loadStartTime + totalEstimate;
                    float lastStart = visualEnd - CurrentItemAppearDuration;
                    int remainingGroups = groups.Count - revealedGroupCount;
                    nextGroupRevealTime = now + (remainingGroups > 0 ? Mathf.Max(0.03f, (lastStart - now) / remainingGroups) : 0.1f);
                    revealedGroupCount++;
                }
            }

            // ── 加载 / 滚动流程 ──────────────────────────────
            // 加载中 / 额外显示：滚动跟随底部（控制台效果，看到最新出现的组标题）。
            // 窗口高度扩到上限（约 12 组）前 maxScroll=0 无滚动（只扩高）；超上限后跟随底部滚动插入。
            // 加载条跑完（extraEndTime 到）→ ShowLoadingVisual 结束 → 直接进入回顶步骤。
            float maxScroll = Mathf.Max(0f, TotalViewHeight - windowRect.height);
            if (isLoading || (extraEndTime >= 0f && now < extraEndTime))
            {
                scrollPosition.y = AnimationsEnabled
                    ? Mathf.MoveTowards(scrollPosition.y, maxScroll, Time.deltaTime * CurrentScrollFollowSpeed)
                    : maxScroll;
                returnToTopPending = true;
            }

            // 加载视觉结束后：按时间设定滚回顶端（ease-out-cubic，固定时长而非固定速度）
            if (!ShowLoadingVisual && returnToTopPending)
            {
                if (AnimationsEnabled)
                {
                    if (scrollReturnStartTime < 0f)
                    {
                        scrollReturnStartTime = now;
                        scrollReturnStartPos = scrollPosition.y;
                    }
                    float t = Mathf.Clamp01((now - scrollReturnStartTime) / CurrentScrollReturnDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);   // ease-out-cubic
                    scrollPosition.y = Mathf.Lerp(scrollReturnStartPos, 0f, eased);
                    if (t >= 1f)
                    {
                        scrollPosition.y = 0f;
                        returnToTopPending = false;
                        scrollReturnStartTime = -1f;
                    }
                }
                else
                {
                    // 动画关闭：直接回顶
                    scrollPosition.y = 0f;
                    returnToTopPending = false;
                    scrollReturnStartTime = -1f;
                }
            }

            // 展开 / 折叠动画
            bool animating = false;
            foreach (StoredItemGroup group in groups)
            {
                float target = expandedTargets.Contains(group) ? 1f : 0f;
                float current = expandProgress.TryGetValue(group, out float value) ? value : 0f;
                float next = AnimationsEnabled
                    ? Mathf.MoveTowards(current, target, Time.deltaTime * CurrentExpandAnimationSpeed)
                    : target;
                if (Mathf.Abs(next - target) > 0.001f)
                {
                    animating = true;
                }
                expandProgress[group] = next;
            }

            // 窗口高度动态动画：加载时组项插入 / 展开折叠时内容变化，高度平滑过渡；
            // 扩高上限 = 用户设置的最大高度（可调高），达到前只扩高窗口，超过后内容改为滚动
            float targetHeight = Mathf.Min(TotalViewHeight, CurrentMaxMenuHeight);
            if (Mathf.Abs(windowRect.height - targetHeight) > 0.5f)
            {
                windowRect.height = AnimationsEnabled
                    ? Mathf.MoveTowards(windowRect.height, targetHeight, Time.deltaTime * CurrentWindowHeightAnimationSpeed)
                    : targetHeight;
                if (windowRect.yMax > Verse.UI.screenHeight)
                {
                    windowRect.y = Verse.UI.screenHeight - windowRect.height - 4f;
                }
                float heightMaxScroll = Mathf.Max(0f, TotalViewHeight - windowRect.height);
                scrollPosition.y = Mathf.Min(scrollPosition.y, heightMaxScroll);
            }
            else if (animating)
            {
                // 高度已到位但展开动画仍在进行（防止滚动位置越界）
                float heightMaxScroll = Mathf.Max(0f, TotalViewHeight - windowRect.height);
                scrollPosition.y = Mathf.Min(scrollPosition.y, heightMaxScroll);
            }
        }

        /// <summary>
        /// 绘制窗口内容：背景卡片 + 滚动视口内按物品分组绘制。
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            // 每帧重置 tooltip（由滚动视口内的绘制更新），避免残留上一帧状态
            hoveredTooltipText = null;

            // 高亮右键的容器（类似原版右键目标的白框效果，用户要求保留；物品高亮圆圈已按需求删除）
            if (highlightStorage != null && highlightStorage.Spawned)
            {
                foreach (IntVec3 cell in highlightStorage.AllSlotCells())
                {
                    GenDraw.DrawTargetHighlightWithLayer(cell, AltitudeLayer.Building);
                }
            }

            // 加载中：拒绝所有与菜单的交互（滚轮滚动、点击、hover 等）。
            // 在滚动视口处理之前消费滚轮事件，避免加载中内容被用户滚动
            bool blockInteraction = ShowLoadingVisual && ModernExpandMenuMod.Settings.showLoadingAnimation && AnimationsEnabled;
            if (blockInteraction && Event.current.type == EventType.ScrollWheel)
            {
                Event.current.Use();
            }

            // MD3 表面卡片背景 + 窗口外框描边（上下左右，MD3 Outline token）
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);
            MD3Widgets.DrawRoundedRectOutline(inRect, MD3Theme.Outline, MD3Theme.WindowCornerRadius, 1f, MD3Theme.Surface);

            // 内容视口从加载条下方开始（避免组与加载条重叠；组边框在 DrawGroups 内绘制）
            float loadingBarTop = MD3Theme.Padding + LoadingBarHeight + LoadingBarGap;
            var contentViewRect = new Rect(inRect.x, inRect.y + loadingBarTop, inRect.width, inRect.height - loadingBarTop);
            float viewWidth = inRect.width;
            float viewHeight = TotalViewHeight;
            var viewRect = new Rect(0f, 0f, viewWidth, viewHeight);

            // 关闭默认滚动条，改用自己的浅色细滚动条（U3D 默认滚动条太丑）
            Widgets.BeginScrollView(contentViewRect, ref scrollPosition, viewRect, showScrollbars: false);
            DrawGroups(viewRect);
            Widgets.EndScrollView();
            DrawCustomScrollbar(contentViewRect, viewHeight);

            // 分帧生成操作期间：内容上方覆盖半透明层 + 中央百分比，并拦截点击
            if (blockInteraction)
            {
                float progress = ComputeLoadProgress();
                // 覆盖层调浅，让"逐组展开"的加载动画透出可见（仍拦截交互）
                MD3Widgets.DrawRoundedRect(inRect, new Color(0f, 0f, 0f, 0.25f), MD3Theme.WindowCornerRadius);
                // 中央环形进度（保留环形本身，无发光/呼吸/亮点）+ 下方百分比
                var ringRect = new Rect(inRect.center.x - 24f, inRect.center.y - 32f, 48f, 48f);
                DrawProgressRing(ringRect, progress);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = MD3Theme.OnSurface;
                Widgets.Label(new Rect(ringRect.x - 20f, ringRect.yMax + 2f, ringRect.width + 40f, 20f), Mathf.RoundToInt(progress * 100f) + "%");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                // 透明按钮拦截所有鼠标点击（加载完成前拒绝与菜单交互）
                Widgets.ButtonInvisible(inRect);
            }

            // 顶部加载条（最顶层绘制：置顶不被内容 / 覆盖层遮挡，常驻显示）
            if (ModernExpandMenuMod.Settings.showLoadingAnimation && AnimationsEnabled)
            {
                float progress = ComputeLoadProgress();
                var topBarRect = new Rect(inRect.x + MD3Theme.Padding, inRect.y + MD3Theme.Padding, inRect.width - MD3Theme.Padding * 2f, LoadingBarHeight);
                DrawLoadingBar(topBarRect, progress);

                // 加载视觉结束后才开放 tooltip：悬停加载条显示加载统计（加载中完全不参与交互，连 tooltip 也没有）
                if (!ShowLoadingVisual && Mouse.IsOver(topBarRect))
                {
                    SetHoveredTooltip(BuildLoadingStatsText());
                }
            }

            // MD3 自绘 tooltip（替代原版 TooltipHandler）
            DrawMd3Tooltip();
        }

        /// <summary>逐物品分组绘制：每个组用一个描边框包住（标题 + 子项目），内容绘制在框内。</summary>
        private void DrawGroups(Rect viewRect)
        {
            // 视口已避开加载条（DoWindowContents 从加载条下方开始），内容顶部留白即可
            float y = MD3Theme.Padding;
            float contentWidth = viewRect.width - MD3Theme.Padding * 2f;
            int count = Mathf.Min(revealedGroupCount, groups.Count);
            for (int i = 0; i < count; i++)
            {
                StoredItemGroup group = groups[i];
                float groupTop = y;
                float progress = GetExpandProgress(group);
                // 组外框高度 = 标题 + 已出现动画的子项目（未到动画时间的下一个不占高，组动画期间子项未排定为 0）
                float groupHeight = MD3Theme.GroupHeaderHeight + ComputeActionsHeight(group, contentWidth);

                // 组外框：一个框把整组包在里面（描边环 + 表面背景），组内容随后绘制覆盖其上
                MD3Widgets.DrawRoundedRectOutline(new Rect(MD3Theme.Padding, groupTop, contentWidth, groupHeight), MD3Theme.Outline, 6f, 1f, MD3Theme.Surface);

                y = DrawGroupHeader(viewRect, group, y, contentWidth);
                if (progress > 0.001f)
                {
                    y = DrawGroupActions(viewRect, group, y, progress, contentWidth);
                }
                y += MD3Theme.GroupGap;
            }
        }

        /// <summary>绘制物品组标题（ClickGUI 面板风格）：图标 + 名称 + 数量 + 展开箭头，点击切换折叠。</summary>
        private float DrawGroupHeader(Rect viewRect, StoredItemGroup group, float y, float contentWidth)
        {
            bool expanded = expandedTargets.Contains(group);
            var headerRect = new Rect(MD3Theme.Padding, y, contentWidth, MD3Theme.GroupHeaderHeight);

            // 大类出现/消失动画（与子项目一致）：滚动进出可视范围时播放，组内串行、组间并行。
            // 动画未到时整块（背景/图标/文本）不显示（alpha 随动画渐变）
            BlockAnim anim = ComputeBlockAnim(ref group.appearTime, ref group.disappearTime, ref group.hasAppeared, y, headerRect.height, group);
            var drawRect = headerRect;
            drawRect.x += anim.offsetX;
            drawRect.y += anim.offsetY;

            Color headerBackground = MD3Theme.SurfaceContainerHigh;
            headerBackground.a *= anim.alpha;
            MD3Widgets.DrawRoundedRect(drawRect, headerBackground, MD3Theme.HeaderCornerRadius);

            // 物品图标（"其他"组无代表物品，不画图标；动画未到时 alpha 隐藏）
            float labelStartX;
            if (group.representativeThing != null)
            {
                var iconRect = new Rect(drawRect.x + 8f, drawRect.y + 4f, 26f, 26f);
                GUI.color = new Color(1f, 1f, 1f, anim.alpha);
                Widgets.ThingIcon(iconRect, group.representativeThing);
                GUI.color = Color.white;
                labelStartX = drawRect.x + 42f;
            }
            else
            {
                labelStartX = drawRect.x + 12f;
            }

            // 名称 + 数量（右侧预留箭头空间）
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;   // 单行截断，避免换行后行高不够被裁
            Color titleColor = MD3Theme.OnSurface;
            titleColor.a *= anim.alpha;
            GUI.color = titleColor;
            string titleText = group.headerLabel.CapitalizeFirst();
            if (ModernExpandMenuMod.Settings.showItemCount && group.totalCount > 0)
            {
                titleText += " ×" + group.totalCount;
            }
            Widgets.Label(new Rect(labelStartX, drawRect.y, drawRect.xMax - labelStartX - 26f, drawRect.height), titleText);
            Text.WordWrap = true;

            // 展开 / 折叠箭头（ClickGUI 风格指示）
            Text.Anchor = TextAnchor.MiddleRight;
            string arrowText = expanded ? "▾" : "▸";
            GUI.color = new Color(1f, 1f, 1f, anim.alpha);
            Widgets.Label(new Rect(drawRect.xMax - 24f, drawRect.y, 18f, drawRect.height), arrowText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // hover 状态层 + MD3 自绘 tooltip（名称可能被截断，悬停显示完整信息；加载中完全不参与任何交互，连 tooltip 也没有）
            if (!ShowLoadingVisual && Mouse.IsOver(headerRect))
            {
                MD3Widgets.DrawHoverState(headerRect, MD3Theme.HeaderCornerRadius);
                SetHoveredTooltip(titleText);
            }

            // 点击标题栏切换展开 / 折叠（加载期间锁定交互）
            if (!ShowLoadingVisual && Widgets.ButtonInvisible(headerRect))
            {
                ToggleGroupCollapsed(group);
            }

            return y + headerRect.height;
        }

        /// <summary>绘制某物品分组下的子菜单操作项（缩进体现层级，按动画进度裁剪，长文本自动换行）。</summary>
        private float DrawGroupActions(Rect viewRect, StoredItemGroup group, float y, float progress, float contentWidth)
        {
            // 高度只统计"已开始动画"的子项目（逐项占高：下一个子项目未到动画时间前不占高不绘制）
            float visibleHeight = ComputeActionsHeight(group, contentWidth);
            var clipRect = new Rect(MD3Theme.Padding, y, contentWidth, visibleHeight);

            GUI.BeginGroup(clipRect);
            float innerY = 0f;
            float now = Time.realtimeSinceStartup;
            foreach (ItemActionEntry entry in group.actions)
            {
                float rowHeight = GetActionRowHeight(entry, contentWidth);
                // 组展开动画完全结束后才开始加载子项目；未排定 / 未到动画时间的下一个子项不占高不绘制
                BlockAnim anim;
                if (progress < 0.999f)
                {
                    anim = BlockAnim.Hidden;   // 组动画中：子项目不参与
                }
                else
                {
                    float entryGlobalTop = y + innerY;
                    anim = ComputeBlockAnim(ref entry.appearTime, ref entry.disappearTime, ref entry.hasAppeared, entryGlobalTop, rowHeight, group);
                    if (now < entry.appearTime)
                    {
                        continue;   // 下一个子项目还没到动画时间：不占高不绘制
                    }
                }
                if (anim.alpha <= 0f)
                {
                    continue;
                }
                var rowRect = new Rect(
                    MD3Theme.ActionIndent,
                    innerY,
                    clipRect.width - MD3Theme.ActionIndent,
                    rowHeight);
                // BeginGroup 内 Event.current.mousePosition 是局部坐标，
                // 因此 Mouse.IsOver / ButtonInvisible / TipRegion 全部用局部 rowRect（与原版 FloatMenu 一致）

                // hover 状态层（加载完成前不响应；动画未到时也不响应）
                bool isHovering = !ShowLoadingVisual && anim.alpha > 0.5f && Mouse.IsOver(rowRect);
                if (isHovering)
                {
                    MD3Widgets.DrawHoverState(rowRect, MD3Theme.ActionCornerRadius);
                }

                // 操作行：动画位移（出现/消失）+ 全元素 alpha（动画未到时图标与组项不显示）
                var drawRect = rowRect;
                drawRect.x += anim.offsetX;
                drawRect.y += anim.offsetY;
                Color barColor = MD3Theme.Primary;
                barColor.a *= anim.alpha;
                MD3Widgets.DrawRoundedRect(new Rect(drawRect.x + 1f, drawRect.y + 4f, 3f, drawRect.height - 8f), barColor, 1.5f);
                if (entry.targetThing != null)
                {
                    var iconRect = new Rect(drawRect.x + 8f, drawRect.y + (drawRect.height - 18f) / 2f, 18f, 18f);
                    GUI.color = new Color(1f, 1f, 1f, anim.alpha);
                    Widgets.ThingIcon(iconRect, entry.targetThing);
                    GUI.color = Color.white;
                }

                // 操作文本（disabled 项灰色显示，label 已含不可执行原因）；
                // 自动换行 + 动态行高，长文本完整显示不被截断；
                // 动画透明度（未到时隐藏）
                Color textColor = entry.disabled ? MD3Theme.DisabledText : MD3Theme.OnSurface;
                textColor.a *= anim.alpha;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = true;
                GUI.color = textColor;
                Widgets.Label(new Rect(drawRect.x + 32f, drawRect.y, drawRect.width - 42f, drawRect.height), entry.label);
                Text.WordWrap = true;   // 恢复默认值，RimWorld 要求帧结束时 WordWrap 必须为 true
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                // 超长文本悬停显示完整内容（MD3 自绘 tooltip）
                if (isHovering)
                {
                    SetHoveredTooltip(entry.label);
                }

                // 点击执行（加载完成前锁定）：先关闭窗口再执行动作，
                // 避免窗口干扰动作内的 Targeter 目标选择 / 对话框
                if (!ShowLoadingVisual && Widgets.ButtonInvisible(rowRect) && !entry.disabled && entry.action != null)
                {
                    Close();
                    entry.action();
                }

                innerY += rowHeight;
            }
            GUI.EndGroup();

            return y + visibleHeight;
        }

        /// <summary>操作项文本可用宽度（用于换行计算，扣除竖条与图标占位）。</summary>
        private float GetActionTextWidth(float contentWidth)
        {
            return contentWidth - MD3Theme.ActionIndent - 46f;
        }

        /// <summary>单个操作项的行高：单行高度与自动换行高度的较大值（底部留缓冲，文字不贴框）。</summary>
        private float GetActionRowHeight(ItemActionEntry entry, float contentWidth)
        {
            Text.Font = GameFont.Small;
            Text.WordWrap = true;   // 必须开启换行，否则 CalcHeight 按单行计算导致行高偏小
            float wrappedHeight = Text.CalcHeight(entry.label, GetActionTextWidth(contentWidth));
            return Mathf.Max(MD3Theme.ItemRowHeight, wrappedHeight + 2f);
        }

        /// <summary>某分组"已开始动画"的子项目总高度（下一个子项目未到动画时间前不占高，逐项占高）。</summary>
        private float ComputeActionsHeight(StoredItemGroup group, float contentWidth)
        {
            float height = 0f;
            float now = Time.realtimeSinceStartup;
            foreach (ItemActionEntry entry in group.actions)
            {
                if (entry.appearTime >= 0f && now >= entry.appearTime)
                {
                    height += GetActionRowHeight(entry, contentWidth);
                }
            }
            return height;
        }

        /// <summary>计算滚动视口的内容总高度（视口已避开加载条；逐组展开时只统计已展开的组）。</summary>
        private float ComputeContentHeight()
        {
            float contentWidth = MD3Theme.MenuWidth - MD3Theme.Padding * 2f;
            // 顶部留白（视口从加载条下方开始，与 DrawGroups 内容起点一致）
            float height = MD3Theme.Padding;
            int count = Mathf.Min(revealedGroupCount, groups.Count);
            for (int i = 0; i < count; i++)
            {
                StoredItemGroup group = groups[i];
                height += MD3Theme.GroupHeaderHeight;
                // 只统计已出现动画的子项目（组动画期间子项未排定为 0，不提前占高）
                height += ComputeActionsHeight(group, contentWidth);
                height += MD3Theme.GroupGap;
            }
            return height;
        }

        /// <summary>当前分组的展开动画进度（0~1）。</summary>
        private float GetExpandProgress(StoredItemGroup group)
        {
            return expandProgress.TryGetValue(group, out float value) ? value : 0f;
        }

        /// <summary>点击分组标题时切换展开 / 折叠（由 WindowUpdate 驱动动画）。</summary>
        private void ToggleGroupCollapsed(StoredItemGroup group)
        {
            if (expandedTargets.Contains(group))
            {
                expandedTargets.Remove(group);
            }
            else
            {
                expandedTargets.Add(group);
                // 展开时：重置该组子项目的出现/消失动画（重新排定 appearTime = -1）。
                // 绘制时按"展开裁剪内 + 滚动可视范围内"调度，实现逐条从底端插入
                foreach (ItemActionEntry entry in group.actions)
                {
                    entry.appearTime = -1f;
                    entry.disappearTime = -1f;
                    entry.hasAppeared = false;
                }
            }
        }

        /// <summary>
        /// 组内串行排定一次出现动画：该组上一项就位（动画播完）后，下一项才开始（含可配置间隔）。
        /// 每组独立调度，组与组之间并行，互不等待。
        /// </summary>
        private float ScheduleAppear(StoredItemGroup group)
        {
            float now = Time.realtimeSinceStartup;
            float endTime = groupNextAppearEndTime.TryGetValue(group, out float value) ? value : -1f;
            float startTime = Mathf.Max(now, endTime);
            groupNextAppearEndTime[group] = startTime + CurrentItemAppearDuration + CurrentItemAppearInterval;
            return startTime;
        }

        /// <summary>单个块（分组标题 / 操作项）的动画状态：透明度 + 位移偏移。</summary>
        private struct BlockAnim
        {
            public float alpha;
            public float offsetX;
            public float offsetY;

            /// <summary>完全隐藏（展开裁剪外 / 动画未到时）。</summary>
            public static BlockAnim Hidden => new BlockAnim { alpha = 0f, offsetX = 0f, offsetY = 0f };
        }

        /// <summary>
        /// 计算单个块（分组标题 / 操作项）的出现/消失动画状态：
        ///   - 出现：未出现且过半可见 → 组内串行排定；出现动画"先水平从左向右滑入（底部位置），再垂直归位"
        ///   - 消失：已出现且不足半可见（滚出视口）→ 排定消失；淡出 + 轻微下滑
        ///   - 动画未到时 alpha=0（图标与组项不显示）
        /// </summary>
        private BlockAnim ComputeBlockAnim(ref float appearTime, ref float disappearTime, ref bool hasAppeared, float top, float height, StoredItemGroup group)
        {
            // 动画总开关关闭：直接显示最终状态（无出现/消失动画）
            if (!AnimationsEnabled)
            {
                appearTime = 0f;
                disappearTime = -1f;
                hasAppeared = true;
                return new BlockAnim { alpha = 1f, offsetX = 0f, offsetY = 0f };
            }

            float now = Time.realtimeSinceStartup;
            float viewTop = scrollPosition.y;
            float viewBottom = viewTop + windowRect.height;
            float visiblePart = Mathf.Max(0f, Mathf.Min(top + height, viewBottom) - Mathf.Max(top, viewTop));
            float visibleRatio = Mathf.Clamp01(visiblePart / Mathf.Max(1f, height));
            float duration = CurrentItemAppearDuration;

            // 出现：未出现且过半可见 → 组内串行排定
            if (!hasAppeared && visibleRatio > 0.5f && appearTime < 0f)
            {
                appearTime = ScheduleAppear(group);
            }
            float appearProgress = appearTime < 0f ? 1f : Mathf.Clamp01((now - appearTime) / duration);
            if (appearTime >= 0f && appearProgress >= 1f && visibleRatio > 0.5f)
            {
                hasAppeared = true;   // 出现完成，进入稳定态
            }

            // 消失：已出现且不足半可见（滚出过半）→ 排定消失
            if (hasAppeared && visibleRatio < 0.5f && disappearTime < 0f)
            {
                disappearTime = now;
            }
            float disappearProgress = disappearTime < 0f ? 0f : Mathf.Clamp01((now - disappearTime) / duration);
            if (disappearTime >= 0f && disappearProgress >= 1f)
            {
                // 消失完成：重置，下次滚回时重新播放出现动画
                hasAppeared = false;
                disappearTime = -1f;
                appearTime = -1f;
            }

            var anim = new BlockAnim();
            if (hasAppeared && disappearTime >= 0f)
            {
                // 消失中：淡出 + 轻微下滑
                anim.alpha = 1f - disappearProgress;
                anim.offsetX = 0f;
                anim.offsetY = (1f - disappearProgress) * height * 0.4f;
            }
            else if (!hasAppeared && appearTime >= 0f)
            {
                // 出现中：纯水平从左向右滑入（不做垂直偏移，避免"左下→右上"对角线）
                anim.alpha = appearProgress;
                anim.offsetX = -(1f - appearProgress) * 20f;
                anim.offsetY = 0f;
            }
            else if (!hasAppeared)
            {
                // 尚未排定出现动画：完全隐藏（图标 / 组项不提前出现）
                anim.alpha = 0f;
                anim.offsetX = 0f;
                anim.offsetY = 0f;
            }
            else
            {
                anim.alpha = 1f;
                anim.offsetX = 0f;
                anim.offsetY = 0f;
            }
            return anim;
        }

        /// <summary>构建加载统计 tooltip 文本（加载完成后悬停加载条显示）。</summary>
        private string BuildLoadingStatsText()
        {
            return "ModernExpandMenu_LoadingStatsTooltip".Translate(
                totalPendingCount,
                finalActionCount,
                loadDurationSeconds.ToString("0.00"),
                (loadDurationSeconds + ModernExpandMenuMod.Settings.extraLoadingBarSeconds).ToString("0.00"));
        }

        /// <summary>按当前内容高度限制滚动位置不越界（窗口高度由 WindowUpdate 平滑过渡，动态动画）。</summary>
        private void RefreshWindowHeight()
        {
            float targetHeight = Mathf.Min(TotalViewHeight, CurrentMaxMenuHeight);
            float maxScroll = Mathf.Max(0f, TotalViewHeight - targetHeight);
            scrollPosition.y = Mathf.Min(scrollPosition.y, maxScroll);
        }

        /// <summary>
        /// 绘制深灰细滚动条（仅在内容超出视口时显示），替代 U3D 默认滚动条。
        /// 轨道上下缩进避开窗口圆角；支持鼠标按住滑块拖动。
        /// </summary>
        private void DrawCustomScrollbar(Rect inRect, float viewHeight)
        {
            // 加载期间隐藏并禁用滚动条（覆盖层锁定交互）
            if (ShowLoadingVisual)
            {
                draggingScrollbar = false;
                return;
            }
            if (viewHeight <= inRect.height)
            {
                draggingScrollbar = false;
                return;
            }
            // 轨道上下各缩进一个圆角半径，完全避开四角圆角区域
            float cornerInset = MD3Theme.WindowCornerRadius;
            float trackTop = inRect.y + cornerInset;
            float trackHeight = inRect.height - cornerInset * 2f;
            float thumbHeight = Mathf.Max(24f, trackHeight * trackHeight / viewHeight);
            float maxScroll = Mathf.Max(1f, viewHeight - inRect.height);
            float thumbOffset = (trackHeight - thumbHeight) * (scrollPosition.y / maxScroll);
            var trackRect = new Rect(inRect.xMax - MD3Theme.ScrollbarWidth - 3f, trackTop, MD3Theme.ScrollbarWidth, trackHeight);
            var thumbRect = new Rect(trackRect.x, trackRect.y + thumbOffset, MD3Theme.ScrollbarWidth, thumbHeight);

            // 鼠标拖动滑块：按住滑块 → 跟随鼠标更新滚动位置
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(thumbRect))
            {
                draggingScrollbar = true;
                Event.current.Use();
            }
            if (draggingScrollbar)
            {
                if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown)
                {
                    float mouseY = Event.current.mousePosition.y;
                    float newOffset = mouseY - trackRect.y - thumbHeight / 2f;
                    float clampedOffset = Mathf.Clamp(newOffset, 0f, trackHeight - thumbHeight);
                    scrollPosition.y = clampedOffset / (trackHeight - thumbHeight) * maxScroll;
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    draggingScrollbar = false;
                    Event.current.Use();
                }
            }

            // 绘制（拖动时滑块高亮）
            MD3Widgets.DrawRoundedRect(trackRect, MD3Theme.ScrollbarTrack, MD3Theme.ScrollbarWidth / 2f);
            MD3Widgets.DrawRoundedRect(thumbRect, draggingScrollbar ? MD3Theme.ScrollbarThumbDragging : MD3Theme.ScrollbarThumb, MD3Theme.ScrollbarWidth / 2f);
        }

        /// <summary>
        /// 分帧生成操作：每帧处理有限数量的待生成物品，避免大量物品时单帧卡死。
        /// 全部完成后最终化分组（过滤 / 排序），并停止加载动画。
        /// </summary>
        private void ProcessPendingActions()
        {
            if (!isLoading)
            {
                return;
            }
            int budget = CurrentMaxProcessedPerFrame;
            foreach (StoredItemGroup group in groups)
            {
                while (group.pendingItems.Count > 0 && budget > 0)
                {
                    Thing item = group.pendingItems[0];
                    group.pendingItems.RemoveAt(0);
                    // 生成操作项（不预排出现动画：由绘制时按"可见范围"串行排定，
                    // 加载时滚动跟随底部逐条出现，加载后滚动进入可视区也重新触发）
                    StoredItemGroup.CollectBasicActionsForItem(group, item, savedContext);
                    budget--;
                }
                if (budget <= 0)
                {
                    break;
                }
            }
            if (groups.All(group => group.pendingItems.Count == 0))
            {
                isLoading = false;
                loadingFinishedAt = Time.realtimeSinceStartup;   // 记录完成时间，供"额外显示进度条"判断
                extraEndTime = loadingFinishedAt + ModernExpandMenuMod.Settings.extraLoadingBarSeconds;   // 额外显示结束时间
                loadDurationSeconds = loadingFinishedAt - loadStartTime;   // 加载实际耗时（统计用）
                StoredItemGroup.FinalizeGroups(groups);
                finalActionCount = groups.Sum(group => group.actions.Count);   // 最终操作项总数（统计用）
                // 加载条跑完后（extraEndTime 到）由 WindowUpdate 进入"平滑回顶"步骤
            }
            RefreshWindowHeight();
        }

        /// <summary>
        /// 当前加载进度：额外时间是总时长的组成部分，全程线性推进（无 90% 分段）。
        /// 加载中按"已用时间 / 预估总时长"计算；加载完成后按"已用时间 / (处理时长+额外)"匀速到 100%。
        /// </summary>
        private float ComputeLoadProgress()
        {
            float extraSeconds = ModernExpandMenuMod.Settings.extraLoadingBarSeconds;
            float elapsed = Time.realtimeSinceStartup - loadStartTime;

            if (isLoading)
            {
                // 加载中：预估总时长 = 处理时长(外推) + 额外时间
                int remaining = groups.Sum(group => group.pendingItems.Count);
                float itemProgress = totalPendingCount <= 0 ? 1f : Mathf.Clamp01((totalPendingCount - remaining) / (float)totalPendingCount);
                float totalEstimate = elapsed / Mathf.Max(0.001f, itemProgress) + extraSeconds;
                return Mathf.Clamp01(elapsed / totalEstimate);
            }

            // 加载完成：总时长固定 = 处理时长 + 额外时间，额外时间内匀速走到 100%
            float totalDuration = (loadingFinishedAt - loadStartTime) + extraSeconds;
            if (totalDuration <= 0f)
            {
                return 1f;
            }
            return Mathf.Clamp01(elapsed / totalDuration);
        }

        /// <summary>
        /// 绘制窗口顶端的加载条（纯色无发光）：轨道 + 已完成段（主色）+ 前端 + 缓冲段（半透明）。
        /// </summary>
        private void DrawLoadingBar(Rect barRect, float progress)
        {
            // 轨道
            MD3Widgets.DrawRoundedRect(barRect, MD3Theme.SurfaceContainer, 2f);

            // 已完成段（主色）
            float fillWidth = barRect.width * progress;
            if (fillWidth > 1f)
            {
                MD3Widgets.DrawRoundedRect(new Rect(barRect.x, barRect.y, fillWidth, barRect.height), MD3Theme.Primary, 2f);
            }

            // 前端（随加载进度前移）
            float tipWidth = Mathf.Min(20f, barRect.width * 0.12f);
            float tipX = barRect.x + fillWidth - tipWidth;
            if (progress < 1f && tipX + tipWidth <= barRect.xMax)
            {
                MD3Widgets.DrawRoundedRect(new Rect(tipX, barRect.y, tipWidth, barRect.height), MD3Theme.Primary, 2f);
            }

            // 缓冲段（填充前端之后）：半透明
            float bufferWidth = barRect.width * 0.15f;
            if (progress < 1f && fillWidth + bufferWidth < barRect.width)
            {
                var bufferRect = new Rect(barRect.x + fillWidth, barRect.y, bufferWidth, barRect.height);
                MD3Widgets.DrawRoundedRect(bufferRect, new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 0.3f), 2f);
            }
        }

        /// <summary>
        /// 绘制平滑环形加载进度：192x192 逐像素生成圆环纹理（内外半径间按角度填充），
        /// 4x4 子采样抗锯齿，从 12 点（顶部）开始顺时针。
        /// 保留环形本身，无发光 / 呼吸脉冲 / 前端亮点（静态显示）。
        /// </summary>
        private void DrawProgressRing(Rect rect, float progress)
        {
            const int size = 192;              // 高分辨率减少锯齿
            const int subSamples = 4;          // 4x4 子采样抗锯齿
            if (ringTexture == null)
            {
                ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                ringTexture.wrapMode = TextureWrapMode.Clamp;
                ringTexture.filterMode = FilterMode.Bilinear;
            }
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float inner = size * 0.31f;
            float outer = size * 0.46f;
            float fillAngleEnd = Mathf.Clamp01(progress) * 360f;
            Color fillColor = new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 1f);
            Color trackColor = new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 0.15f);
            if (ringPixels == null)
            {
                ringPixels = new Color[size * size];   // 复用缓冲，避免每帧 new 大数组
            }

            // 节流重建：仅每隔一小段时间重新生成像素并上传 GPU（SetPixels + Apply），
            // 避免每帧重建造成大量 GC 分配与 GPU 上传（帧率下降的根因）
            float subStep = 1f / subSamples;
            if (Time.realtimeSinceStartup - lastRingRebuildTime >= RingRebuildIntervalSeconds)
            {
                for (int screenY = 0; screenY < size; screenY++)
                {
                    // 屏幕坐标 y 向下（顶部=12 点）；GUI 绘制时纹理 v 与屏幕 y 相反，写入时行翻转
                    int textureRow = size - 1 - screenY;
                    for (int x = 0; x < size; x++)
                    {
                        int index = textureRow * size + x;
                        int ringHits = 0;
                        int fillHits = 0;
                        for (int sy = 0; sy < subSamples; sy++)
                        {
                            for (int sx = 0; sx < subSamples; sx++)
                            {
                                // 屏幕坐标点（顶部 screenY=0 处 y 为负 → 12 点）
                                Vector2 point = new Vector2(x + (sx + 0.5f) * subStep, screenY + (sy + 0.5f) * subStep) - center;
                                float distance = point.magnitude;
                                if (distance < inner || distance > outer)
                                {
                                    continue;
                                }
                                ringHits++;
                                float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
                                if (angle < 0f)
                                {
                                    angle += 360f;
                                }
                                // 从 12 点开始顺时针：顶部(0,-r)→0，右侧→90，底部→180，左侧→270
                                float fromTop = (angle + 90f + 360f) % 360f;
                                if (fromTop <= fillAngleEnd)
                                {
                                    fillHits++;
                                }
                            }
                        }
                        float totalSamples = subSamples * subSamples;
                        if (ringHits == 0)
                        {
                            ringPixels[index] = Color.clear;
                            continue;
                        }
                        // 边缘像素：按环覆盖率做 alpha 过渡（抗锯齿）+ 按填充比例混合颜色
                        float ringCoverage = ringHits / totalSamples;
                        float fillCoverage = fillHits / (float)ringHits;
                        Color blended = Color.Lerp(trackColor, fillColor, fillCoverage);
                        blended.a *= ringCoverage;
                        ringPixels[index] = blended;
                    }
                }
                ringTexture.SetPixels(ringPixels);
                ringTexture.Apply();
                lastRingRebuildTime = Time.realtimeSinceStartup;
            }

            // 绘制（静态显示，无发光/呼吸效果）
            GUI.color = Color.white;
            GUI.DrawTexture(rect, ringTexture);
            GUI.color = Color.white;
        }

        /// <summary>记录悬停项的 tooltip 文本（每帧首个悬停项生效）。</summary>
        private void SetHoveredTooltip(string text)
        {
            hoveredTooltipText = text;
            if (hoverStartTime < 0f)
            {
                hoverStartTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// 绘制 MD3 自绘 tooltip：深色圆角气泡 + 主色描边 + 左侧主色竖条点缀，
        /// 替代原版 TooltipHandler（灰白系统框）。延迟显示，鼠标附近，防溢出屏幕。
        /// </summary>
        private void DrawMd3Tooltip()
        {
            if (hoveredTooltipText.NullOrEmpty())
            {
                hoverStartTime = -1f;
                return;
            }
            // 延迟显示
            if (Time.realtimeSinceStartup - hoverStartTime < TooltipDelaySeconds)
            {
                return;
            }

            Text.Font = GameFont.Small;
            Text.WordWrap = true;
            float tooltipWidth = Mathf.Min(380f, Mathf.Max(80f, hoveredTooltipText.Length * 8f + 24f));
            float tooltipHeight = Text.CalcHeight(hoveredTooltipText, tooltipWidth - 20f) + 16f;

            Vector2 mousePosition = Verse.UI.MousePositionOnUIInverted;
            var tooltipRect = new Rect(mousePosition.x + 14f, mousePosition.y + 14f, tooltipWidth, tooltipHeight);
            tooltipRect.x = Mathf.Clamp(tooltipRect.x, 4f, Verse.UI.screenWidth - tooltipRect.width - 4f);
            tooltipRect.y = Mathf.Clamp(tooltipRect.y, 4f, Verse.UI.screenHeight - tooltipRect.height - 4f);

            string tooltipText = hoveredTooltipText;
            Find.WindowStack.ImmediateWindow(Md3TooltipWindowId, tooltipRect, WindowLayer.Super, delegate
            {
                var localRect = tooltipRect.AtZero();
                // 深色圆角气泡 + 表面内衬 + 左侧主色竖条点缀
                MD3Widgets.DrawRoundedRect(localRect, MD3Theme.SurfaceContainerHigh, 6f);
                MD3Widgets.DrawRoundedRect(localRect.ContractedBy(1f), MD3Theme.Surface, 5f);
                MD3Widgets.DrawRoundedRect(new Rect(localRect.x + 4f, localRect.y + 5f, 3f, localRect.height - 10f), MD3Theme.Primary, 1.5f);

                Text.Font = GameFont.Small;
                Text.WordWrap = true;
                GUI.color = MD3Theme.OnSurface;
                Widgets.Label(new Rect(localRect.x + 12f, localRect.y + 6f, localRect.width - 20f, localRect.height - 12f), tooltipText);
                Text.WordWrap = true;   // 帧末必须为 true
                GUI.color = Color.white;
            }, doBackground: false, absorbInputAroundWindow: false, 0f);
        }

        /// <summary>
        /// 绘制从鼠标指向目标物品的发光箭头（原版 FloatMenu hover 同款效果）：
        /// 底层粗淡黄线（光晕）+ 上层细亮黄线 + 目标端箭头尖。
        /// </summary>
        private void DrawHoverArrow(Thing target)
        {
            Vector3 from = Verse.UI.MouseMapPosition();
            Vector3 to = target.DrawPos;
            Vector3 direction = (to - from).normalized;

            // 发光层：粗的半透明黄线
            GenDraw.DrawLineBetween(from, to, SimpleColor.Yellow, 0.8f);
            // 亮线层：细亮黄线
            GenDraw.DrawLineBetween(from, to, SimpleColor.Yellow, 0.25f);

            // 箭头尖：目标端两条短线构成 V 形
            Vector3 tipBase = to - direction * 1.2f;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * 0.45f;
            GenDraw.DrawLineBetween(tipBase + perpendicular, to, SimpleColor.Yellow, 0.25f);
            GenDraw.DrawLineBetween(tipBase - perpendicular, to, SimpleColor.Yellow, 0.25f);
        }
    }
}
