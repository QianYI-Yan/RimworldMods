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
        private Texture2D ringTexture;                         // 环形进度纹理（逐像素生成，平滑圆弧）
        private Thing hoveredTargetThing;                      // 当前悬停操作项对应的目标物品（滚动视口外统一绘制高亮）
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
        private float nextAppearTime;     // 下一条操作项的出现时间（控制台式逐条载入）
        private const float AppearInterval = 0.03f;            // 相邻操作项出现间隔（秒）
        private const float AppearDuration = 0.25f;            // 单条操作项渐入时长（秒）

        // 以下两项读取模组设置（游戏内"选项 → Mod 设置"可调）
        private static int CurrentMaxProcessedPerFrame => Mathf.Max(1, ModernExpandMenuMod.Settings.maxProcessedPerFrame);
        private static float CurrentMaxMenuHeight => ModernExpandMenuMod.Settings.maxMenuHeight;

        /// <summary>
        /// 是否显示加载视觉：分帧生成中，或生成完成后仍在"强制额外显示"时间内。
        /// 期间保持覆盖层与点击锁定（100% 进度条按设置的秒数继续展示）。
        /// </summary>
        private bool ShowLoadingVisual =>
            isLoading || (loadingFinishedAt >= 0f &&
                          Time.realtimeSinceStartup - loadingFinishedAt < ModernExpandMenuMod.Settings.extraLoadingBarSeconds);

        // 展开/折叠动画：目标状态 + 当前进度（0=收起，1=展开），key 为 ThingDef（"其他"组为 null）
        // 展开/折叠状态用 StoredItemGroup 对象作 key —— 对象引用永不为 null，
        // 避免"其他"组（representativeThing 为 null）导致 Dictionary null key 异常
        private readonly HashSet<StoredItemGroup> expandedTargets = new HashSet<StoredItemGroup>();
        private readonly Dictionary<StoredItemGroup, float> expandProgress = new Dictionary<StoredItemGroup, float>();
        private const float ExpandAnimationSpeed = 10f;

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

        /// <summary>内容高度 + 上下边距。</summary>
        private float TotalViewHeight => ComputeContentHeight() + MD3Theme.Padding * 2f;

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
            windowRect = new Rect(x, y, size.x, size.y);
        }

        /// <summary>每帧驱动展开/折叠动画，并随动画更新窗口高度。</summary>
        public override void WindowUpdate()
        {
            base.WindowUpdate();

            // 分帧生成操作项：每帧处理有限数量的物品实例，避免大量物品时一帧卡死
            ProcessPendingActions();

            bool animating = false;
            foreach (StoredItemGroup group in groups)
            {
                float target = expandedTargets.Contains(group) ? 1f : 0f;
                float current = expandProgress.TryGetValue(group, out float value) ? value : 0f;
                float next = Mathf.MoveTowards(current, target, Time.deltaTime * ExpandAnimationSpeed);
                if (Mathf.Abs(next - target) > 0.001f)
                {
                    animating = true;
                }
                expandProgress[group] = next;
            }
            if (animating)
            {
                RefreshWindowHeight();
            }
        }

        /// <summary>
        /// 绘制窗口内容：背景卡片 + 滚动视口内按物品分组绘制。
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            // 每帧重置悬停目标与 tooltip（由滚动视口内的绘制更新），避免残留上一帧状态
            hoveredTargetThing = null;
            hoveredTooltipText = null;

            // 持续高亮右键命中的物品（原版右键目标的白框效果），不改变当前选中状态
            foreach (Thing thing in highlightedItems)
            {
                if (thing != null && thing.Spawned)
                {
                    GenDraw.DrawTargetHighlight(thing);
                }
            }

            // 高亮右键的容器（类似原版右键目标的白框效果）
            if (highlightStorage != null && highlightStorage.Spawned)
            {
                foreach (IntVec3 cell in highlightStorage.AllSlotCells())
                {
                    GenDraw.DrawTargetHighlightWithLayer(cell, AltitudeLayer.Building);
                }
            }

            // 悬停操作项时：在滚动视口外统一绘制目标物品高亮 + 发光箭头
            // （世界空间绘制放到无 GUI 裁剪处，避免 BeginGroup 内调用导致的闪烁与"需按键输入才显示"问题）
            if (!ShowLoadingVisual && ModernExpandMenuMod.Settings.showHoverHighlightAndArrow &&
                hoveredTargetThing != null && hoveredTargetThing.Spawned)
            {
                GenDraw.DrawTargetHighlight(hoveredTargetThing);
                DrawHoverArrow(hoveredTargetThing);
            }

            // MD3 表面卡片背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 分帧生成操作期间：窗口顶端显示带缓冲动画的加载条（可在设置中关闭动画）
            if (ShowLoadingVisual && ModernExpandMenuMod.Settings.showLoadingAnimation)
            {
                float progress = ComputeLoadProgress();
                var topBarRect = new Rect(inRect.x + MD3Theme.Padding, inRect.y + MD3Theme.Padding, inRect.width - MD3Theme.Padding * 2f, 5f);
                DrawLoadingBar(topBarRect, progress);
            }

            // 内容全宽，左右边距在绘制时用 Padding 偏移；滚动条悬浮贴右缘，不占内容宽度
            float viewWidth = inRect.width;
            float viewHeight = TotalViewHeight;
            var viewRect = new Rect(0f, 0f, viewWidth, viewHeight);

            // 关闭默认滚动条，改用自己的浅色细滚动条（U3D 默认滚动条太丑）
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, showScrollbars: false);
            DrawGroups(viewRect);
            Widgets.EndScrollView();
            DrawCustomScrollbar(inRect, viewHeight);

            // 分帧生成操作期间：内容上方覆盖半透明层 + 中央环形进度 + 百分比（可在设置中关闭动画）
            if (ShowLoadingVisual && ModernExpandMenuMod.Settings.showLoadingAnimation)
            {
                float progress = ComputeLoadProgress();
                MD3Widgets.DrawRoundedRect(inRect, new Color(0f, 0f, 0f, 0.4f), MD3Theme.WindowCornerRadius);
                var ringRect = new Rect(inRect.center.x - 24f, inRect.center.y - 32f, 48f, 48f);
                DrawProgressRing(ringRect, progress);
                // 环形下方百分比
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = MD3Theme.OnSurface;
                Widgets.Label(new Rect(ringRect.x - 20f, ringRect.yMax + 2f, ringRect.width + 40f, 20f), Mathf.RoundToInt(progress * 100f) + "%");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // MD3 自绘 tooltip（替代原版 TooltipHandler）
            DrawMd3Tooltip();
        }

        /// <summary>逐物品分组绘制：组标题 + 子菜单操作项（含动画裁剪）。</summary>
        private void DrawGroups(Rect viewRect)
        {
            float y = MD3Theme.Padding;   // 顶部留白
            // 加载期间顶部让出加载条空间，避免第一个分组标题遮挡顶栏加载条
            if (ShowLoadingVisual && ModernExpandMenuMod.Settings.showLoadingAnimation)
            {
                y += 5f + 4f;
            }
            float contentWidth = viewRect.width - MD3Theme.Padding * 2f;
            foreach (StoredItemGroup group in groups)
            {
                y = DrawGroupHeader(viewRect, group, y, contentWidth);
                float progress = GetExpandProgress(group);
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
            MD3Widgets.DrawRoundedRect(headerRect, MD3Theme.SurfaceContainerHigh, MD3Theme.HeaderCornerRadius);

            // 物品图标（"其他"组无代表物品，不画图标）
            float labelStartX;
            if (group.representativeThing != null)
            {
                var iconRect = new Rect(headerRect.x + 8f, headerRect.y + 4f, 26f, 26f);
                Widgets.ThingIcon(iconRect, group.representativeThing);
                labelStartX = headerRect.x + 42f;
            }
            else
            {
                labelStartX = headerRect.x + 12f;
            }

            // 名称 + 数量（右侧预留箭头空间）
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;   // 单行截断，避免换行后行高不够被裁
            GUI.color = MD3Theme.OnSurface;
            string titleText = group.headerLabel.CapitalizeFirst();
            if (ModernExpandMenuMod.Settings.showItemCount && group.totalCount > 0)
            {
                titleText += " ×" + group.totalCount;
            }
            Widgets.Label(new Rect(labelStartX, headerRect.y, headerRect.xMax - labelStartX - 26f, headerRect.height), titleText);
            Text.WordWrap = true;

            // 展开 / 折叠箭头（ClickGUI 风格指示）
            Text.Anchor = TextAnchor.MiddleRight;
            string arrowText = expanded ? "▾" : "▸";
            Widgets.Label(new Rect(headerRect.xMax - 24f, headerRect.y, 18f, headerRect.height), arrowText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // hover 状态层 + MD3 自绘 tooltip（名称可能被截断，悬停显示完整信息）
            if (Mouse.IsOver(headerRect))
            {
                MD3Widgets.DrawHoverState(headerRect, MD3Theme.HeaderCornerRadius);
                SetHoveredTooltip(titleText);
            }

            // 点击标题栏切换展开 / 折叠
            if (Widgets.ButtonInvisible(headerRect))
            {
                ToggleGroupCollapsed(group);
            }

            return y + headerRect.height;
        }

        /// <summary>绘制某物品分组下的子菜单操作项（缩进体现层级，按动画进度裁剪，长文本自动换行）。</summary>
        private float DrawGroupActions(Rect viewRect, StoredItemGroup group, float y, float progress, float contentWidth)
        {
            float totalActionsHeight = ComputeActionsHeight(group, contentWidth);
            float visibleHeight = totalActionsHeight * progress;
            var clipRect = new Rect(MD3Theme.Padding, y, contentWidth, visibleHeight);

            // 裁剪区实现平滑展开：只显示顶部 progress 比例的操作项
            GUI.BeginGroup(clipRect);
            float innerY = 0f;
            foreach (ItemActionEntry entry in group.actions)
            {
                float rowHeight = GetActionRowHeight(entry, contentWidth);
                var rowRect = new Rect(
                    MD3Theme.ActionIndent,
                    innerY,
                    clipRect.width - MD3Theme.ActionIndent,
                    rowHeight);
                // BeginGroup 内 Event.current.mousePosition 是局部坐标，
                // 因此 Mouse.IsOver / ButtonInvisible / TipRegion 全部用局部 rowRect（与原版 FloatMenu 一致）

                // 左侧渐入动画进度（控制台式逐条载入到底端）
                float appearProgress = Mathf.Clamp01((Time.realtimeSinceStartup - entry.appearTime) / AppearDuration);

                // hover 状态层（加载完成前不响应）
                bool isHovering = !ShowLoadingVisual && Mouse.IsOver(rowRect);
                if (isHovering)
                {
                    MD3Widgets.DrawHoverState(rowRect, MD3Theme.ActionCornerRadius);
                }
                // 记录悬停目标物品：高亮与箭头统一在滚动视口外的 DoWindowContents 绘制
                // （避免在 BeginGroup 内调用世界空间绘制导致的闪烁 / 事件依赖问题）
                if (isHovering && ModernExpandMenuMod.Settings.showHoverHighlightAndArrow &&
                    entry.targetThing != null && entry.targetThing.Spawned)
                {
                    hoveredTargetThing = entry.targetThing;
                }

                // 操作行左侧：目标物品小图标（对应操作执行对象，原版悬浮菜单同款）
                var drawRect = rowRect;
                drawRect.x -= (1f - appearProgress) * 20f;
                if (entry.targetThing != null)
                {
                    var iconRect = new Rect(drawRect.x + 2f, drawRect.y + (drawRect.height - 18f) / 2f, 18f, 18f);
                    Widgets.ThingIcon(iconRect, entry.targetThing);
                }

                // 操作文本（disabled 项灰色显示，label 已含不可执行原因）；
                // 自动换行 + 动态行高，长文本完整显示不被截断；
                // 渐入动画：透明度渐变 + 左侧滑入偏移
                Color textColor = entry.disabled ? MD3Theme.DisabledText : MD3Theme.OnSurface;
                textColor.a *= appearProgress;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = true;
                GUI.color = textColor;
                Widgets.Label(new Rect(drawRect.x + 26f, drawRect.y, drawRect.width - 36f, drawRect.height), entry.label);
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

        /// <summary>操作项文本可用宽度（用于换行计算，扣除图标占位）。</summary>
        private float GetActionTextWidth(float contentWidth)
        {
            return contentWidth - MD3Theme.ActionIndent - 40f;
        }

        /// <summary>单个操作项的行高：单行高度与自动换行高度的较大值（底部留缓冲，文字不贴框）。</summary>
        private float GetActionRowHeight(ItemActionEntry entry, float contentWidth)
        {
            Text.Font = GameFont.Small;
            Text.WordWrap = true;   // 必须开启换行，否则 CalcHeight 按单行计算导致行高偏小
            float wrappedHeight = Text.CalcHeight(entry.label, GetActionTextWidth(contentWidth));
            return Mathf.Max(MD3Theme.ItemRowHeight, wrappedHeight + 2f);
        }

        /// <summary>某分组全部操作项的总高度（按各自换行高度累加）。</summary>
        private float ComputeActionsHeight(StoredItemGroup group, float contentWidth)
        {
            float height = 0f;
            foreach (ItemActionEntry entry in group.actions)
            {
                height += GetActionRowHeight(entry, contentWidth);
            }
            return height;
        }

        /// <summary>计算滚动视口的内容总高度（按动画进度 + 操作项换行高度）。</summary>
        private float ComputeContentHeight()
        {
            float contentWidth = MD3Theme.MenuWidth - MD3Theme.Padding * 2f;
            float height = 0f;
            foreach (StoredItemGroup group in groups)
            {
                height += MD3Theme.GroupHeaderHeight;
                height += ComputeActionsHeight(group, contentWidth) * GetExpandProgress(group);
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
            }
        }

        /// <summary>按当前内容高度重算窗口高度，并防止超出屏幕。</summary>
        private void RefreshWindowHeight()
        {
            float newHeight = Mathf.Min(TotalViewHeight, CurrentMaxMenuHeight);
            windowRect.height = newHeight;
            if (windowRect.yMax > Verse.UI.screenHeight)
            {
                windowRect.y = Verse.UI.screenHeight - newHeight - 4f;
            }
            float maxScroll = Mathf.Max(0f, TotalViewHeight - newHeight);
            scrollPosition.y = Mathf.Min(scrollPosition.y, maxScroll);
        }

        /// <summary>
        /// 绘制深灰细滚动条（仅在内容超出视口时显示），替代 U3D 默认滚动条。
        /// 轨道上下缩进避开窗口圆角；支持鼠标按住滑块拖动。
        /// </summary>
        private void DrawCustomScrollbar(Rect inRect, float viewHeight)
        {
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
                    int beforeCount = group.actions.Count;
                    StoredItemGroup.CollectBasicActionsForItem(group, item, savedContext);
                    // 为新生成的操作项排定出现时间（控制台式逐条载入到底端）
                    for (int i = beforeCount; i < group.actions.Count; i++)
                    {
                        group.actions[i].appearTime = nextAppearTime;
                        nextAppearTime += AppearInterval;
                    }
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
                loadingFinishedAt = Time.realtimeSinceStartup;   // 记录完成时间，供"强制额外显示进度条"判断
                StoredItemGroup.FinalizeGroups(groups);
                scrollPosition = Vector2.zero;   // 加载完成后滚动到顶端
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
        /// 绘制窗口顶端的加载条（脉冲型，仿 VS Code 输入框指示器）：
        /// 已完成段呼吸发光 + 前端脉冲亮点 + 缓冲段流动高光。
        /// </summary>
        private void DrawLoadingBar(Rect barRect, float progress)
        {
            float time = Time.realtimeSinceStartup;
            // 呼吸脉冲 0~1（正弦）
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 4f);

            // 动态呼吸颜色边框（外扩一圈，亮度随正弦脉动，参考 Copilot 等待指示的呼吸感）
            float breath = 0.5f + 0.5f * Mathf.Sin(time * 3f);
            var borderRect = barRect.ExpandedBy(1.5f);
            MD3Widgets.DrawRoundedRect(borderRect,
                new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 0.25f + 0.45f * breath), 3f);

            // 轨道
            MD3Widgets.DrawRoundedRect(barRect, MD3Theme.SurfaceContainer, 2f);

            // 已完成段：主色 + 呼吸亮度
            float fillWidth = barRect.width * progress;
            if (fillWidth > 1f)
            {
                Color fillColor = new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 0.8f + 0.2f * pulse);
                MD3Widgets.DrawRoundedRect(new Rect(barRect.x, barRect.y, fillWidth, barRect.height), fillColor, 2f);
            }

            // 前端脉冲亮点：随加载进度前移，亮度呼吸（输入框光标式）
            float tipWidth = Mathf.Min(20f, barRect.width * 0.12f);
            float tipX = barRect.x + fillWidth - tipWidth;
            if (progress < 1f && tipX + tipWidth <= barRect.xMax)
            {
                float tipAlpha = 0.4f + 0.6f * pulse;
                MD3Widgets.DrawRoundedRect(new Rect(tipX, barRect.y, tipWidth, barRect.height),
                    new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, tipAlpha), 2f);
            }

            // 缓冲段（填充前端之后）：半透明 + 流动高光
            float bufferWidth = barRect.width * 0.15f;
            if (progress < 1f && fillWidth + bufferWidth < barRect.width)
            {
                var bufferRect = new Rect(barRect.x + fillWidth, barRect.y, bufferWidth, barRect.height);
                MD3Widgets.DrawRoundedRect(bufferRect, new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 0.3f), 2f);
                // 流动高光：随时间在缓冲段内往返移动
                float shine = (time * 0.6f) % 1f;
                float shineWidth = bufferWidth * 0.5f;
                float shineX = bufferRect.x + (bufferWidth - shineWidth) * shine;
                MD3Widgets.DrawRoundedRect(new Rect(shineX, barRect.y, shineWidth, barRect.height),
                    new Color(MD3Theme.Primary.r, MD3Theme.Primary.g, MD3Theme.Primary.b, 0.6f + 0.3f * pulse), 2f);
            }
        }

        /// <summary>
        /// 绘制平滑环形加载进度：192x192 逐像素生成圆环纹理（内外半径间按角度填充），
        /// 4x4 子采样抗锯齿，从 12 点（顶部）开始顺时针；
        /// 呼吸脉冲亮度 + 前端白色亮点（输入框光标式）。
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
            var pixels = new Color[size * size];
            float subStep = 1f / subSamples;
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
                        pixels[index] = Color.clear;
                        continue;
                    }
                    // 边缘像素：按环覆盖率做 alpha 过渡（抗锯齿）+ 按填充比例混合颜色
                    float ringCoverage = ringHits / totalSamples;
                    float fillCoverage = fillHits / (float)ringHits;
                    Color blended = Color.Lerp(trackColor, fillColor, fillCoverage);
                    blended.a *= ringCoverage;
                    pixels[index] = blended;
                }
            }
            ringTexture.SetPixels(pixels);
            ringTexture.Apply();

            // 呼吸脉冲亮度
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
            GUI.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * pulse);
            GUI.DrawTexture(rect, ringTexture);
            GUI.color = Color.white;

            // 前端白色亮点（输入框光标式脉冲）；屏幕角度 fromTop=fillAngleEnd → 原始角 = fillAngleEnd-90
            float tipAngle = (fillAngleEnd - 90f) * Mathf.Deg2Rad;
            Vector2 tipPosition = rect.center + new Vector2(Mathf.Cos(tipAngle), Mathf.Sin(tipAngle)) * (rect.width / 2f - 5f);
            Texture2D dotTexture = SolidColorMaterials.NewSolidColorTexture(Color.white);
            float tipAlpha = 0.4f + 0.6f * pulse;
            GUI.color = new Color(1f, 1f, 1f, tipAlpha);
            GUI.DrawTexture(new Rect(tipPosition.x - 3f, tipPosition.y - 3f, 6f, 6f), dotTexture);
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
