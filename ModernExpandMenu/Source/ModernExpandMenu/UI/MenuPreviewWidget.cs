using ModernExpandMenu.Theme;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    /// <summary>
    /// 可交互菜单预览：模拟一份游戏右键分组菜单（组标题 + 子项目）。
    /// 点击组标题展开/收起（组展开动画 + 子项目逐条出现动画），内容超高时可滚动，
    /// 实时反映当前动画速度设置与 MD3 颜色主题。用于设置界面左侧预览区（直接模拟游戏操作）。
    /// </summary>
    public static class MenuPreviewWidget
    {
        private class PreviewGroup
        {
            public int index;
            public int itemCount;
            public bool expanded;
            public float expandProgress;               // 展开动画进度 0~1
            public readonly float[] itemAppearTime;    // 各子项目出现动画开始时间（-1=未排定）
            public float nextItemTime;                 // 下一条子项目可排定时间（组内串行）

            public PreviewGroup(int index, int itemCount)
            {
                this.index = index;
                this.itemCount = itemCount;
                itemAppearTime = new float[itemCount];
                for (int i = 0; i < itemCount; i++)
                {
                    itemAppearTime[i] = -1f;
                }
            }
        }

        private static readonly PreviewGroup[] groups =
        {
            new PreviewGroup(1, 2),
            new PreviewGroup(2, 4),
            new PreviewGroup(3, 8),
            new PreviewGroup(4, 16),
            new PreviewGroup(5, 32),
        };

        private static Vector2 scrollPosition;

        /// <summary>绘制可交互菜单预览（模拟游戏右键分组菜单）。</summary>
        public static void Draw(Rect rect)
        {
            float now = Time.realtimeSinceStartup;
            float duration = Mathf.Max(0.02f, ModernExpandMenuMod.Settings.itemAppearDuration);
            float interval = Mathf.Max(0f, ModernExpandMenuMod.Settings.itemAppearInterval);
            float expandSpeed = Mathf.Max(1f, ModernExpandMenuMod.Settings.expandAnimationSpeed);

            // 组展开/收起动画推进（用当前展开速度设置）
            foreach (PreviewGroup g in groups)
            {
                float target = g.expanded ? 1f : 0f;
                g.expandProgress = Mathf.MoveTowards(g.expandProgress, target, Time.deltaTime * expandSpeed);
            }

            // 展开的组：组动画完成后逐条排定子项目（组内串行，用当前出现时长/间隔设置）
            foreach (PreviewGroup g in groups)
            {
                if (!g.expanded || g.expandProgress < 0.999f || now < g.nextItemTime)
                {
                    continue;
                }
                for (int i = 0; i < g.itemCount; i++)
                {
                    if (g.itemAppearTime[i] < 0f)
                    {
                        // 组内串行排定：匀速，每项动画时长一致
                        g.itemAppearTime[i] = now;
                        g.nextItemTime = now + duration + interval;
                        break;
                    }
                }
            }

            // 背景 + 外框（模拟菜单卡片）
            MD3Widgets.DrawRoundedRect(rect, MD3Theme.Surface, 8f);
            MD3Widgets.DrawRoundedRectOutline(rect, MD3Theme.Outline, 8f, 1f, MD3Theme.Surface);

            float contentHeight = ComputeContentHeight();
            var viewRect = new Rect(0f, 0f, rect.width, contentHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, showScrollbars: false);

            float y = 8f;
            float width = rect.width - MD3Theme.ScrollbarWidth - 4f;
            foreach (PreviewGroup g in groups)
            {
                // 组标题行（hover 高亮 + 点击展开/收起）
                var headerRect = new Rect(4f, y, width - 8f, 34f);
                DrawGroupHeader(headerRect, g);
                if (Mouse.IsOver(headerRect))
                {
                    MD3Widgets.DrawHoverState(headerRect, 8f);
                }
                if (Widgets.ButtonInvisible(headerRect))
                {
                    ToggleGroup(g);
                }
                y += 34f;

                // 子项目（按展开进度显示，逐条出现动画；未到动画时间不占高）
                if (g.expandProgress > 0.01f)
                {
                    int visibleCount = Mathf.CeilToInt(g.itemCount * g.expandProgress);
                    for (int i = 0; i < visibleCount; i++)
                    {
                        if (g.itemAppearTime[i] < 0f || now < g.itemAppearTime[i])
                        {
                            continue;   // 未到动画时间：不占高不绘制
                        }
                        float progress = Mathf.Clamp01((now - g.itemAppearTime[i]) / duration);
                        DrawItemRow(new Rect(4f, y, width - 8f, 30f), g, i, progress);
                        y += 30f;
                    }
                }
                y += 6f;
            }

            Widgets.EndScrollView();
            MD3Widgets.MD3Scrollbar(rect, ref scrollPosition, contentHeight, 3001, 8f);
        }

        /// <summary>切换组展开/收起（重置子项目出现动画，重新排定）。</summary>
        private static void ToggleGroup(PreviewGroup g)
        {
            g.expanded = !g.expanded;
            for (int i = 0; i < g.itemAppearTime.Length; i++)
            {
                g.itemAppearTime[i] = -1f;
            }
            g.nextItemTime = 0f;
        }

        /// <summary>预览内容总高度（标题 + 按展开进度显示的子项目 + 组间距）。</summary>
        private static float ComputeContentHeight()
        {
            float height = 8f;
            foreach (PreviewGroup g in groups)
            {
                height += 34f;
                height += 30f * g.itemCount * g.expandProgress;
                height += 6f;
            }
            return height + 8f;
        }

        /// <summary>绘制组标题行：主色竖条 + 图标块 + 名称 + 数量 + 展开箭头。</summary>
        private static void DrawGroupHeader(Rect rect, PreviewGroup g)
        {
            MD3Widgets.DrawRoundedRect(rect, MD3Theme.SurfaceContainerHigh, 8f);
            MD3Widgets.DrawRoundedRect(new Rect(rect.x + 6f, rect.y + 7f, 3f, rect.height - 14f), MD3Theme.Primary, 1.5f);
            MD3Widgets.DrawRoundedRect(new Rect(rect.x + 16f, rect.y + 7f, 20f, 20f), MD3Theme.OnSurfaceVariant, 6f);

            GUI.color = MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rect.x + 44f, rect.y, rect.width - 44f - 64f, rect.height), "ModernExpandMenu_PreviewGroup".Translate(g.index));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true
            GUI.color = MD3Theme.Primary;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.xMax - 62f, rect.y, 40f, rect.height), "×" + g.itemCount);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = MD3Theme.OnSurfaceVariant;
            Widgets.Label(new Rect(rect.xMax - 20f, rect.y, 14f, rect.height), g.expanded ? "▾" : "▸");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        /// <summary>绘制子项目行（主色竖条 + 文本），按出现进度左滑 + 淡入。</summary>
        private static void DrawItemRow(Rect rect, PreviewGroup g, int itemIndex, float progress)
        {
            float drawX = rect.x + 12f - (1f - progress) * 16f;
            Color bg = MD3Theme.SurfaceContainer;
            bg.a = Mathf.Clamp01(progress * 2f);
            MD3Widgets.DrawRoundedRect(new Rect(drawX, rect.y, rect.width - 12f, rect.height), bg, 6f);
            Color primary = MD3Theme.Primary;
            primary.a = Mathf.Clamp01(progress * 2f);
            MD3Widgets.DrawRoundedRect(new Rect(drawX + 4f, rect.y + 6f, 3f, rect.height - 12f), primary, 1.5f);

            Color text = MD3Theme.OnSurface;
            text.a = Mathf.Clamp01(progress * 2f);
            GUI.color = text;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(new Rect(drawX + 14f, rect.y, rect.width - 24f, rect.height), "ModernExpandMenu_PreviewItem".Translate(g.index, itemIndex + 1));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true
            GUI.color = Color.white;
        }
    }
}
