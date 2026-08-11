using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ModernExpandMenu.UI
{
    // ═══════════════════════════════════════════════════
    // MD3 基础绘制控件：圆角矩形、卡片、hover 状态层
    // 使用预生成的圆角矩形纹理 + 九宫格拉伸实现圆角，
    // 避免依赖 Unity 原生 GUI 皮肤。
    // ═══════════════════════════════════════════════════
    [StaticConstructorOnStartup]
    public static class MD3Widgets
    {
        // 圆角矩形纹理：64x64，纹理圆角半径 = 目标 radius（按 radius 生成并缓存，9-slice 精确）。
        // 不能用「固定 16px 圆角纹理 + 动态 UV」：radius<8 时取的纹理子区域 alpha 几乎全透明 → 圆角破损；
        // 也不能用「固定 16px UV」：小 radius 四角采样错位 → 拼接处出现竖线/接缝。
        // StaticConstructorOnStartup 保证静态字段在游戏启动时于主线程初始化。
        private const int TextureSize = 64;
        private const float TextureCorner = 16f;

        private static readonly Dictionary<float, Texture2D> roundedRectTextureCache = new Dictionary<float, Texture2D>();

        /// <summary>获取指定圆角半径的圆角矩形纹理（缓存，纹理圆角 = radius）。</summary>
        private static Texture2D GetRoundedRectTexture(float radius)
        {
            float r = Mathf.Clamp(radius, 1f, TextureCorner);
            if (!roundedRectTextureCache.TryGetValue(r, out Texture2D texture))
            {
                texture = CreateRoundedRectTexture(r);
                roundedRectTextureCache[r] = texture;
            }
            return texture;
        }

        // 圆形纹理（64x64，中心不透明、边缘平滑过渡；用于圆形快速路径，避免 9-slice 拼接缝）
        private static Texture2D circleTexture;

        private static Texture2D GetCircleTexture()
        {
            if (circleTexture == null)
            {
                const int size = 64;
                circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                circleTexture.wrapMode = TextureWrapMode.Clamp;
                circleTexture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[size * size];
                float center = (size - 1) / 2f;
                float radius = size / 2f - 1.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                        // 半径边缘平滑过渡（双线性过滤下柔和）
                        float alpha = Mathf.Clamp01(radius - distance + 1f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                circleTexture.SetPixels(pixels);
                circleTexture.Apply();
                circleTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            return circleTexture;
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
        /// 生成四角圆角矩形纹理（圆角半径 radius）：中心与边全不透明，四角按到圆心的距离场
        /// 计算 alpha，得到平滑圆角遮罩。
        /// </summary>
        private static Texture2D CreateRoundedRectTexture(float radius)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);
            texture.wrapMode = TextureWrapMode.Clamp;      // 9-slice UV 采样不越界
            texture.filterMode = FilterMode.Bilinear;      // 双线性过滤，边缘柔和无锯齿
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, CornerAlphaAt(x, y, radius));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
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
            // 圆角边缘 1.5px 内 smoothstep 平滑过渡（非硬边），配合双线性过滤消除放大后的锯齿/毛刺
            float edge = radius - distance;
            float t = Mathf.Clamp01(edge / 1.5f);
            return t * t * (3f - 2f * t);
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
            // 9-slice 各块坐标若为非整数像素，相邻块光栅化会产生 1px 拼接缝（露底色竖线，
            // 表现为按钮右侧 / 开关轨道左侧的竖线）。统一取整到最近像素：所有块整数对齐，根治竖线。
            rect = new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height));
            radius = Mathf.Round(radius);

            // 连圆角都放不下时才退化为实心矩形
            // （允许 width == radius*2 的胶囊形，如细滚动条两端半圆）
            if (rect.width < radius * 2f || rect.height < radius * 2f)
            {
                GUI.color = color;
                GUI.DrawTexture(rect, SolidColorMaterials.NewSolidColorTexture(Color.white));
                GUI.color = Color.white;
                return;
            }

            // 圆形快速路径（宽≈高≈2×radius）：四角 9-slice 在中心拼接，
            // 非整数 radius（如小开关圆点 size*0.7）会产生 1px 垂直接缝（露底色竖线）。
            // 用单张圆纹理绘制（无拼接），根治圆点/圆球竖线。
            if (Mathf.Abs(rect.width - rect.height) < 0.5f && Mathf.Abs(rect.width - radius * 2f) < 0.5f)
            {
                GUI.color = color;
                GUI.DrawTexture(rect, GetCircleTexture());
                GUI.color = Color.white;
                return;
            }

            // 9-slice：纹理圆角半径 = 目标 radius（按 radius 生成的纹理），
            // 因此 cornerUv 直接取 radius 比例即可获得精确圆角，且四块拼接处 UV 连续（无竖线/接缝）。
            float cornerUv = Mathf.Clamp(radius / TextureSize, 0.02f, TextureCorner / TextureSize);
            float centerUv = 1f - cornerUv * 2f;
            Texture2D texture = GetRoundedRectTexture(radius);

            GUI.color = color;

            // 中心主体（全实心，UV 任意）
            DrawTextureWithUv(texture, new Rect(rect.x + radius, rect.y + radius, rect.width - radius * 2f, rect.height - radius * 2f),
                new Rect(cornerUv, cornerUv, centerUv, centerUv));
            // 四条边（GUI 纹理 v=0 在底部：目标顶部 ← v 大，目标底部 ← v 小）
            DrawTextureWithUv(texture, new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, radius),
                new Rect(cornerUv, 1f - cornerUv, centerUv, cornerUv));
            DrawTextureWithUv(texture, new Rect(rect.x + radius, rect.yMax - radius, rect.width - radius * 2f, radius),
                new Rect(cornerUv, 0f, centerUv, cornerUv));
            DrawTextureWithUv(texture, new Rect(rect.x, rect.y + radius, radius, rect.height - radius * 2f),
                new Rect(0f, cornerUv, cornerUv, centerUv));
            DrawTextureWithUv(texture, new Rect(rect.xMax - radius, rect.y + radius, radius, rect.height - radius * 2f),
                new Rect(1f - cornerUv, cornerUv, cornerUv, centerUv));
            // 四角：圆角尖端必须朝外（目标角的外侧），
            // 目标左上角取纹理视觉左上角（v 方向已正确翻转）
            DrawTextureWithUv(texture, new Rect(rect.x, rect.y, radius, radius),
                new Rect(0f, 1f - cornerUv, cornerUv, cornerUv));
            DrawTextureWithUv(texture, new Rect(rect.xMax - radius, rect.y, radius, radius),
                new Rect(1f - cornerUv, 1f - cornerUv, cornerUv, cornerUv));
            DrawTextureWithUv(texture, new Rect(rect.x, rect.yMax - radius, radius, radius),
                new Rect(0f, 0f, cornerUv, cornerUv));
            DrawTextureWithUv(texture, new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius),
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
        /// hoverColor 可传入杂项配色（全局替换功能用），默认跟随扩展菜单主题色。
        /// </summary>
        public static void DrawHoverState(Rect rect, float radius, Color? hoverColor = null)
        {
            DrawRoundedRect(rect, hoverColor ?? Theme.MD3Theme.HoverStateLayer, radius);
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
        private static readonly Dictionary<int, float> switchAnimationStartTime = new Dictionary<int, float>();   // 本次动画开始时间
        private static readonly Dictionary<int, float> switchAnimationStartValue = new Dictionary<int, float>();  // 本次动画起始值
        private static readonly Dictionary<int, float> switchAnimationTarget = new Dictionary<int, float>();      // 本次动画目标值
        // 开关圆点动画时长（固定时长 + Material standard 近似缓动，手感顺滑）
        private const float SwitchAnimationDuration = 0.18f;

        /// <summary>
        /// MD3 安卓风格滑动开关：圆角轨道 + 白色圆形滑块，
        /// 开启时轨道主色、滑块靠右；关闭时轨道深灰描边、滑块靠左。
        /// 滑块位置有平滑滑动动画；点击返回切换后的值。
        /// primaryColor 等可传入杂项配色（全局替换功能用），默认跟随扩展菜单主题色。
        /// </summary>
        public static bool MD3ToggleSwitch(Rect rect, bool value, int switchId, Color? primaryColor = null, Color? trackColor = null, Color? surfaceColor = null)
        {
            Color primary = primaryColor ?? Theme.MD3Theme.Primary;
            Color track = trackColor ?? Theme.MD3Theme.SurfaceContainerHigh;
            Color surface = surfaceColor ?? Theme.MD3Theme.Surface;
            const float trackWidth = 38f;
            const float trackHeight = 20f;
            const float knobSize = 16f;
            var trackRect = new Rect(rect.x, rect.y + (rect.height - trackHeight) / 2f, trackWidth, trackHeight);

            // 圆点滑动动画（0=左/关，1=右/开）：固定时长 + Material standard 近似曲线（smoothstep），
            // 比纯指数 ease-out 起步更从容、尾段更平滑（对齐 Gemini demo 的手感）
            float target = value ? 1f : 0f;
            float now = Time.realtimeSinceStartup;
            if (!switchAnimationStartTime.TryGetValue(switchId, out float startTime)
                || Mathf.Abs(switchAnimationTarget[switchId] - target) > 0.001f)
            {
                // 首次或目标变化：记录本次动画起点（从当前进度开始，避免切换瞬间跳变）
                switchAnimationStartTime[switchId] = now;
                switchAnimationStartValue[switchId] = switchAnimationProgress.TryGetValue(switchId, out float cur) ? cur : target;
                switchAnimationTarget[switchId] = target;
            }
            float t = Mathf.Clamp01((now - switchAnimationStartTime[switchId]) / SwitchAnimationDuration);
            float eased = t * t * (3f - 2f * t);   // smoothstep，近似 Material standard cubic-bezier(0.2,0,0,1)
            float animated = Mathf.Lerp(switchAnimationStartValue[switchId], target, eased);
            if (t >= 1f)
            {
                animated = target;
            }
            switchAnimationProgress[switchId] = animated;

            // 轨道：开启主色 / 关闭深灰 + 描边
            if (value)
            {
                DrawRoundedRect(trackRect, primary, trackHeight / 2f);
            }
            else
            {
                DrawRoundedRect(trackRect, track, trackHeight / 2f);
                DrawRoundedRect(trackRect.ContractedBy(1f), surface, trackHeight / 2f - 1f);
            }

            // 白色圆形滑块（带内侧阴影点）
            float knobX = trackRect.x + (trackRect.width - knobSize) * animated;
            var knob = new Rect(knobX, trackRect.y + (trackRect.height - knobSize) / 2f, knobSize, knobSize);
            DrawRoundedRect(knob, Color.white, knobSize / 2f);
            DrawRoundedRect(knob.ContractedBy(4f), Theme.MD3Theme.OnSurface, (knobSize - 8f) / 2f);

            // 点击切换
            if (Widgets.ButtonInvisible(rect))
            {
                return !value;
            }
            return value;
        }

        // 滑块圆点 hover/拖动放大动画进度（key 为 sliderId）
        private static readonly Dictionary<int, float> sliderKnobScaleProgress = new Dictionary<int, float>();

        /// <summary>
        /// MD3 风格滑块：圆角轨道 + 主色填充 + 圆形滑块，支持点击轨道跳转与按住拖动
        /// （鼠标移出滑块区域后拖动仍持续跟随）；hover / 拖动时圆点平滑放大高亮，方便吸附抓取。
        /// primaryColor / trackColor 可传入杂项配色（全局替换功能用），默认跟随扩展菜单主题色。
        /// </summary>
        public static float MD3Slider(Rect rect, float value, float min, float max, int sliderId, Color? primaryColor = null, Color? trackColor = null)
        {
            Color primary = primaryColor ?? Theme.MD3Theme.Primary;
            Color trackFillColor = trackColor ?? Theme.MD3Theme.SurfaceContainerHigh;
            float t = Mathf.InverseLerp(min, max, value);
            var track = new Rect(rect.x, rect.y + (rect.height - 4f) / 2f, rect.width, 4f);
            DrawRoundedRect(track, trackFillColor, 2f);
            var fill = new Rect(track.x, track.y, track.width * t, track.height);
            if (fill.width > 2f)
            {
                DrawRoundedRect(fill, primary, 2f);
            }

            // 交互：点击轨道跳转 / 按住拖动（MouseDrag 持续，拖出 rect 后仍跟随鼠标）
            bool hovering = Mouse.IsOver(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hovering)
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

            // 圆点：hover 或拖动时平滑放大高亮（安卓风格，方便吸附抓取）
            bool active = hovering || draggingSliderId == sliderId;
            float targetScale = active ? 1f : 0f;
            float animated = sliderKnobScaleProgress.TryGetValue(sliderId, out float cur) ? cur : targetScale;
            animated = Mathf.MoveTowards(animated, targetScale, Time.deltaTime * 10f);
            sliderKnobScaleProgress[sliderId] = animated;
            float knobSize = Mathf.Lerp(16f, 22f, animated);
            var knob = new Rect(track.x + t * track.width - knobSize / 2f, rect.y + (rect.height - knobSize) / 2f, knobSize, knobSize);
            DrawRoundedRect(knob, primary, knobSize / 2f);   // 正方形 + 半边长圆角 = 圆形
            DrawRoundedRect(knob.ContractedBy(3f), Theme.MD3Theme.Surface, (knobSize - 6f) / 2f);
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

        private static GUIStyle GetMd3TextFieldStyle(Color? textColor = null)
        {
            Color text = textColor ?? Theme.MD3Theme.OnSurface;
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
            md3TextFieldStyle.normal.textColor = text;
            md3TextFieldStyle.focused.textColor = text;
            return md3TextFieldStyle;
        }

        // 全局 MD3 输入框样式缓存（按原始样式引用区分字号/字体，避免每帧新建产生 GC）
        private static readonly Dictionary<GUIStyle, GUIStyle> md3TextFieldStyleCache = new Dictionary<GUIStyle, GUIStyle>(new StyleReferenceComparer());

        private sealed class StyleReferenceComparer : IEqualityComparer<GUIStyle>
        {
            public bool Equals(GUIStyle a, GUIStyle b) => ReferenceEquals(a, b);
            public int GetHashCode(GUIStyle style) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(style);
        }

        // MD3 输入框背景纹理（深色圆角背景 + 边缘反色 20% 边框色），按背景色缓存（菜单色/杂项色各一份）
        private static readonly Dictionary<Color, Texture2D> md3TextFieldBgTextureCache = new Dictionary<Color, Texture2D>();

        private static Texture2D GetMd3TextFieldBgTexture(Color bg)
        {
            if (!md3TextFieldBgTextureCache.TryGetValue(bg, out Texture2D texture))
            {
                texture = CreateMd3TextFieldBgTexture(bg);
                md3TextFieldBgTextureCache[bg] = texture;
            }
            return texture;
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
        /// + 反色 20% 边框（9-slice 纹理，随配色缓存）；文字/光标颜色跟随配色，每帧更新。
        /// 供"原版输入框全部改为 MD3 样式"可选功能使用（Text.CurTextFieldStyle 全局替换）。
        /// textColor / bgColor 可传入杂项配色（全局替换功能用），默认跟随扩展菜单主题色。
        /// </summary>
        public static GUIStyle ToMd3TextFieldStyle(GUIStyle original, Color? textColor = null, Color? bgColor = null)
        {
            Color text = textColor ?? Theme.MD3Theme.OnSurface;
            Color bg = bgColor ?? Theme.MD3Theme.SurfaceContainerHigh;
            if (original == null)
            {
                GUIStyle fallback = GetMd3TextFieldStyle();
                fallback.normal.textColor = text;
                fallback.focused.textColor = text;
                return fallback;
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
                md3TextFieldStyleCache[original] = style;
            }
            // 背景纹理按当前配色缓存重建（切换配色时更新）
            Texture2D background = GetMd3TextFieldBgTexture(bg);
            style.normal.background = background;
            style.hover.background = background;
            style.focused.background = background;
            style.active.background = background;
            style.normal.textColor = text;
            style.focused.textColor = text;
            return style;
        }

        /// <summary>
        /// MD3 文本输入框：深色圆角背景 + 主色描边环（valid 为 false 时红色描边），
        /// 内部用无边框透明样式的原生 GUI.TextField 输入（不叠加原版输入框外观，纯 MD3）。
        /// fieldId 用于区分多个输入框（控件名）；返回编辑后的文本。
        /// primaryColor / bgColor / textColor 可传入杂项配色（全局替换功能用），默认跟随扩展菜单主题色。
        /// </summary>
        public static string MD3TextField(Rect rect, string text, int fieldId, bool valid, Color? primaryColor = null, Color? bgColor = null, Color? textColor = null)
        {
            Color primary = primaryColor ?? Theme.MD3Theme.Primary;
            Color bg = bgColor ?? Theme.MD3Theme.SurfaceContainerHigh;
            string controlName = "MD3TextField" + fieldId;

            // MD3 外观：深色圆角背景 + 主色描边环（描边环不填充内部，不覆盖文字）
            DrawRoundedRect(rect, bg, 6f);
            Color outline = valid ? primary : new Color(1f, 0.3f, 0.3f, 0.85f);
            DrawRoundedRectOutline(rect, outline, 6f, 1.5f, bg);

            // 点击聚焦（聚焦后原生控件才接收键盘输入 / 显示光标）
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect))
            {
                GUI.FocusControl(controlName);
            }
            GUI.SetNextControlName(controlName);
            Text.Font = GameFont.Small;   // 让 GUI.skin.font 使用游戏字体
            return GUI.TextField(rect.ContractedBy(6f), text, GetMd3TextFieldStyle(textColor));
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

        /// <summary>按 UV 子区域绘制圆角纹理（texture 为对应圆角半径的纹理）。</summary>
        private static void DrawTextureWithUv(Texture2D texture, Rect rect, Rect uv)
        {
            GUI.DrawTextureWithTexCoords(rect, texture, uv);
        }

        // ═══════════════════════════════════════════════════
        // 赛博炫酷开关（Cyber Switch）+ 多段选择器（Segmented Control）
        // 灵感来自 Gemini demo：流光循环边框 / 极光网格背景 / 斜向扫光束 / 多重冲击波。
        // 在 RimWorld 即时模式 GUI 中实现为：HSV 色相循环描边环（角边无缝）、
        // 平铺网格纹理、切换瞬间从左到右的扫光亮带、从徽章扩散的半透明冲击波圆。
        // ═══════════════════════════════════════════════════

        // 赛博风格通用状态：记录开关值切换时刻（触发扫光 / 冲击波一次性动画）
        private static readonly Dictionary<int, float> cyberToggleTime = new Dictionary<int, float>();
        private static readonly Dictionary<int, bool> cyberLastValue = new Dictionary<int, bool>();

        /// <summary>
        /// 圆角矩形细边框纹理缓存（环厚 2px，圆角 radius，64×64，9-slice 拉伸绘制）。
        /// 只画环、不填充内部，可画在已有内容之上（赛博开关的流光边框需盖住网格/扫光）。
        /// </summary>
        private static readonly Dictionary<float, Texture2D> roundedRectBorderTextureCache = new Dictionary<float, Texture2D>();

        private static Texture2D GetRoundedRectBorderTexture(float radius)
        {
            float r = Mathf.Clamp(radius, 1f, TextureCorner);
            if (!roundedRectBorderTextureCache.TryGetValue(r, out Texture2D texture))
            {
                texture = CreateRoundedRectBorderTexture(r);
                roundedRectBorderTextureCache[r] = texture;
            }
            return texture;
        }

        /// <summary>生成圆角矩形边框环纹理（环厚 2px，圆角 radius；环内 alpha=1，环外 0）。</summary>
        private static Texture2D CreateRoundedRectBorderTexture(float radius)
        {
            const float thickness = 2f;
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);
            texture.wrapMode = TextureWrapMode.Clamp;      // 9-slice UV 采样不越界
            texture.filterMode = FilterMode.Bilinear;      // 双线性过滤，描边边缘柔和
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float alpha = BorderAlphaAt(x, y, radius, thickness);
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        /// <summary>计算纹理像素的边框环 alpha（环内 1，环外 0；环厚 thickness）。</summary>
        private static float BorderAlphaAt(int x, int y, float radius, float thickness)
        {
            // 在外圆角矩形内（角部距离场 > 0）？
            float outerAlpha = CornerAlphaAt(x, y, radius);
            if (outerAlpha <= 0.001f)
            {
                return 0f;
            }
            // 在内缩 thickness 的圆角矩形内（角部距离场 == 1）→ 非环区域
            float innerAlpha = CornerAlphaAt(x, y, Mathf.Max(1f, radius - thickness));
            if (innerAlpha > 0.999f)
            {
                return 0f;
            }
            // 环带：外边缘平滑（乘外角 alpha），内边缘由 innerAlpha 挖掉后自然成型
            return outerAlpha;
        }

        /// <summary>
        /// 圆角矩形描边环（环厚 2px，圆角 radius，9-slice 拉伸，角与边无缝）。
        /// 只画环、不填充内部，可画在已有内容之上。
        /// </summary>
        public static void DrawRoundedRectBorder(Rect rect, Color color, float radius)
        {
            float r = Mathf.Clamp(radius, 1f, TextureCorner);
            if (rect.width < r * 2f || rect.height < r * 2f)
            {
                DrawRoundedRect(rect, color, r);
                return;
            }
            const float thickness = 2f;
            float cornerUv = r / TextureSize;
            float edgeUv = thickness / TextureSize;
            float centerUv = 1f - cornerUv * 2f;
            Texture2D texture = GetRoundedRectBorderTexture(r);

            // 四角（r×r，圆角过渡带；v=0 在底部，目标顶部 ← v 大）
            DrawTextureWithUv(texture, new Rect(rect.x, rect.y, r, r), new Rect(0f, 1f - cornerUv, cornerUv, cornerUv), color);
            DrawTextureWithUv(texture, new Rect(rect.xMax - r, rect.y, r, r), new Rect(1f - cornerUv, 1f - cornerUv, cornerUv, cornerUv), color);
            DrawTextureWithUv(texture, new Rect(rect.x, rect.yMax - r, r, r), new Rect(0f, 0f, cornerUv, cornerUv), color);
            DrawTextureWithUv(texture, new Rect(rect.xMax - r, rect.yMax - r, r, r), new Rect(1f - cornerUv, 0f, cornerUv, cornerUv), color);
            // 四条边（厚 thickness）
            DrawTextureWithUv(texture, new Rect(rect.x + r, rect.y, rect.width - r * 2f, thickness), new Rect(cornerUv, 1f - cornerUv, centerUv, edgeUv), color);
            DrawTextureWithUv(texture, new Rect(rect.x + r, rect.yMax - thickness, rect.width - r * 2f, thickness), new Rect(cornerUv, 0f, centerUv, edgeUv), color);
            DrawTextureWithUv(texture, new Rect(rect.x, rect.y + r, thickness, rect.height - r * 2f), new Rect(0f, cornerUv, edgeUv, centerUv), color);
            DrawTextureWithUv(texture, new Rect(rect.xMax - thickness, rect.y + r, thickness, rect.height - r * 2f), new Rect(1f - cornerUv, cornerUv, edgeUv, centerUv), color);
        }

        // ── 赛博三色渐变（demo：蓝 #00A8FF → 青 #00FFCC → 粉 #FF007F → 蓝 循环）──
        private static readonly Color CyberBlue = new Color32(0, 168, 255, 255);
        private static readonly Color CyberCyan = new Color32(0, 255, 204, 255);
        private static readonly Color CyberPink = new Color32(255, 0, 127, 255);

        /// <summary>赛博三色渐变（蓝→青→粉→蓝 循环，t∈[0,1)）。</summary>
        private static Color CyberGradientColor(float t)
        {
            t = ((t % 1f) + 1f) % 1f;
            if (t < 0.333f) return Color.Lerp(CyberBlue, CyberCyan, t / 0.333f);
            if (t < 0.666f) return Color.Lerp(CyberCyan, CyberPink, (t - 0.333f) / 0.333f);
            return Color.Lerp(CyberPink, CyberBlue, (t - 0.666f) / 0.334f);
        }

        /// <summary>绘制赛博流光渐变边框（8 段三色渐变沿边框循环流动，每 3 秒一圈，对齐 demo rainbowGlow）。</summary>
        /// <summary>
        /// 绘制赛博流光渐变边框（36 段沿周长平滑采样三色渐变，3s 一圈流动；对齐 demo rainbowGlow）。
        /// 每条边分 8 小段 + 4 个圆角块，段间颜色连续 → 视觉平滑无明显分块（比 8 段版更接近 CSS 渐变）。
        /// </summary>
        public static void DrawCyberGradientBorder(Rect rect, float radius, float flowTime)
        {
            float r = Mathf.Clamp(radius, 1f, TextureCorner);
            if (rect.width < r * 2f || rect.height < r * 2f)
            {
                DrawRoundedRect(rect, CyberGradientColor(flowTime), r);
                return;
            }
            const float thickness = 2f;
            float cornerUv = r / TextureSize;
            float edgeUv = thickness / TextureSize;
            float centerUv = 1f - cornerUv * 2f;
            Texture2D texture = GetRoundedRectBorderTexture(r);

            float flow = flowTime % 1f;
            const int edgeSegments = 8;                 // 每条边段数
            const int totalSegments = edgeSegments * 4 + 4;   // 4 边 + 4 角 = 36
            const float segmentStep = 1f / totalSegments;

            // 沿周长均匀采样三色渐变（index = 段序号，顺时针 0..35）
            Color EdgeColor(int index)
            {
                return CyberGradientColor(flow + (index + 0.5f) * segmentStep);
            }

            float edgeLen = rect.width - r * 2f;
            float vertLen = rect.height - r * 2f;

            // 上边（index 0..7，左→右）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segW = edgeLen / edgeSegments;
                var segRect = new Rect(rect.x + r + segW * i, rect.y, segW, thickness);
                DrawTextureWithUv(texture, segRect, new Rect(cornerUv, 1f - cornerUv, centerUv, edgeUv), EdgeColor(i));
            }
            // 右上角（8）
            DrawTextureWithUv(texture, new Rect(rect.xMax - r, rect.y, r, r), new Rect(1f - cornerUv, 1f - cornerUv, cornerUv, cornerUv), EdgeColor(8));
            // 右边（9..16，上→下）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segH = vertLen / edgeSegments;
                var segRect = new Rect(rect.xMax - thickness, rect.y + r + segH * i, thickness, segH);
                DrawTextureWithUv(texture, segRect, new Rect(1f - cornerUv, cornerUv, edgeUv, centerUv), EdgeColor(9 + i));
            }
            // 右下角（17）
            DrawTextureWithUv(texture, new Rect(rect.xMax - r, rect.yMax - r, r, r), new Rect(1f - cornerUv, 0f, cornerUv, cornerUv), EdgeColor(17));
            // 下边（18..25，右→左）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segW = edgeLen / edgeSegments;
                var segRect = new Rect(rect.xMax - r - segW * (i + 1), rect.yMax - thickness, segW, thickness);
                DrawTextureWithUv(texture, segRect, new Rect(cornerUv, 0f, centerUv, edgeUv), EdgeColor(18 + i));
            }
            // 左下角（26）
            DrawTextureWithUv(texture, new Rect(rect.x, rect.yMax - r, r, r), new Rect(0f, 0f, cornerUv, cornerUv), EdgeColor(26));
            // 左边（27..34，下→上）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segH = vertLen / edgeSegments;
                var segRect = new Rect(rect.x, rect.yMax - r - segH * (i + 1), thickness, segH);
                DrawTextureWithUv(texture, segRect, new Rect(0f, cornerUv, edgeUv, centerUv), EdgeColor(27 + i));
            }
            // 左上角（35）
            DrawTextureWithUv(texture, new Rect(rect.x, rect.y, r, r), new Rect(0f, 1f - cornerUv, cornerUv, cornerUv), EdgeColor(35));
        }

        /// <summary>
        /// 主色动态跑马灯边框（仿 VSCode Copilot 输入框等待输出的流动光带）：
        /// 沿周长 36 段，主色亮度波峰随 flowTime 循环流动（波峰亮、波谷暗的主色光带扫过边框）。
        /// thickness 描边厚度可调；颜色固定主色（MD3Theme.Primary）。
        /// </summary>
        public static void DrawMarqueeBorder(Rect rect, float radius, float flowTime, float thickness = 2f)
        {
            float r = Mathf.Clamp(radius, 1f, TextureCorner);
            if (rect.width < r * 2f || rect.height < r * 2f)
            {
                DrawRoundedRect(rect, Theme.MD3Theme.Primary, r);
                return;
            }
            float cornerUv = r / TextureSize;
            float edgeUv = thickness / TextureSize;
            float centerUv = 1f - cornerUv * 2f;
            Texture2D texture = GetRoundedRectBorderTexture(r);

            float flow = flowTime % 1f;
            const int edgeSegments = 8;
            const int totalSegments = edgeSegments * 4 + 4;   // 36
            const float segmentStep = 1f / totalSegments;
            Color primary = Theme.MD3Theme.Primary;
            // 波峰亮色（主色提亮，防过曝）
            Color bright = new Color(Mathf.Min(1f, primary.r * 1.15f), Mathf.Min(1f, primary.g * 1.15f), Mathf.Min(1f, primary.b * 1.15f), primary.a);
            // 波谷暗色（主色压暗，保持色调，边框整体始终可见）
            Color dim = new Color(primary.r * 0.35f, primary.g * 0.35f, primary.b * 0.35f, primary.a);

            // 沿周长：亮度波峰随 flow 流动（段位置 = flow 处最亮，两侧渐暗）
            Color EdgeColor(int index)
            {
                float phase = ((flow - (index + 0.5f) * segmentStep) % 1f + 1f) % 1f;
                float wave = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
                return Color.Lerp(dim, bright, wave);
            }

            float edgeLen = rect.width - r * 2f;
            float vertLen = rect.height - r * 2f;

            // 上边（index 0..7，左→右）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segW = edgeLen / edgeSegments;
                var segRect = new Rect(rect.x + r + segW * i, rect.y, segW, thickness);
                DrawTextureWithUv(texture, segRect, new Rect(cornerUv, 1f - cornerUv, centerUv, edgeUv), EdgeColor(i));
            }
            // 右上角（8）
            DrawTextureWithUv(texture, new Rect(rect.xMax - r, rect.y, r, r), new Rect(1f - cornerUv, 1f - cornerUv, cornerUv, cornerUv), EdgeColor(8));
            // 右边（9..16，上→下）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segH = vertLen / edgeSegments;
                var segRect = new Rect(rect.xMax - thickness, rect.y + r + segH * i, thickness, segH);
                DrawTextureWithUv(texture, segRect, new Rect(1f - cornerUv, cornerUv, edgeUv, centerUv), EdgeColor(9 + i));
            }
            // 右下角（17）
            DrawTextureWithUv(texture, new Rect(rect.xMax - r, rect.yMax - r, r, r), new Rect(1f - cornerUv, 0f, cornerUv, cornerUv), EdgeColor(17));
            // 下边（18..25，右→左）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segW = edgeLen / edgeSegments;
                var segRect = new Rect(rect.xMax - r - segW * (i + 1), rect.yMax - thickness, segW, thickness);
                DrawTextureWithUv(texture, segRect, new Rect(cornerUv, 0f, centerUv, edgeUv), EdgeColor(18 + i));
            }
            // 左下角（26）
            DrawTextureWithUv(texture, new Rect(rect.x, rect.yMax - r, r, r), new Rect(0f, 0f, cornerUv, cornerUv), EdgeColor(26));
            // 左边（27..34，下→上）
            for (int i = 0; i < edgeSegments; i++)
            {
                float segH = vertLen / edgeSegments;
                var segRect = new Rect(rect.x, rect.yMax - r - segH * (i + 1), thickness, segH);
                DrawTextureWithUv(texture, segRect, new Rect(0f, cornerUv, edgeUv, centerUv), EdgeColor(27 + i));
            }
            // 左上角（35）
            DrawTextureWithUv(texture, new Rect(rect.x, rect.y, r, r), new Rect(0f, 1f - cornerUv, cornerUv, cornerUv), EdgeColor(35));
        }

        /// <summary>按 UV 子区域绘制圆角纹理（指定颜色，画完恢复）。</summary>
        private static void DrawTextureWithUv(Texture2D texture, Rect rect, Rect uv, Color color)
        {
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, texture, uv);
            GUI.color = Color.white;
        }

        // （赛博强调色已由三色渐变 CyberGradientColor 取代）

        // 赛博网格背景纹理（8x8 十字线，平铺 + 缓慢移动）
        private static Texture2D cyberGridTexture;

        private static Texture2D GetCyberGridTexture()
        {
            if (cyberGridTexture == null)
            {
                const int size = 8;
                cyberGridTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool line = x == 0 || y == 0;
                        pixels[y * size + x] = new Color(1f, 1f, 1f, line ? 1f : 0f);
                    }
                }
                cyberGridTexture.SetPixels(pixels);
                cyberGridTexture.Apply();
                cyberGridTexture.wrapMode = TextureWrapMode.Repeat;
                cyberGridTexture.filterMode = FilterMode.Point;
                cyberGridTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            return cyberGridTexture;
        }

        /// <summary>绘制赛博网格背景（平铺，offset 随时间缓慢移动形成流动感；对齐 demo 8s 移 2 格）。</summary>
        private static void DrawCyberGrid(Rect rect, float alpha, float moveSpeed = 4f)
        {
            float offset = (Time.realtimeSinceStartup * moveSpeed) % 8f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(rect, GetCyberGridTexture(),
                new Rect(offset / 8f, offset / 8f, rect.width / 8f, rect.height / 8f));
            GUI.color = Color.white;
        }

        // 赛博扫光渐变纹理（64x1：中间白、两端透明，模拟 demo LinearGradientBrush）
        private static Texture2D cyberSweepGradientTexture;

        private static Texture2D GetCyberSweepGradientTexture()
        {
            if (cyberSweepGradientTexture == null)
            {
                const int width = 64;
                cyberSweepGradientTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
                var pixels = new Color[width];
                for (int i = 0; i < width; i++)
                {
                    float t = (float)i / (width - 1);
                    pixels[i] = new Color(1f, 1f, 1f, Mathf.Sin(t * Mathf.PI));   // 0→1→0 正弦渐变
                }
                cyberSweepGradientTexture.SetPixels(pixels);
                cyberSweepGradientTexture.Apply();
                cyberSweepGradientTexture.wrapMode = TextureWrapMode.Clamp;
                cyberSweepGradientTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            return cyberSweepGradientTexture;
        }

        /// <summary>绘制切换瞬间的斜向渐变扫光束（skewX -25°，0.4s easeOutCubic 从左到右，淡出；对齐 demo sweepAnim）。</summary>
        private static void DrawCyberSweep(Rect rect, float toggleAge, float duration = 0.4f, float alpha = 0.28f)
        {
            if (toggleAge >= duration)
            {
                return;
            }
            float p = toggleAge / duration;
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            float sweepWidth = rect.width * 0.5f;
            float sweepX = Mathf.Lerp(rect.x - sweepWidth, rect.xMax, ease);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-25f, rect.center);
            GUI.color = new Color(1f, 1f, 1f, alpha * (1f - p));
            GUI.DrawTextureWithTexCoords(new Rect(sweepX, rect.y - rect.height * 0.2f, sweepWidth, rect.height * 1.4f),
                GetCyberSweepGradientTexture(), new Rect(0f, 0f, 1f, 1f));
            GUI.color = Color.white;
            GUI.matrix = oldMatrix;
        }

        /// <summary>
        /// 横向渐变光带（Copilot 顶条跑马灯）：一条主色渐变高光带沿 barRect 从左到右循环扫过。
        /// sweepProgress01 为 0~1 循环进度，sweepWidthRatio 为光带宽度占 barRect 的比例。
        /// </summary>
        public static void DrawHorizontalSweep(Rect barRect, float sweepProgress01, float sweepWidthRatio, Color color)
        {
            float sweepWidth = barRect.width * Mathf.Clamp(sweepWidthRatio, 0.1f, 1f);
            float sweepX = barRect.x - sweepWidth + (barRect.width + sweepWidth) * Mathf.Clamp01(sweepProgress01);
            var sweepRect = new Rect(sweepX, barRect.y, sweepWidth, barRect.height);
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(sweepRect, GetCyberSweepGradientTexture(), new Rect(0f, 0f, 1f, 1f));
            GUI.color = Color.white;
        }

        // 径向渐变纹理（中心白 → 边缘透明，模拟 CSS radial-gradient，用于发光/光晕）
        private static Texture2D cyberRadialGradientTexture;

        private static Texture2D GetRadialGradientTexture()
        {
            if (cyberRadialGradientTexture == null)
            {
                const int size = 128;
                cyberRadialGradientTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                float center = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy) / center;   // 0(中心)..1(边缘)
                        float alpha = Mathf.Clamp01(1f - distance);
                        alpha = alpha * alpha * (3f - 2f * alpha);   // smoothstep 更柔和（接近 CSS radial-gradient）
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                cyberRadialGradientTexture.SetPixels(pixels);
                cyberRadialGradientTexture.Apply();
                cyberRadialGradientTexture.wrapMode = TextureWrapMode.Clamp;
                cyberRadialGradientTexture.filterMode = FilterMode.Bilinear;
                cyberRadialGradientTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            return cyberRadialGradientTexture;
        }

        /// <summary>绘制径向发光（预生成径向渐变纹理，比多层圆叠加更平滑；对齐 demo radial-gradient）。</summary>
        private static void DrawCyberRadialGlow(Vector2 center, float outerRadius, Color color, float maxAlpha)
        {
            Color c = color;
            c.a = maxAlpha;
            GUI.color = c;
            GUI.DrawTexture(new Rect(center.x - outerRadius, center.y - outerRadius, outerRadius * 2f, outerRadius * 2f),
                GetRadialGradientTexture());
            GUI.color = Color.white;
        }

        /// <summary>绘制切换瞬间从徽章扩散的双重描边圆环冲击波（青色→粉色，放大 + 淡出；对齐 demo pulseWave 0.4s）。</summary>
        private static void DrawCyberShockwaves(Vector2 center, float toggleAge, float startSize, Color backgroundColor)
        {
            if (toggleAge >= 0.4f)
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                float local = Mathf.Clamp01((toggleAge - i * 0.2f) / 0.4f);
                if (local <= 0f)
                {
                    continue;
                }
                float size = Mathf.Lerp(startSize * 0.8f, startSize * 2.4f, local);
                Color ring = Color.Lerp(CyberCyan, CyberPink, local);
                ring.a = (1f - local) * 0.9f;
                DrawRoundedRect(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), ring, size / 2f);
                // 内圆用卡片背景色盖中心，形成 2px 描边环
                float innerSize = Mathf.Max(1f, size - 4f);
                DrawRoundedRect(new Rect(center.x - innerSize / 2f, center.y - innerSize / 2f, innerSize, innerSize), backgroundColor, innerSize / 2f);
            }
        }

        /// <summary>
        /// 赛博卡片式开关（完全对齐 Gemini「Ultimate Cyberpunk Switch」demo）：
        /// 三色流光渐变边框（3s 流动）、双径向发光（青/蓝）、流动网格、斜向扫光束、
        /// 徽章描边冲击波（青→粉）、点击 3D 压感、右侧渐变徽章。
        /// 关闭态：暗色卡片 + 细白描边 + 灰徽章。
        /// 返回切换后的值。
        /// </summary>
        /// <summary>
        /// 赛博卡片式开关（完全对齐 Gemini「CyberSwitchCard」WinForms 移植 + demo1）：
        /// 卡片布局 = 左侧图标 + 标题/描述两行文字 + 右侧 36px 徽章；
        /// 动效 = 三色流光渐变边框 + 16px 流动网格 + 斜向渐变扫光 + 双重冲击波 + 按压缩放。
        /// 建议高度 56~64px（demo 为 76px）。返回切换后的值。
        /// </summary>
        public static bool MD3CyberSwitch(Rect rect, string label, string description, bool value, int switchId)
        {
            float now = Time.realtimeSinceStartup;
            if (!cyberLastValue.TryGetValue(switchId, out bool lastValue) || lastValue != value)
            {
                cyberLastValue[switchId] = value;
                cyberToggleTime[switchId] = now;
            }
            float toggleAge = now - cyberToggleTime[switchId];
            float flow = now / 3f;   // 流光 3 秒一圈（对齐 demo rainbowGlow 3s）

            // 按压 3D 缩放（近似 demo scale 0.97）
            bool pressed = Input.GetMouseButton(0) && Mouse.IsOver(rect);
            Rect drawRect = pressed ? rect.ContractedBy(2f) : rect;

            float radius = Mathf.Min(18f, drawRect.height / 2f);
            Color background = value ? new Color32(16, 20, 30, 255) : new Color32(20, 23, 32, 255);   // #10141E / #141720

            // 1. 卡片主体背景
            DrawRoundedRect(drawRect, background, radius);
            // 2. 动态网格背景（激活，16px 格流动）
            if (value)
            {
                DrawCyberGrid(drawRect.ContractedBy(3f), 0.05f, 12f);
            }
            // 3. 斜向渐变扫光束（切换瞬间，0.4s）
            DrawCyberSweep(drawRect, toggleAge);
            // 4. 边框：激活 → 三色流光渐变；未激活 → 细白透明描边
            if (value)
            {
                DrawCyberGradientBorder(drawRect, radius, flow);
            }
            else
            {
                DrawRoundedRectBorder(drawRect, new Color(1f, 1f, 1f, 0.16f), radius);
            }

            // 5. 左侧图标（28px 圆 + 内点；激活强调色流光 / 未激活灰）
            float iconSize = 28f;
            var iconRect = new Rect(drawRect.x + 14f, drawRect.y + (drawRect.height - iconSize) / 2f, iconSize, iconSize);
            Color iconColor = value ? CyberGradientColor(flow + 0.25f) : new Color(0.55f, 0.57f, 0.62f, 1f);
            DrawRoundedRect(iconRect, iconColor, iconSize / 2f);
            DrawRoundedRect(iconRect.ContractedBy(9f), background, (iconSize - 18f) / 2f);

            // 6. 标题 + 描述（两行，右侧留出徽章空间）
            float textLeft = iconRect.xMax + 10f;
            float textWidth = drawRect.xMax - textLeft - (36f + 24f + 8f);
            var titleRect = new Rect(textLeft, drawRect.y + 8f, textWidth, 20f);
            var descRect = new Rect(textLeft, drawRect.y + 28f, textWidth, 18f);
            GUI.color = value ? Color.white : new Color(0.78f, 0.80f, 0.85f, 1f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            Widgets.Label(titleRect, label);
            GUI.color = value ? CyberGradientColor(flow + 0.5f) : new Color(0.55f, 0.57f, 0.62f, 1f);
            Text.Font = GameFont.Tiny;
            if (!description.NullOrEmpty())
            {
                Widgets.Label(descRect, description);
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;   // 帧末必须为 true
            GUI.color = Color.white;

            // 7. 右侧徽章（36px）+ 冲击波 + 对勾
            const float badgeSize = 36f;
            var badgeRect = new Rect(drawRect.xMax - badgeSize - 20f, drawRect.y + (drawRect.height - badgeSize) / 2f, badgeSize, badgeSize);
            if (value)
            {
                // 双重冲击波（青→粉，从徽章扩散）
                DrawCyberShockwaves(badgeRect.center, toggleAge, badgeSize, background);
                // 徽章 135° 渐变（相位流光近似）
                DrawRoundedRect(badgeRect, CyberGradientColor(flow + 0.5f), badgeSize / 2f);
                // 深色对勾（对齐 demo #041019）
                GUI.color = new Color(0.016f, 0.063f, 0.098f, 1f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(badgeRect, "✓");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
            }
            else
            {
                DrawRoundedRect(badgeRect, new Color(1f, 1f, 1f, 0.06f), badgeSize / 2f);
                DrawRoundedRectBorder(badgeRect, new Color(1f, 1f, 1f, 0.16f), badgeSize / 2f);
                GUI.color = new Color(0.55f, 0.57f, 0.62f, 0.5f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(badgeRect, "✓");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
            }

            // 8. 点击切换
            if (Widgets.ButtonInvisible(rect))
            {
                return !value;
            }
            return value;
        }

        /// <summary>
        /// 绘制小尺寸赛博开关（用于原版复选框 patch，保持原版占位尺寸）：
        /// 圆角方形轨道 + 三色流光渐变边框 + 发光圆点 + 斜向扫光。纯绘制，交互由原版继续处理。
        /// </summary>
        public static void DrawCyberCheckbox(Rect rect, bool value, bool disabled, int switchId)
        {
            float now = Time.realtimeSinceStartup;
            if (!cyberLastValue.TryGetValue(switchId, out bool lastValue) || lastValue != value)
            {
                cyberLastValue[switchId] = value;
                cyberToggleTime[switchId] = now;
            }
            float toggleAge = now - cyberToggleTime[switchId];
            float flow = now / 3f;

            const float radius = 4f;
            Color background = value ? new Color(0.06f, 0.08f, 0.12f, 1f) : new Color(0.12f, 0.13f, 0.17f, 1f);

            DrawRoundedRect(rect, background, radius);
            if (value)
            {
                DrawCyberRadialGlow(rect.center, Mathf.Min(rect.width, rect.height) * 0.7f, CyberCyan, 0.2f);
            }
            DrawCyberSweep(rect, toggleAge, 0.5f, 0.2f);

            // 发光圆点（开启：白色外圈 + 渐变内芯；关闭：暗灰）
            float knobSize = rect.height - 6f;
            float knobX = rect.x + (rect.width - knobSize) * (value ? 1f : 0f);
            var knob = new Rect(knobX, rect.y + (rect.height - knobSize) / 2f, knobSize, knobSize);
            if (disabled)
            {
                DrawRoundedRect(knob, new Color(0.35f, 0.38f, 0.45f, 1f), knobSize / 2f);
            }
            else if (value)
            {
                DrawRoundedRect(knob, Color.white, knobSize / 2f);
                DrawRoundedRect(knob.ContractedBy(2.5f), CyberGradientColor(flow + 0.5f), (knobSize - 5f) / 2f);
            }
            else
            {
                DrawRoundedRect(knob, new Color(0.35f, 0.38f, 0.45f, 1f), knobSize / 2f);
            }

            if (value)
            {
                DrawCyberGradientBorder(rect, radius, flow);
            }
            else
            {
                DrawRoundedRectBorder(rect, new Color(1f, 1f, 1f, 0.1f), radius);
            }
        }

        // 分段选择器指示器滑动动画状态（key = controlId）
        private static readonly Dictionary<int, float> segmentedAnimStartTime = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> segmentedAnimStartX = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> segmentedAnimTargetX = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> segmentedSelected = new Dictionary<int, int>();
        private const float SegmentedAnimDuration = 0.25f;

        /// <summary>
        /// MD3 多段选择器（分段开关，仿 Gemini demo）：外层圆角容器 + 滑动胶囊指示器 + 等宽选项。
        /// 指示器随选中项平滑滑动（easeOutBack 轻微回弹，近似 cubic-bezier(0.34,1.56,0.64,1)）。
        /// cyberStyle 为 true 时容器变为赛博风格（深色 + 网格 + 流光边框 + 发光指示器）。
        /// 返回新的选中索引。
        /// </summary>
        public static int MD3SegmentedControl(Rect rect, int selectedIndex, string[] labels, int controlId, bool cyberStyle = false)
        {
            int count = Mathf.Max(1, labels.Length);
            const float pad = 4f;
            float optionWidth = (rect.width - pad * 2f) / count;
            float optionHeight = rect.height - pad * 2f;
            float containerRadius = Mathf.Min(16f, rect.height / 2f);   // demo 容器圆角 16
            float indicatorRadius = Mathf.Min(12f, optionHeight / 2f);  // demo 指示器圆角 12

            Color primary = Theme.MD3Theme.Primary;
            Color onPrimary = Theme.MD3Theme.OnPrimary;
            float flow = Time.realtimeSinceStartup / 3f;

            // 容器背景 + 边框（赛博 / 常规两种外观）
            if (cyberStyle)
            {
                DrawRoundedRect(rect, new Color(0.07f, 0.085f, 0.12f, 1f), containerRadius);
                DrawCyberGrid(rect.ContractedBy(3f), 0.05f, 4f);
                DrawCyberRadialGlow(new Vector2(rect.x + rect.width * 0.8f, rect.y + rect.height * 0.5f), rect.height * 0.6f, CyberCyan, 0.12f);
                DrawCyberGradientBorder(rect, containerRadius, flow);
                onPrimary = new Color(0.05f, 0.08f, 0.12f, 1f);
            }
            else
            {
                DrawRoundedRect(rect, Theme.MD3Theme.SurfaceContainerHigh, containerRadius);
                DrawRoundedRectBorder(rect, Theme.MD3Theme.Outline, containerRadius);
            }

            // 指示器位置（x 坐标）
            float GetIndicatorX(int index)
            {
                return rect.x + pad + optionWidth * index;
            }

            // 选中索引变化 → 记录本次动画起点（从当前位置开始，避免跳变）
            float now = Time.realtimeSinceStartup;
            if (!segmentedSelected.TryGetValue(controlId, out int prevSelected))
            {
                prevSelected = selectedIndex;
                segmentedSelected[controlId] = selectedIndex;
                segmentedAnimStartTime[controlId] = now;
                segmentedAnimStartX[controlId] = GetIndicatorX(selectedIndex);
                segmentedAnimTargetX[controlId] = GetIndicatorX(selectedIndex);
            }
            if (prevSelected != selectedIndex)
            {
                segmentedSelected[controlId] = selectedIndex;
                segmentedAnimStartTime[controlId] = now;
                segmentedAnimStartX[controlId] = GetIndicatorX(prevSelected);
                segmentedAnimTargetX[controlId] = GetIndicatorX(selectedIndex);
            }
            float startX = segmentedAnimStartX[controlId];
            float targetX = segmentedAnimTargetX[controlId];
            float t = Mathf.Clamp01((now - segmentedAnimStartTime[controlId]) / SegmentedAnimDuration);
            // easeOutBack：先轻微回弹再到位（overshoot），近似 demo 的 cubic-bezier(0.34,1.56,0.64,1)
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float eased = t < 1f ? 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f) : 1f;
            float indicatorX = Mathf.Lerp(startX, targetX, Mathf.Clamp01(eased));

            // 滑动胶囊指示器：主色填充 + 下方投影光晕（对齐 demo box-shadow 0 2px 8px）
            var indicatorRect = new Rect(indicatorX, rect.y + pad, optionWidth - 2f, optionHeight);
            Color indicatorColor = cyberStyle ? CyberGradientColor(flow + 0.5f) : primary;
            Color indicatorShadow = indicatorColor;
            indicatorShadow.a = 0.30f;
            DrawRoundedRect(new Rect(indicatorRect.x - 2f, indicatorRect.y + 3f, indicatorRect.width + 4f, 3f), indicatorShadow, 1.5f);
            DrawRoundedRect(indicatorRect, indicatorColor, indicatorRadius);

            // 各选项：文字 + 点击（选中文字用 onPrimary，随指示器移动）
            int result = selectedIndex;
            for (int i = 0; i < count; i++)
            {
                var optionRect = new Rect(rect.x + pad + optionWidth * i, rect.y + pad, optionWidth, optionHeight);
                bool isSelected = i == selectedIndex;
                GUI.color = isSelected ? onPrimary : Theme.MD3Theme.OnSurfaceVariant;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                Widgets.Label(optionRect, labels[i]);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;   // 帧末必须为 true
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(optionRect))
                {
                    result = i;
                }
            }
            return result;
        }
    }
}
