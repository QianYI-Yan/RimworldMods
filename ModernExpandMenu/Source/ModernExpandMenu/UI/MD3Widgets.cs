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
        // 圆角矩形纹理：64x64，四角圆角半径 16（采样基准）。
        // StaticConstructorOnStartup 保证静态字段在游戏启动时于主线程初始化，
        // 满足 Unity 纹理必须主线程创建的要求（消除 RimWorld 启动警告）
        private const int TextureSize = 64;
        private const float TextureCorner = 16f;

        private static readonly Texture2D roundedRectTexture = CreateRoundedRectTexture();

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
            float radius = TextureCorner;
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

        /// <summary>
        /// MD3 安卓 15 风格数字输入框（自绘）：圆角背景 + 聚焦主色描边 + 闪烁光标。
        /// 仅接受数字与小数点；返回编辑后的文本；submitted=回车提交、cancelled=ESC 取消。
        /// </summary>
        public static string MD3NumberField(Rect rect, string text, bool focused, out bool submitted, out bool cancelled)
        {
            // 背景 + 描边（聚焦时主色，否则描边色）
            Color borderColor = focused ? Theme.MD3Theme.Primary : Theme.MD3Theme.Outline;
            DrawRoundedRect(rect, borderColor, 6f);
            DrawRoundedRect(rect.ContractedBy(1.5f), Theme.MD3Theme.SurfaceContainerHigh, 4.5f);

            submitted = false;
            cancelled = false;
            string newText = text;

            // 文本 + 闪烁光标
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            GUI.color = Theme.MD3Theme.OnSurface;
            Widgets.Label(new Rect(rect.x + 8f, rect.y, rect.width - 18f, rect.height), newText);
            if (focused && (int)(Time.realtimeSinceStartup * 2f) % 2 == 0)
            {
                float textWidth = Text.CalcSize(newText).x;
                GUI.color = Theme.MD3Theme.Primary;
                Widgets.Label(new Rect(rect.x + 8f + textWidth, rect.y + 2f, 2f, rect.height - 4f), "|");
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;

            // 键盘输入处理
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
                else if (Event.current.keyCode == KeyCode.Backspace)
                {
                    if (newText.Length > 0)
                    {
                        newText = newText.Substring(0, newText.Length - 1);
                    }
                    Event.current.Use();
                }
                else if (Event.current.character != '\0')
                {
                    char c = Event.current.character;
                    if (char.IsDigit(c) || c == '.' || c == ',')
                    {
                        newText += c;
                    }
                    Event.current.Use();
                }
            }
            return newText;
        }

        /// <summary>按 UV 子区域绘制圆角纹理。</summary>
        private static void DrawTextureWithUv(Rect rect, Rect uv)
        {
            GUI.DrawTextureWithTexCoords(rect, roundedRectTexture, uv);
        }
    }
}
