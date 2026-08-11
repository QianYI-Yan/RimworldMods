using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ModernSettingsUI
{
    /// <summary>
    /// 完全对齐 HTML (Ultimate Cyberpunk Switch Demo) 渲染与动效逻辑的 C# WinForms 自绘控件。
    /// 包含：彩虹流光边框、径向发光背景、流动网格、斜向扫光束、双重爆裂冲击波、缩放旋转徽章与按下 3D 压感。
    /// </summary>
    public class CyberSwitchCard : Control
    {
        // ═══════════════════════════════════════════════════
        // 配色 Tokens (1:1 对应 HTML CSS :root 变量)
        // ═══════════════════════════════════════════════════
        public Color ColorPrimary { get; set; } = Color.FromArgb(0, 168, 255);    // --md-primary: #00a8ff
        public Color ColorAccent { get; set; } = Color.FromArgb(0, 255, 204);     // --md-accent: #00ffcc
        public Color ColorPink { get; set; } = Color.FromArgb(255, 0, 127);       // --md-pink: #ff007f
        public Color ColorSurfaceCard { get; set; } = Color.FromArgb(20, 23, 32);  // --md-surface-card: #141720
        public Color ColorOnSurface { get; set; } = Color.FromArgb(226, 226, 233); // --md-on-surface: #e2e2e9
        public Color ColorOnSurfaceVariant { get; set; } = Color.FromArgb(139, 144, 160); // --md-on-surface-variant: #8b90a0

        // ═══════════════════════════════════════════════════
        // 属性与动效计时状态变量
        // ═══════════════════════════════════════════════════
        private bool _checked = false;
        private string _title = "超高帧率与渲染加速";
        private string _description = "开启 120Hz 动态渲染与全局 GPU 补帧";
        private bool _isPressed = false;

        // 动效驱动 Timer 与时间轴记录
        private readonly Timer _animTimer;
        private float _rainbowPhase = 0f;      // 0.0 -> 1.0 (对应 @keyframes rainbowGlow 3s 循环)
        private float _gridOffset = 0f;        // 0.0 -> 32.0 (对应 @keyframes gridMove 8s 循环)
        
        private long _activationStartTick = 0; // 记录激活瞬间，驱动单次触发的扫光与冲击波动画

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    if (_checked)
                    {
                        // 对齐 JS：激活时触发单次扫光与冲击波动画重置
                        _activationStartTick = DateTime.Now.Ticks;
                    }
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; Invalidate(); }
        }

        public event EventHandler CheckedChanged;

        // ═══════════════════════════════════════════════════
        // 构造函数与 60 FPS 动效驱动
        // ═══════════════════════════════════════════════════
        public CyberSwitchCard()
        {
            DoubleBuffered = true;
            Size = new Size(460, 78);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            _animTimer = new Timer { Interval = 16 }; // ~60 FPS
            _animTimer.Tick += (s, e) =>
            {
                // 1. 流光边框相位推进 (3s 周期)
                _rainbowPhase += 0.016f / 3.0f;
                if (_rainbowPhase >= 1.0f) _rainbowPhase -= 1.0f;

                // 2. 网格流动偏移推进 (8s 移动 32px)
                _gridOffset += (32.0f / 8.0f) * 0.016f;
                if (_gridOffset >= 16.0f) _gridOffset -= 16.0f;

                Invalidate();
            };
            _animTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        // ═══════════════════════════════════════════════════
        // 核心 GDI+ 自绘制逻辑（对齐 CSS 渲染层级结构）
        // ═══════════════════════════════════════════════════
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 获取从点击激活开始计算的秒数 (用于扫光与冲击波动画插值)
            float elapsedActiveTime = (_activationStartTick > 0) 
                ? (float)TimeSpan.FromTicks(DateTime.Now.Ticks - _activationStartTick).TotalSeconds 
                : 99f;

            // 0. 按压 3D 压感模拟 (对应 CSS .cyber-card-wrapper:active .cyber-card { transform: scale(0.96) })
            if (_isPressed)
            {
                g.TranslateTransform(Width / 2f, Height / 2f);
                g.ScaleTransform(0.96f, 0.96f);
                g.TranslateTransform(-Width / 2f, -Height / 2f);
            }

            // 外层 Wrapper 与 Card 尺寸定义 (Padding 2px 给流光边框)
            float borderPadding = 2f;
            RectangleF outerBounds = new RectangleF(0, 0, Width - 1, Height - 1);
            RectangleF cardBounds = new RectangleF(borderPadding, borderPadding, Width - borderPadding * 2 - 1, Height - borderPadding * 2 - 1);
            
            float outerRadius = 20f;
            float cardRadius = 18f;

            // 1. 外层流光循环边框 (对应 CSS .cyber-card-wrapper::before 渐变动画)
            if (_checked)
            {
                using (GraphicsPath outerPath = CreateRoundRect(outerBounds, outerRadius))
                using (LinearGradientBrush borderBrush = new LinearGradientBrush(
                    outerBounds, ColorPrimary, ColorPink, 0f))
                {
                    ColorBlend blend = new ColorBlend(5)
                    {
                        Colors = new Color[] { ColorPrimary, ColorAccent, ColorPink, ColorPrimary, ColorPrimary },
                        Positions = new float[] { 0f, 0.33f, 0.66f, 0.99f, 1.0f }
                    };
                    borderBrush.InterpolationColors = blend;
                    
                    // 通过 Scale Transform 模拟 HTML 的 background-position 300% 移动
                    borderBrush.ScaleTransform(3.0f, 1.0f);
                    borderBrush.TranslateTransform(-_rainbowPhase * outerBounds.Width * 2.0f, 0f);

                    using (Pen borderPen = new Pen(borderBrush, 2.5f))
                    {
                        g.DrawPath(borderPen, outerPath);
                    }
                }
            }

            // 2. 卡片主体与背景绘制 (对应 CSS .cyber-card & active 背景)
            using (GraphicsPath cardPath = CreateRoundRect(cardBounds, cardRadius))
            {
                if (_checked)
                {
                    // 激活态背景：径向发光极光 (RGBA 混合) + 基础深色背景 #10141e
                    using (SolidBrush baseBg = new SolidBrush(Color.FromArgb(16, 20, 30)))
                    {
                        g.FillPath(baseBg, cardPath);
                    }

                    // 右侧青色光晕 (at 80% 50%, rgba(0, 255, 204, 0.15))
                    DrawRadialGlow(g, cardPath, new PointF(cardBounds.Right - cardBounds.Width * 0.2f, cardBounds.Top + cardBounds.Height * 0.5f), 
                        cardBounds.Width * 0.6f, Color.FromArgb(38, ColorAccent));

                    // 左侧蓝色光晕 (at 20% 50%, rgba(0, 168, 255, 0.25))
                    DrawRadialGlow(g, cardPath, new PointF(cardBounds.Left + cardBounds.Width * 0.2f, cardBounds.Top + cardBounds.Height * 0.5f), 
                        cardBounds.Width * 0.6f, Color.FromArgb(64, ColorPrimary));
                }
                else
                {
                    // 未激活态背景：--md-surface-card (#141720)
                    using (SolidBrush baseBg = new SolidBrush(ColorSurfaceCard))
                    {
                        g.FillPath(baseBg, cardPath);
                    }
                }

                // 未激活时的默认细边框 (border: 1px solid rgba(255, 255, 255, 0.08))
                if (!_checked)
                {
                    using (Pen inactiveBorderPen = new Pen(Color.FromArgb(20, 255, 255, 255), 1f))
                    {
                        g.DrawPath(inactiveBorderPen, cardPath);
                    }
                }

                // 裁剪视口，防止网格与扫光束超出卡片圆角
                g.SetClip(cardPath);

                // 3. 背景网格与光晕流动动画 (对应 CSS .cyber-bg-grid & gridMove 8s)
                if (_checked)
                {
                    using (Pen gridPen = new Pen(Color.FromArgb(13, 255, 255, 255), 1f))
                    {
                        for (float x = cardBounds.Left + _gridOffset - 16f; x < cardBounds.Right; x += 16f)
                        {
                            g.DrawLine(gridPen, x, cardBounds.Top, x, cardBounds.Bottom);
                        }
                        for (float y = cardBounds.Top + _gridOffset - 16f; y < cardBounds.Bottom; y += 16f)
                        {
                            g.DrawLine(gridPen, cardBounds.Left, y, cardBounds.Right, y);
                        }
                    }
                }

                // 4. 斜向扫光束 (对应 CSS .cyber-light-sweep & @keyframes sweepAnim 0.6s)
                if (_checked && elapsedActiveTime <= 0.6f)
                {
                    float sweepProgress = elapsedActiveTime / 0.6f; // 0.0 -> 1.0
                    float sweepWidth = cardBounds.Width * 0.6f;
                    float startX = cardBounds.Left - sweepWidth;
                    float totalDistance = cardBounds.Width * 3.0f;
                    float currentX = startX + (totalDistance * sweepProgress);

                    RectangleF sweepRect = new RectangleF(currentX, cardBounds.Top, sweepWidth, cardBounds.Height);

                    // 倾斜矩阵 (transform: skewX(-25deg))
                    GraphicsState state = g.Save();
                    Matrix skewMatrix = g.Transform;
                    skewMatrix.Multiply(new Matrix(1, 0, (float)Math.Tan(-25 * Math.PI / 180.0), 1, 0, 0));
                    g.Transform = skewMatrix;

                    using (LinearGradientBrush sweepBrush = new LinearGradientBrush(
                        sweepRect, Color.Transparent, Color.FromArgb(64, 255, 255, 255), 0f))
                    {
                        ColorBlend blend = new ColorBlend(3)
                        {
                            Colors = new Color[] { Color.Transparent, Color.FromArgb(64, 255, 255, 255), Color.Transparent },
                            Positions = new float[] { 0f, 0.5f, 1.0f }
                        };
                        sweepBrush.InterpolationColors = blend;
                        g.FillRectangle(sweepBrush, sweepRect);
                    }
                    g.Restore(state);
                }

                g.ResetClip();

                // 5. 内容区绘制 (图标 + 标题 + 描述)
                float contentPaddingLeft = cardBounds.Left + 24f;

                // 左侧 Icon 绘制 (对应 CSS .card-icon & active transform)
                float iconSize = 30f;
                PointF iconCenter = new PointF(contentPaddingLeft + iconSize / 2f, cardBounds.Top + cardBounds.Height / 2f);

                GraphicsState iconState = g.Save();
                if (_checked)
                {
                    // 激活态：放大 1.25 + 旋转 15deg
                    g.TranslateTransform(iconCenter.X, iconCenter.Y);
                    g.ScaleTransform(1.25f, 1.25f);
                    g.RotateTransform(15f);
                    g.TranslateTransform(-iconCenter.X, -iconCenter.Y);
                }

                Color currentIconColor = _checked ? ColorAccent : ColorOnSurfaceVariant;
                DrawDefaultCyberIcon(g, iconCenter, iconSize, currentIconColor);
                g.Restore(iconState);

                // 文本区绘制 (对应 CSS .card-title & .card-desc)
                float textLeft = contentPaddingLeft + iconSize + 16f;
                Color currentTitleColor = _checked ? Color.White : ColorOnSurface;
                Color currentDescColor = _checked ? ColorPrimary : ColorOnSurfaceVariant;

                using (Font titleFont = new Font(Font.FontFamily, 10.5f, _checked ? FontStyle.Bold : FontStyle.Regular))
                using (Font descFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
                using (SolidBrush titleBrush = new SolidBrush(currentTitleColor))
                using (SolidBrush descBrush = new SolidBrush(currentDescColor))
                {
                    g.DrawString(_title, titleFont, titleBrush, textLeft, cardBounds.Top + 18f);
                    g.DrawString(_description, descFont, descBrush, textLeft, cardBounds.Top + 42f);
                }

                // 6. 右侧对勾徽章与爆裂冲击波 (对应 CSS .badge-box & shockwave-1/2)
                float badgeSize = 36f;
                PointF badgeCenter = new PointF(cardBounds.Right - 24f - badgeSize / 2f, cardBounds.Top + cardBounds.Height / 2f);

                // 绘制双重冲击波 (@keyframes pulseWave 0.6s & 延迟 0.15s)
                if (_checked)
                {
                    DrawPulseShockwave(g, badgeCenter, elapsedActiveTime, 0.0f);  // 冲击波 1
                    DrawPulseShockwave(g, badgeCenter, elapsedActiveTime, 0.15f); // 冲击波 2
                }

                // 绘制对勾徽章主体 (对应 CSS .card-badge & active transform scale 1.15 + rotate 360deg)
                GraphicsState badgeState = g.Save();
                if (_checked)
                {
                    g.TranslateTransform(badgeCenter.X, badgeCenter.Y);
                    g.ScaleTransform(1.15f, 1.15f);
                    g.RotateTransform(360f * Math.Min(1.0f, elapsedActiveTime / 0.35f)); // 平滑旋转一圈
                    g.TranslateTransform(-badgeCenter.X, -badgeCenter.Y);
                }

                RectangleF badgeBounds = new RectangleF(badgeCenter.X - badgeSize / 2f, badgeCenter.Y - badgeSize / 2f, badgeSize, badgeSize);

                if (_checked)
                {
                    // 激活态：渐变填充 (135deg, var(--md-primary), var(--md-accent))
                    using (LinearGradientBrush badgeBg = new LinearGradientBrush(
                        badgeBounds, ColorPrimary, ColorAccent, 135f))
                    {
                        g.FillEllipse(badgeBg, badgeBounds);
                    }
                }
                else
                {
                    // 未激活态：半透明浅色 + 描边
                    using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(13, 255, 255, 255)))
                    using (Pen badgeBorder = new Pen(Color.FromArgb(38, 255, 255, 255), 1f))
                    {
                        g.FillEllipse(badgeBg, badgeBounds);
                        g.DrawEllipse(badgeBorder, badgeBounds);
                    }
                }

                // 徽章内部 Checkmark 对勾线条绘制
                Color checkmarkColor = _checked ? Color.FromArgb(4, 16, 25) : ColorOnSurfaceVariant;
                using (Pen checkPen = new Pen(checkmarkColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    PointF p1 = new PointF(badgeCenter.X - 7f, badgeCenter.Y);
                    PointF p2 = new PointF(badgeCenter.X - 2f, badgeCenter.Y + 5f);
                    PointF p3 = new PointF(badgeCenter.X + 7f, badgeCenter.Y - 5f);
                    g.DrawLines(checkPen, new PointF[] { p1, p2, p3 });
                }

                g.Restore(badgeState);
            }
        }

        // ═══════════════════════════════════════════════════
        // GDI+ 绘图辅助函数
        // ═══════════════════════════════════════════════════

        // 辅助 1：绘制扩散冲击波 (对应 CSS @keyframes pulseWave)
        private void DrawPulseShockwave(Graphics g, PointF center, float elapsedTime, float delaySeconds)
        {
            float waveTime = elapsedTime - delaySeconds;
            if (waveTime < 0f || waveTime > 0.6f) return;

            float progress = waveTime / 0.6f; // 0.0 -> 1.0
            float scale = 0.8f + (1.6f * progress); // scale(0.8) -> scale(2.4)
            float size = 36f * scale;

            float alpha = (1.0f - progress) * 255f; // Opacity 1 -> 0
            Color currentWaveColor = LerpColor(ColorAccent, ColorPink, progress);

            RectangleF waveBounds = new RectangleF(center.X - size / 2f, center.Y - size / 2f, size, size);
            using (Pen wavePen = new Pen(Color.FromArgb((int)alpha, currentWaveColor), 2f))
            {
                g.DrawEllipse(wavePen, waveBounds);
            }
        }

        // 辅助 2：绘制径向发光 (模拟 CSS radial-gradient)
        private void DrawRadialGlow(Graphics g, GraphicsPath clipPath, PointF center, float radius, Color color)
        {
            RectangleF bounds = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterPoint = center;
                    pgb.CenterColor = color;
                    pgb.SurroundColors = new Color[] { Color.Transparent };
                    
                    GraphicsState state = g.Save();
                    g.SetClip(clipPath, CombineMode.Intersect);
                    g.FillPath(pgb, path);
                    g.Restore(state);
                }
            }
        }

        // 辅助 3：绘制默认 Vector 图标
        private void DrawDefaultCyberIcon(Graphics g, PointF center, float size, Color color)
        {
            float r = size / 2f;
            RectangleF iconBounds = new RectangleF(center.X - r, center.Y - r, size, size);

            using (Pen pen = new Pen(color, 2.2f) { LineJoin = LineJoin.Round })
            {
                g.DrawEllipse(pen, iconBounds);
                
                // 绘制内嵌勾号对勾
                PointF p1 = new PointF(center.X - 5f, center.Y);
                PointF p2 = new PointF(center.X - 1f, center.Y + 4f);
                PointF p3 = new PointF(center.X + 5f, center.Y - 3f);
                g.DrawLines(pen, new PointF[] { p1, p2, p3 });
            }
        }

        // 辅助 4：颜色线性插值 (Lerp)
        private Color LerpColor(Color c1, Color c2, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(r, g, b);
        }

        // 辅助 5：创建圆角矩形 Path
        private GraphicsPath CreateRoundRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}