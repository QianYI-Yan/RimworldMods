using System.Collections.Generic;
using ModernRClickMenu.Theme;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModernRClickMenu.UI
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
        private Vector2 scrollPosition;
        private bool draggingScrollbar;   // 是否正在拖动滚动条滑块

        // 展开/折叠动画：目标状态 + 当前进度（0=收起，1=展开），key 为 ThingDef（"其他"组为 null）
        private readonly HashSet<ThingDef> expandedTargets = new HashSet<ThingDef>();
        private readonly Dictionary<ThingDef, float> expandProgress = new Dictionary<ThingDef, float>();
        private const float ExpandAnimationSpeed = 10f;

        // 内容区不额外留 Margin：Window 默认 18px 会让内容区缩小 36px，
        // 导致内容被截断、未达最大高度就出现滚动。我们用自己的 Padding 控制边距。
        protected override float Margin => 0f;

        public MD3FloatMenuWindow(List<StoredItemGroup> groups)
        {
            this.groups = groups;

            // 默认收起所有子项（ClickGUI 风格：点击标题展开）
            foreach (StoredItemGroup group in groups)
            {
                expandProgress[group.representativeThing?.def] = 0f;
            }

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
            new Vector2(MD3Theme.MenuWidth, Mathf.Min(TotalViewHeight, MD3Theme.MaxMenuHeight));

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
            bool animating = false;
            foreach (StoredItemGroup group in groups)
            {
                ThingDef key = group.representativeThing?.def;
                float target = expandedTargets.Contains(key) ? 1f : 0f;
                float current = expandProgress.TryGetValue(key, out float value) ? value : 0f;
                float next = Mathf.MoveTowards(current, target, Time.deltaTime * ExpandAnimationSpeed);
                if (Mathf.Abs(next - target) > 0.001f)
                {
                    animating = true;
                }
                expandProgress[key] = next;
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
            // MD3 表面卡片背景
            MD3Widgets.DrawCard(inRect, MD3Theme.Surface, MD3Theme.WindowCornerRadius);

            // 内容全宽，左右边距在绘制时用 Padding 偏移；滚动条悬浮贴右缘，不占内容宽度
            float viewWidth = inRect.width;
            float viewHeight = TotalViewHeight;
            var viewRect = new Rect(0f, 0f, viewWidth, viewHeight);

            // 关闭默认滚动条，改用自己的浅色细滚动条（U3D 默认滚动条太丑）
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, showScrollbars: false);
            DrawGroups(viewRect);
            Widgets.EndScrollView();
            DrawCustomScrollbar(inRect, viewHeight);
        }

        /// <summary>逐物品分组绘制：组标题 + 子菜单操作项（含动画裁剪）。</summary>
        private void DrawGroups(Rect viewRect)
        {
            float y = MD3Theme.Padding;   // 顶部留白
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
            bool expanded = expandedTargets.Contains(group.representativeThing?.def);
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
            if (group.totalCount > 0)
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

            // hover 状态层
            if (Mouse.IsOver(headerRect))
            {
                MD3Widgets.DrawHoverState(headerRect, MD3Theme.HeaderCornerRadius);
            }

            // 标题 tooltip：名称可能被截断，悬停时显示完整信息
            TooltipHandler.TipRegion(headerRect, titleText);

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

                // hover 状态层
                if (Mouse.IsOver(rowRect))
                {
                    MD3Widgets.DrawHoverState(rowRect, MD3Theme.ActionCornerRadius);
                }

                // 操作文本（disabled 项灰色显示，label 已含不可执行原因）；
                // 自动换行 + 动态行高，长文本完整显示不被截断
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = true;
                GUI.color = entry.disabled ? MD3Theme.DisabledText : MD3Theme.OnSurface;
                Widgets.Label(new Rect(rowRect.x + 10f, rowRect.y, rowRect.width - 16f, rowRect.height), entry.label);
                Text.WordWrap = true;   // 恢复默认值，RimWorld 要求帧结束时 WordWrap 必须为 true
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                // 超长文本悬停显示完整内容
                TooltipHandler.TipRegion(rowRect, entry.label);

                // 点击执行
                if (Widgets.ButtonInvisible(rowRect) && !entry.disabled && entry.action != null)
                {
                    entry.action();
                    Close();
                }

                innerY += rowHeight;
            }
            GUI.EndGroup();

            return y + visibleHeight;
        }

        /// <summary>操作项文本可用宽度（用于换行计算）。</summary>
        private float GetActionTextWidth(float contentWidth)
        {
            return contentWidth - MD3Theme.ActionIndent - 16f;
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
            return expandProgress.TryGetValue(group.representativeThing?.def, out float value) ? value : 0f;
        }

        /// <summary>点击分组标题时切换展开 / 折叠（由 WindowUpdate 驱动动画）。</summary>
        private void ToggleGroupCollapsed(StoredItemGroup group)
        {
            ThingDef key = group.representativeThing?.def;
            if (expandedTargets.Contains(key))
            {
                expandedTargets.Remove(key);
            }
            else
            {
                expandedTargets.Add(key);
            }
        }

        /// <summary>按当前内容高度重算窗口高度，并防止超出屏幕。</summary>
        private void RefreshWindowHeight()
        {
            float newHeight = Mathf.Min(TotalViewHeight, MD3Theme.MaxMenuHeight);
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
    }
}
