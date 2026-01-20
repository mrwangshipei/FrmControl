using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class VerticalProgressBar : Control
{
    private int _value = 60;

    [DefaultValue(60)]
    public int Value
    {
        get => _value;
        set
        {
            int v = Math.Max(0, Math.Min(100, value));
            if (_value != v) { _value = v; Invalidate(); }
        }
    }

    [DefaultValue(true)]
    public bool ShowText { get; set; } = true;

    [DefaultValue("{0}%")]
    public string TextFormat { get; set; } = "{0}%";

    [DefaultValue(24)]
    public int BarWidth { get; set; } = 24;

    // ✅ 更小的默认 padding，并且都可配置
    [DefaultValue(4)]
    public int PaddingTop { get; set; } = 4;

    [DefaultValue(4)]
    public int PaddingBottom { get; set; } = 4;

    [DefaultValue(4)]
    public int PaddingSide { get; set; } = 4;

    // ✅ 圆角矩形的圆角半径（不是胶囊）
    [DefaultValue(8)]
    public int CornerRadius { get; set; } = 8;

    [DefaultValue(typeof(Color), "MediumSeaGreen")]
    public Color BarColor { get; set; } = Color.MediumSeaGreen;

    [DefaultValue(typeof(Color), "Gainsboro")]
    public Color TrackColor { get; set; } = Color.Gainsboro;

    [DefaultValue(typeof(Color), "DimGray")]
    public Color TextColor { get; set; } = Color.DimGray;

    [DefaultValue(typeof(Color), "White")]
    public Color InBarTextColor { get; set; } = Color.White;

    // 文本与柱顶的间距
    [DefaultValue(4)]
    public int TextGap { get; set; } = 4;

    public VerticalProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.UserPaint
               | ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        Size = new Size(80, 220);
    }

    // ✅ 真·透明叠加：先让父容器把背景画到我这里
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (BackColor != Color.Transparent)
        {
            base.OnPaintBackground(e);
            return;
        }

        if (Parent != null)
        {
            var state = e.Graphics.Save();
            try
            {
                e.Graphics.TranslateTransform(-Left, -Top);
                var pe = new PaintEventArgs(e.Graphics, Parent.ClientRectangle);
                InvokePaintBackground(Parent, pe);
                InvokePaint(Parent, pe);
            }
            finally
            {
                e.Graphics.Restore(state);
            }
        }
        else
        {
            e.Graphics.Clear(SystemColors.Control);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        Rectangle client = ClientRectangle;
        if (client.Width <= 2 || client.Height <= 2) return;

        // 计算轨道区域（确保在可见区域内）
        int trackHeight = client.Height - PaddingTop - PaddingBottom;
        if (trackHeight <= 2) return;

        int x = client.Left + PaddingSide + (Math.Max(2, BarWidth));
        // 让柱子尽量靠左（你也可以改成居中：cx = client.Width/2）
        int barX = client.Left + PaddingSide;
        int barY = client.Top + PaddingTop;
        int barW = Math.Min(BarWidth, client.Width - PaddingSide * 2);
        if (barW <= 2) return;

        Rectangle trackRect = new Rectangle(barX, barY, barW, trackHeight);

        // 轨道圆角矩形
        using (var trackPath = CreateRoundedRectPath(trackRect, CornerRadius))
        using (var trackBrush = new SolidBrush(TrackColor))
        {
            g.FillPath(trackBrush, trackPath);
        }

        // fill 区域
        int fillHeight = (int)Math.Round(trackHeight * (Value / 100.0));
        fillHeight = Math.Max(0, Math.Min(trackHeight, fillHeight));

        Rectangle fillRect = new Rectangle(
            trackRect.X,
            trackRect.Bottom - fillHeight,
            trackRect.Width,
            fillHeight
        );

        if (fillHeight > 0)
        {
            // fill 顶部圆角：当 fill 没有到顶时，顶部也保持圆角（更像图里的“圆柱”质感）
            int r = Math.Min(CornerRadius, fillRect.Width / 2);
            using (var fillPath = CreateRoundedRectPath(fillRect, r))
            {
                // ✅ 立体感：横向渐变（左暗-中亮-右暗）
                using (var lg = new LinearGradientBrush(fillRect, Color.Empty, Color.Empty, 0f))
                {
                    Color c1 = Darken(BarColor, 0.25f);
                    Color c2 = Lighten(BarColor, 0.25f);
                    Color c3 = Darken(BarColor, 0.20f);

                    lg.InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { c1, c2, c3 },
                        Positions = new[] { 0f, 0.5f, 1f }
                    };

                    g.FillPath(lg, fillPath);
                }

                // 轻微高光（可选，但一般更接近你图里的立体效果）
                Rectangle gloss = fillRect;
                gloss.Width = Math.Max(2, fillRect.Width / 4);
                using (var glossPath = CreateRoundedRectPath(gloss, r))
                using (var glossBrush = new LinearGradientBrush(gloss, Color.FromArgb(90, Color.White), Color.FromArgb(0, Color.White), 90f))
                {
                    g.FillPath(glossBrush, glossPath);
                }
            }
        }

        // 文字：外部 -> 内部 -> 右侧
        // ✅ 文字：默认在 fill 内部底部；fill 太矮才放到顶部（外部）
        // ✅ 文字：fill 内部靠上（优先）；fill 太矮才放到顶部（外部）
        if (ShowText)
        {
            string text = string.Format(TextFormat, Value);
            SizeF ts = g.MeasureString(text, Font);

            // fill 顶端；Value=0 时 fillHeight=0
            int fillTopY = (fillHeight > 0) ? fillRect.Top : trackRect.Bottom;

            // 1) 优先：文字放在 fill 内部靠上
            float inX = trackRect.Left + (trackRect.Width - ts.Width) / 2f;
            float inY = fillRect.Top + TextGap;

            // 需要 fill 足够容纳文字（上下各留一点边距更稳）
            bool canInTop =
                fillHeight >= (int)(ts.Height + TextGap * 2);

            if (canInTop)
            {
                DrawTextOutlined(
                    g, text, Font,
                    InBarTextColor,
                    Color.FromArgb(160, Color.Black),
                    inX, inY, 2f
                );
            }
            else
            {
                // 2) fill 太矮：放到顶部外侧（在当前进度顶部上方）
                float outX = trackRect.Left + (trackRect.Width - ts.Width) / 2f;
                float outY = fillTopY - TextGap - ts.Height;

                // 防止越界
                outY = Math.Max(0, outY);

                DrawTextWithShadow(g, text, Font, TextColor, outX, outY);
            }
        }


    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        radius = Math.Max(0, radius);
        int d = Math.Min(Math.Min(radius * 2, rect.Width), rect.Height);
        int r = d / 2;

        GraphicsPath path = new GraphicsPath();
        if (r <= 0)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }

        // 4 个角
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Darken(Color c, float amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        return Color.FromArgb(c.A,
            (int)(c.R * (1 - amount)),
            (int)(c.G * (1 - amount)),
            (int)(c.B * (1 - amount)));
    }

    private static Color Lighten(Color c, float amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        return Color.FromArgb(c.A,
            (int)(c.R + (255 - c.R) * amount),
            (int)(c.G + (255 - c.G) * amount),
            (int)(c.B + (255 - c.B) * amount));
    }

    private static void DrawTextWithShadow(Graphics g, string text, Font font, Color color, float x, float y)
    {
        using (var shadow = new SolidBrush(Color.FromArgb(80, Color.Black)))
        using (var brush = new SolidBrush(color))
        {
            g.DrawString(text, font, shadow, x + 1, y + 1);
            g.DrawString(text, font, brush, x, y);
        }
    }

    private static void DrawTextOutlined(Graphics g, string text, Font font, Color fill, Color stroke, float x, float y, float strokeWidth)
    {
        using (GraphicsPath p = new GraphicsPath())
        {
            p.AddString(text, font.FontFamily, (int)font.Style, g.DpiY * font.Size / 72f,
                new PointF(x, y), StringFormat.GenericDefault);

            using (Pen pen = new Pen(stroke, strokeWidth) { LineJoin = LineJoin.Round })
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.DrawPath(pen, p);
                g.FillPath(brush, p);
            }
        }
    }
}
