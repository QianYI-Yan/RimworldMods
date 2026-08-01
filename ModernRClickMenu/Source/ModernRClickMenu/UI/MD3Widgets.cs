using UnityEngine;
using Verse;

namespace ModernRClickMenu.UI
{
    // ═══════════════════════════════════════════════════
    // MD3 基础绘制控件：圆角矩形、卡片、hover 状态层
    // 使用预生成的圆角矩形纹理 + 九宫格拉伸实现圆角，
    // 避免依赖 Unity 原生 GUI 皮肤。
    // ═══════════════════════════════════════════════════
    public static class MD3Widgets
    {
        // 圆角矩形纹理：64x64，四角圆角半径 16（采样基准）
        private const int TextureSize = 64;
        private const float TextureCorner = 16f;

        private static Texture2D roundedRectTexture;
        private static bool initialized;

        /// <summary>懒初始化圆角纹理（首次绘制时生成一次）。</summary>
        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            roundedRectTexture = CreateRoundedRectTexture();
            initialized = true;
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
            EnsureInitialized();
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

        /// <summary>按 UV 子区域绘制圆角纹理。</summary>
        private static void DrawTextureWithUv(Rect rect, Rect uv)
        {
            GUI.DrawTextureWithTexCoords(rect, roundedRectTexture, uv);
        }
    }
}
