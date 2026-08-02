using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NotificationsOnWindowsNow.UI
{
    // ═══════════════════════════════════════════════════
    // MD3 基础绘制控件：圆角矩形、卡片、hover 状态层
    // 使用预生成的圆角矩形纹理 + 九宫格拉伸实现圆角，
    // 避免依赖 Unity 原生 GUI 皮肤。
    // ═══════════════════════════════════════════════════
    [StaticConstructorOnStartup]
    public static class MD3Widgets
    {
        // 圆角矩形纹理：64x64，四角圆角半径 16（采样基准）。
        // StaticConstructorOnStartup 保证静态字段在游戏启动时于主线程初始化，
        // 满足 Unity 纹理必须主线程创建的要求（消除 RimWorld 启动警告）
        private const int TextureSize = 64;
        private const float TextureCorner = 16f;

        private static readonly Texture2D roundedRectTexture = CreateRoundedRectTexture();

        // 垂直渐变纹理（滚动内容顶部/底部淡出遮罩）：
        // fadeTopTexture   —— 顶部不透明 → 向下渐透明（内容从顶部边缘淡入时遮挡硬截断）
        // fadeBottomTexture—— 底部不透明 → 向上渐透明（内容从底部边缘淡出时遮挡硬截断）
        private static readonly Texture2D fadeTopTexture = CreateFadeTexture(opaqueAtBottom: false);
        private static readonly Texture2D fadeBottomTexture = CreateFadeTexture(opaqueAtBottom: true);

        /// <summary>生成 1x16 垂直渐变遮罩纹理（pixels[0] 为纹理底部，GUI 中显示在 rect 底部）。</summary>
        private static Texture2D CreateFadeTexture(bool opaqueAtBottom)
        {
            const int height = 16;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false);
            var pixels = new Color[height];
            for (int y = 0; y < height; y++)
            {
                float alpha = opaqueAtBottom ? (height - 1 - y) / (float)(height - 1) : y / (float)(height - 1);
                pixels[y] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        /// <summary>
        /// 绘制垂直渐变遮罩（用于滚动内容上下边缘淡出，避免内容被窗口边缘硬截断）。
        /// color 为遮罩色（通常取窗口表面色）；opaqueAtBottom=true 时底部不透明。
        /// </summary>
        public static void DrawVerticalFade(Rect rect, Color color, bool opaqueAtBottom)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, opaqueAtBottom ? fadeBottomTexture : fadeTopTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制圆角矩形描边：先整块填充 outlineColor，再用 fillColor 内缩覆盖中间，留出描边环。
        /// 用于窗口外框上下左右描边（MD3 Outline token）。
        /// </summary>
        public static void DrawRoundedRectOutline(Rect rect, Color outlineColor, float radius, float thickness, Color fillColor)
        {
            DrawRoundedRect(rect, outlineColor, radius);
            float inset = Mathf.Max(0.5f, thickness);
            DrawRoundedRect(new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f),
                fillColor, Mathf.Max(1f, radius - inset));
        }

        /// <summary>
        /// 生成四角圆角矩形纹理：中心与边全不透明，四角按到圆心的距离场
        /// 计算 alpha，得到平滑圆角遮罩。
        /// </summary>
        private static Texture2D CreateRoundedRectTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, CornerAlphaAt(x, y));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        /// <summary>计算纹理中某像素的圆角遮罩 alpha（0~1）。</summary>
        private static float CornerAlphaAt(int x, int y)
        {
            return CornerAlphaAt(x, y, TextureCorner);
        }

        /// <summary>计算纹理中某像素的圆角遮罩 alpha（0~1，radius 为圆角半径）。</summary>
        private static float CornerAlphaAt(int x, int y, float radius)
        {
            float dx = 0f;
            float dy = 0f;
            bool insideCorner = true;

            if (x < radius && y < radius)
            {
                dx = radius - x;
                dy = radius - y;
            }
            else if (x >= TextureSize - radius && y < radius)
            {
                dx = x - (TextureSize - radius);
                dy = radius - y;
            }
            else if (x < radius && y >= TextureSize - radius)
            {
                dx = radius - x;
                dy = y - (TextureSize - radius);
            }
            else if (x >= TextureSize - radius && y >= TextureSize - radius)
            {
                dx = x - (TextureSize - radius);
                dy = y - (TextureSize - radius);
            }
            else
            {
                insideCorner = false;
            }

            if (!insideCorner)
            {
                return 1f;
            }
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - distance);
        }

        /// <summary>
        /// 绘制圆角矩形（九宫格拉伸，圆角形状保持正确）。
        /// </summary>
        public static void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            if (rect.width < 1f || rect.height < 1f)
            {
                return;
            }

            // 连圆角都放不下时才退化为实心矩形
            // （允许 width == radius*2 的胶囊形，如细滚动条两端半圆）
            if (rect.width < radius * 2f || rect.height < radius * 2f)
            {
                GUI.color = color;
                GUI.DrawTexture(rect, SolidColorMaterials.NewSolidColorTexture(Color.white));
                GUI.color = Color.white;
                return;
            }

            float cornerUv = TextureCorner / TextureSize;
            float centerUv = 1f - cornerUv * 2f;

            GUI.color = color;

            // 中心主体（全实心，UV 任意）
            DrawTextureWithUv(new Rect(rect.x + radius, rect.y + radius, rect.width - radius * 2f, rect.height - radius * 2f),
                new Rect(cornerUv, cornerUv, centerUv, centerUv));
            // 四条边（GUI 纹理 v=0 在底部：目标顶部 ← v 大，目标底部 ← v 小）
            DrawTextureWithUv(new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, radius),
                new Rect(cornerUv, 1f - cornerUv, centerUv, cornerUv));
            DrawTextureWithUv(new Rect(rect.x + radius, rect.yMax - radius, rect.width - radius * 2f, radius),
                new Rect(cornerUv, 0f, centerUv, cornerUv));
            DrawTextureWithUv(new Rect(rect.x, rect.y + radius, radius, rect.height - radius * 2f),
                new Rect(0f, cornerUv, cornerUv, centerUv));
            DrawTextureWithUv(new Rect(rect.xMax - radius, rect.y + radius, radius, rect.height - radius * 2f),
                new Rect(1f - cornerUv, cornerUv, cornerUv, centerUv));
            // 四角：圆角尖端必须朝外（目标角的外侧），
            // 目标左上角取纹理视觉左上角（v 方向已正确翻转）
            DrawTextureWithUv(new Rect(rect.x, rect.y, radius, radius),
                new Rect(0f, 1f - cornerUv, cornerUv, cornerUv));
            DrawTextureWithUv(new Rect(rect.xMax - radius, rect.y, radius, radius),
                new Rect(1f - cornerUv, 1f - cornerUv, cornerUv, cornerUv));
            DrawTextureWithUv(new Rect(rect.x, rect.yMax - radius, radius, radius),
                new Rect(0f, 0f, cornerUv, cornerUv));
            DrawTextureWithUv(new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius),
                new Rect(1f - cornerUv, 0f, cornerUv, cornerUv));

            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制 MD3 卡片：底部柔和阴影 + 圆角表面。
        /// </summary>
        public static void DrawCard(Rect rect, Color surfaceColor, float radius)
        {
            var shadowRect = rect;
            shadowRect.y += 3f;
            DrawRoundedRect(shadowRect, Theme.MD3Theme.Shadow, radius);
            DrawRoundedRect(rect, surfaceColor, radius);
        }

        /// <summary>
        /// 绘制 hover 状态层（半透明主色覆盖在圆角区域上）。
        /// </summary>
        public static void DrawHoverState(Rect rect, float radius)
        {
            DrawRoundedRect(rect, Theme.MD3Theme.HoverStateLayer, radius);
        }

        // 当前正在拖动的滚动条滑块 id（-1 表示无），避免多个滚动条拖动互相冲突
        private static int draggingScrollbarId = -1;

        /// <summary>
        /// MD3 滚动视口：隐藏原版默认滚动条（太丑），由 EndScrollView 绘制 MD3 细滚动条。
        /// </summary>
        public static void MD3BeginScrollView(Rect viewRect, ref Vector2 scrollPosition, Rect contentRect)
        {
            Widgets.BeginScrollView(viewRect, ref scrollPosition, contentRect, showScrollbars: false);
        }

        /// <summary>
        /// 结束 MD3 滚动视口并绘制 MD3 细滚动条（轨道 + 滑块，支持拖动）。
        /// scrollbarId 用于区分多个滚动条；cornerInset 为轨道上下缩进（避开圆角）。
        /// </summary>
        public static void MD3EndScrollView(Rect viewRect, ref Vector2 scrollPosition, float contentHeight, int scrollbarId, float cornerInset)
        {
            Widgets.EndScrollView();
            MD3Scrollbar(viewRect, ref scrollPosition, contentHeight, scrollbarId, cornerInset);
        }

        /// <summary>
        /// MD3 自定义细滚动条（替代 U3D 默认滚动条）：轨道贴右缘、上下缩进避开圆角，
        /// 支持按住滑块拖动。仅在内容超出视口时显示。
        /// </summary>
        public static void MD3Scrollbar(Rect viewRect, ref Vector2 scrollPosition, float contentHeight, int scrollbarId, float cornerInset)
        {
            if (contentHeight <= viewRect.height + 0.5f)
            {
                draggingScrollbarId = -1;
                return;
            }
            float trackTop = viewRect.y + cornerInset;
            float trackHeight = viewRect.height - cornerInset * 2f;
            float thumbHeight = Mathf.Max(24f, trackHeight * trackHeight / contentHeight);
            float maxScroll = Mathf.Max(1f, contentHeight - viewRect.height);
            float thumbOffset = (trackHeight - thumbHeight) * (scrollPosition.y / maxScroll);
            var trackRect = new Rect(viewRect.xMax - Theme.MD3Theme.ScrollbarWidth - 3f, trackTop, Theme.MD3Theme.ScrollbarWidth, trackHeight);
            var thumbRect = new Rect(trackRect.x, trackRect.y + thumbOffset, Theme.MD3Theme.ScrollbarWidth, thumbHeight);

            // 按住滑块拖动
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(thumbRect))
            {
                draggingScrollbarId = scrollbarId;
                Event.current.Use();
            }
            if (draggingScrollbarId == scrollbarId)
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
                    draggingScrollbarId = -1;
                    Event.current.Use();
                }
            }

            // 绘制（拖动时滑块高亮）
            DrawRoundedRect(trackRect, Theme.MD3Theme.ScrollbarTrack, Theme.MD3Theme.ScrollbarWidth / 2f);
            DrawRoundedRect(thumbRect, draggingScrollbarId == scrollbarId ? Theme.MD3Theme.ScrollbarThumbDragging : Theme.MD3Theme.ScrollbarThumb, Theme.MD3Theme.ScrollbarWidth / 2f);
        }

        // 当前正在拖动的滑块 id（-1 表示无），避免多个滑块拖动互相冲突
        private static int draggingSliderId = -1;

        // 滑动开关的圆点动画进度缓存（key 为开关 id）
        private static readonly Dictionary<int, float> switchAnimationProgress = new Dictionary<int, float>();

        /// <summary>
        /// MD3 安卓风格滑动开关：圆角轨道 + 白色圆形滑块，
        /// 开启时轨道主色、滑块靠右；关闭时轨道深灰描边、滑块靠左。
        /// 滑块位置有平滑滑动动画；点击返回切换后的值。
        /// </summary>
        public static bool MD3ToggleSwitch(Rect rect, bool value, int switchId)
        {
            const float trackWidth = 38f;
            const float trackHeight = 20f;
            const float knobSize = 16f;
            var track = new Rect(rect.x, rect.y + (rect.height - trackHeight) / 2f, trackWidth, trackHeight);

            // 圆点滑动动画（0=左/关，1=右/开）
            float target = value ? 1f : 0f;
            float animated = switchAnimationProgress.TryGetValue(switchId, out float current) ? current : target;
            animated = Mathf.MoveTowards(animated, target, Time.deltaTime * 8f);
            switchAnimationProgress[switchId] = animated;

            // 轨道：开启主色 / 关闭深灰 + 描边
            if (value)
            {
                DrawRoundedRect(track, Theme.MD3Theme.Primary, trackHeight / 2f);
            }
            else
            {
                DrawRoundedRect(track, Theme.MD3Theme.SurfaceContainerHigh, trackHeight / 2f);
                DrawRoundedRect(track.ContractedBy(1f), Theme.MD3Theme.Surface, trackHeight / 2f - 1f);
            }

            // 白色圆形滑块（带内侧阴影点）
            float knobX = track.x + (track.width - knobSize) * animated;
            var knob = new Rect(knobX, track.y + (track.height - knobSize) / 2f, knobSize, knobSize);
            DrawRoundedRect(knob, Color.white, knobSize / 2f);
            DrawRoundedRect(knob.ContractedBy(4f), Theme.MD3Theme.OnSurface, (knobSize - 8f) / 2f);

            // 点击切换
            if (Widgets.ButtonInvisible(rect))
            {
                return !value;
            }
            return value;
        }

        /// <summary>
        /// MD3 风格滑块：圆角轨道 + 主色填充 + 圆形滑块，支持点击轨道跳转与拖动。
        /// </summary>
        public static float MD3Slider(Rect rect, float value, float min, float max, int sliderId)
        {
            float t = Mathf.InverseLerp(min, max, value);
            var track = new Rect(rect.x, rect.y + (rect.height - 4f) / 2f, rect.width, 4f);
            DrawRoundedRect(track, Theme.MD3Theme.SurfaceContainerHigh, 2f);
            var fill = new Rect(track.x, track.y, track.width * t, track.height);
            if (fill.width > 1f)
            {
                DrawRoundedRect(fill, Theme.MD3Theme.Primary, 2f);
            }
            const float knobSize = 16f;
            var knob = new Rect(track.x + t * track.width - knobSize / 2f, rect.y + (rect.height - knobSize) / 2f, knobSize, knobSize);
            DrawRoundedRect(knob, Theme.MD3Theme.Primary, knobSize / 2f);   // 正方形 + 半边长圆角 = 圆形
            DrawRoundedRect(knob.ContractedBy(3f), Theme.MD3Theme.Surface, (knobSize - 6f) / 2f);

            // 交互：点击轨道跳转 / 按住拖动
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect))
            {
                draggingSliderId = sliderId;
                Event.current.Use();
            }
            if (draggingSliderId == sliderId)
            {
                if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown)
                {
                    float localT = Mathf.Clamp01((Event.current.mousePosition.x - rect.x) / rect.width);
                    value = Mathf.Lerp(min, max, localT);
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    draggingSliderId = -1;
                    Event.current.Use();
                }
            }
            return value;
        }

        /// <summary>
        /// MD3 多段滑块（离散档位）：轨道 + 分段圆点，点击任意位置吸附到最近档位。
        /// 用于"排列组合"类离散选项（如滚动收起策略的 2×2 组合）。
        /// segmentCount 为档位数；返回 0~segmentCount-1 的新档位。
        /// </summary>
        public static int MD3SegmentSlider(Rect rect, int value, int segmentCount, int sliderId)
        {
            int count = Mathf.Max(2, segmentCount);
            float centerY = rect.y + rect.height / 2f;
            float padX = 10f;
            var trackRect = new Rect(rect.x + padX, centerY - 1f, rect.width - padX * 2f, 2f);
            float step = trackRect.width / (count - 1);

            // 交互：点击轨道任意位置吸附到最近档位（含拖动跟随）
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect))
            {
                draggingSliderId = sliderId;
                Event.current.Use();
            }
            if (draggingSliderId == sliderId)
            {
                if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown)
                {
                    float localX = Mathf.Clamp(Event.current.mousePosition.x - trackRect.x, 0f, trackRect.width);
                    value = Mathf.Clamp(Mathf.RoundToInt(localX / step), 0, count - 1);
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    draggingSliderId = -1;
                    Event.current.Use();
                }
            }

            // 绘制：轨道（已选区段主色填充）+ 分段圆点（当前档位主色高亮）
            DrawRoundedRect(trackRect, Theme.MD3Theme.SurfaceContainerHigh, 1f);
            if (value > 0)
            {
                DrawRoundedRect(new Rect(trackRect.x, trackRect.y, step * value, trackRect.height), Theme.MD3Theme.Primary, 1f);
            }
            for (int i = 0; i < count; i++)
            {
                var dot = new Rect(trackRect.x + step * i - 5f, centerY - 5f, 10f, 10f);
                bool selected = i == value;
                DrawRoundedRect(dot, selected ? Theme.MD3Theme.Primary : Theme.MD3Theme.SurfaceContainerHigh, 5f);
                if (selected)
                {
                    DrawRoundedRect(dot.ContractedBy(2f), Theme.MD3Theme.Surface, 3f);
                }
            }
            return value;
        }

        /// <summary>
        /// MD3 风格按钮：圆角填充 + hover 高亮，主色强调或深色次要。
        /// 返回是否被点击。
        /// </summary>
        public static bool MD3Button(Rect rect, string label, bool emphasized = false)
        {
            DrawRoundedRect(rect, emphasized ? Theme.MD3Theme.Primary : Theme.MD3Theme.SurfaceContainerHigh, 6f);
            if (Mouse.IsOver(rect))
            {
                DrawHoverState(rect, 6f);
            }
            GUI.color = emphasized ? Theme.MD3Theme.OnPrimary : Theme.MD3Theme.OnSurface;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true
            GUI.color = Color.white;
            return Widgets.ButtonInvisible(rect);
        }

        // 无边框透明文本输入样式（仅文字，背景与描边环由 MD3 自绘；文字/光标颜色跟随主题，每帧更新）
        private static GUIStyle md3TextFieldStyle;

        private static GUIStyle GetMd3TextFieldStyle()
        {
            if (md3TextFieldStyle == null)
            {
                md3TextFieldStyle = new GUIStyle(GUI.skin.textField)
                {
                    border = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
                // 去掉原版灰底 / 白框，只保留文字（背景与描边环由 MD3 自绘）
                md3TextFieldStyle.normal.background = null;
                md3TextFieldStyle.hover.background = null;
                md3TextFieldStyle.focused.background = null;
                md3TextFieldStyle.active.background = null;
            }
            md3TextFieldStyle.normal.textColor = Theme.MD3Theme.OnSurface;
            md3TextFieldStyle.focused.textColor = Theme.MD3Theme.OnSurface;
            return md3TextFieldStyle;
        }

        // 全局 MD3 输入框样式缓存（按原始样式引用区分字号/字体，避免每帧新建产生 GC）
        private static readonly Dictionary<GUIStyle, GUIStyle> md3TextFieldStyleCache = new Dictionary<GUIStyle, GUIStyle>(new StyleReferenceComparer());

        private sealed class StyleReferenceComparer : IEqualityComparer<GUIStyle>
        {
            public bool Equals(GUIStyle a, GUIStyle b) => ReferenceEquals(a, b);
            public int GetHashCode(GUIStyle style) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(style);
        }

        // MD3 输入框背景纹理（深色圆角背景 + 边缘反色 20% 边框色），随主题背景色缓存重建
        private static Texture2D md3TextFieldBgTexture;
        private static Color md3TextFieldBgCacheColor;

        private static Texture2D GetMd3TextFieldBgTexture()
        {
            Color bg = Theme.MD3Theme.SurfaceContainerHigh;
            if (md3TextFieldBgTexture == null || md3TextFieldBgCacheColor != bg)
            {
                if (md3TextFieldBgTexture != null)
                {
                    Object.Destroy(md3TextFieldBgTexture);
                }
                md3TextFieldBgTexture = CreateMd3TextFieldBgTexture(bg);
                md3TextFieldBgCacheColor = bg;
            }
            return md3TextFieldBgTexture;
        }

        /// <summary>生成 MD3 输入框背景纹理：深色圆角背景 + 边缘反色 20% 边框（64x64，9-slice 拉伸用）。</summary>
        private static Texture2D CreateMd3TextFieldBgTexture(Color bg)
        {
            const float radius = 6f;               // 圆角半径（与 9-slice border 一致；小圆角避免低输入框 9-slice 折断）
            const float borderThickness = 1.5f;    // 边框厚度（像素）
            // 边框色 = 背景色反色的 20% 强度混合（输入框附近反色，形成弱对比描边）
            Color border = Color.Lerp(bg, new Color(1f - bg.r, 1f - bg.g, 1f - bg.b, 1f), 0.2f);
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float alpha = CornerAlphaAt(x, y, radius);
                    float distToEdge = Mathf.Min(x, Mathf.Min(y, Mathf.Min(TextureSize - 1 - x, TextureSize - 1 - y)));
                    Color c = distToEdge < borderThickness ? border : bg;
                    c.a = alpha;
                    pixels[y * TextureSize + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        /// <summary>
        /// 把原版输入框样式转换为 MD3 样式（克隆原始样式保留字号/字体）：深色圆角背景
        /// + 反色 20% 边框（9-slice 纹理，随主题色重建）；文字/光标颜色跟随主题，每帧更新。
        /// 供"原版输入框全部改为 MD3 样式"可选功能使用（Text.CurTextFieldStyle 全局替换）。
        /// </summary>
        public static GUIStyle ToMd3TextFieldStyle(GUIStyle original)
        {
            if (original == null)
            {
                return GetMd3TextFieldStyle();
            }
            if (!md3TextFieldStyleCache.TryGetValue(original, out GUIStyle style))
            {
                style = new GUIStyle(original)
                {
                    border = new RectOffset(6, 6, 6, 6),   // 9-slice：圆角半径 6（与纹理一致）
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(8, 8, 3, 3),   // 文字避开边框/圆角
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
                Texture2D background = GetMd3TextFieldBgTexture();
                style.normal.background = background;
                style.hover.background = background;
                style.focused.background = background;
                style.active.background = background;
                md3TextFieldStyleCache[original] = style;
            }
            style.normal.textColor = Theme.MD3Theme.OnSurface;
            style.focused.textColor = Theme.MD3Theme.OnSurface;
            return style;
        }

        /// <summary>
        /// MD3 文本输入框：深色圆角背景 + 主色描边环（valid 为 false 时红色描边），
        /// 内部用无边框透明样式的原生 GUI.TextField 输入（不叠加原版输入框外观，纯 MD3）。
        /// fieldId 用于区分多个输入框（控件名）；返回编辑后的文本。
        /// </summary>
        public static string MD3TextField(Rect rect, string text, int fieldId, bool valid)
        {
            string controlName = "MD3TextField" + fieldId;

            // MD3 外观：深色圆角背景 + 主色描边环（描边环不填充内部，不覆盖文字）
            DrawRoundedRect(rect, Theme.MD3Theme.SurfaceContainerHigh, 6f);
            Color outline = valid ? Theme.MD3Theme.Primary : new Color(1f, 0.3f, 0.3f, 0.85f);
            DrawRoundedRectOutline(rect, outline, 6f, 1.5f, Theme.MD3Theme.SurfaceContainerHigh);

            // 点击聚焦（聚焦后原生控件才接收键盘输入 / 显示光标）
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect))
            {
                GUI.FocusControl(controlName);
            }
            GUI.SetNextControlName(controlName);
            Text.Font = GameFont.Small;   // 让 GUI.skin.font 使用游戏字体
            return GUI.TextField(rect.ContractedBy(6f), text, GetMd3TextFieldStyle());
        }

        /// <summary>
        /// MD3 安卓 15 风格数字输入框：圆角 + 主色描边外观，
        /// 输入处理使用原版 Widgets.TextFieldNumeric（Windows 键盘/输入法兼容、可靠保存数值），
        /// 仅绘制 MD3 边框包裹。
        /// </summary>
        public static void MD3NumberField(Rect rect, ref float value, ref string buffer, float min, out bool submitted, out bool cancelled)
        {
            // 背景 + 主色描边（描边环不填充内部，避免覆盖输入文字——此前在 TextFieldNumeric 之后再填充边框导致"点击后看不到内容"）
            DrawRoundedRect(rect, Theme.MD3Theme.SurfaceContainerHigh, 6f);
            DrawRoundedRectOutline(rect, Theme.MD3Theme.Primary, 6f, 1.5f, Theme.MD3Theme.SurfaceContainerHigh);

            // 原版可靠的数值输入（无上限，仅按下限限制）；文字画在背景上，不被覆盖
            Widgets.TextFieldNumeric(rect.ContractedBy(1f), ref value, ref buffer, min, 1E+09f);

            submitted = false;
            cancelled = false;
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return)
                {
                    submitted = true;
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    cancelled = true;
                    Event.current.Use();
                }
            }
        }

        /// <summary>按 UV 子区域绘制圆角纹理。</summary>
        private static void DrawTextureWithUv(Rect rect, Rect uv)
        {
            GUI.DrawTextureWithTexCoords(rect, roundedRectTexture, uv);
        }
    }
}
