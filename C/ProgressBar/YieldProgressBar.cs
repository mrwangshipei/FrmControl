using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class YieldProgressBar : Control
{
	private float _progress = 0f; // 0..100
	private float _yield = 0f;    // 0..100

	public YieldProgressBar()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint |
				 ControlStyles.OptimizedDoubleBuffer |
				 ControlStyles.SupportsTransparentBackColor|
				 ControlStyles.ResizeRedraw |
				 ControlStyles.UserPaint, true);

		Height = 10;
		BackColor = Color.Transparent;
	}

	[Category("Behavior")]
	[Description("Progress percent (0..100). Controls filled length.")]
	public float Progress
	{
		get => _progress;
		set
		{
			_progress = Clamp(value, 0f, 100f);
			Invalidate();
		}
	}

	[Category("Behavior")]
	[Description("Yield percent (0..100). Controls color from red->green.")]
	public float Yield
	{
		get => _yield;
		set
		{
			_yield = Clamp(value, 0f, 100f);
			Invalidate();
		}
	}
	[Category("Appearance")]
	[Description("Show percentage text.")]
	public bool ShowPercentText { get; set; } = true;

	[Category("Appearance")]
	[Description("Font for percentage text.")]
	public Font PercentFont { get; set; } = new Font("Segoe UI", 8f, FontStyle.Bold);

	[Category("Appearance")]
	[Description("Text color. If Empty, auto picks black/white based on background.")]
	public Color PercentTextColor { get; set; } = Color.Empty;

	[Category("Appearance")]
	[Description("Text format, e.g. \"{0:0}%\" uses Progress.")]
	public string PercentTextFormat { get; set; } = "{0:0}%";

	[Category("Appearance")]
	[Description("Track/background color.")]
	public Color TrackColor { get; set; } = Color.FromArgb(0xE6, 0xF3, 0xFA); // light blue

	[Category("Appearance")]
	[Description("Padding inside the control.")]
	public int InnerPadding { get; set; } = 0;

	[Category("Appearance")]
	[Description("Corner radius. If 0, uses pill radius = Height/2.")]
	public int CornerRadius { get; set; } = 0;
	

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		var g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;
		g.CompositingQuality = CompositingQuality.HighQuality;

		Rectangle rect = ClientRectangle;
		rect.Inflate(-InnerPadding, -InnerPadding);
		if (rect.Width <= 0 || rect.Height <= 0) return;

		int radius = CornerRadius > 0 ? CornerRadius : rect.Height / 2;
		radius = Math.Max(1, radius);
		radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2); // 防止半径超过矩形

		// 1) Draw track
		using (var trackPath = CreateRoundedRectPath(rect, radius))
		using (var trackBrush = new SolidBrush(TrackColor))
		{
			g.FillPath(trackBrush, trackPath);
		}

		// 2) Compute filled rect
		float fillRatio = _progress / 100f;
		int fillWidth = (int)Math.Round(rect.Width * fillRatio);
		if (fillWidth <= 0) return;
		if (fillRatio > 0 && fillWidth < 1) fillWidth = 1;
		if (fillWidth <= 0 || rect.Height <= 0) return;

		Rectangle fillRect = new Rectangle(rect.X, rect.Y, fillWidth, rect.Height);

		// 3) Compute yield-based base color (bias: >=60% looks greener)
		Color baseColor = YieldToColorBiased(_yield / 100f, greenStartsAt: 0.60f);

		// Make left darker, right lighter to match your screenshot
		Color left = AdjustLightness(baseColor, -0.12f);
		Color right = AdjustLightness(baseColor, +0.10f);
		if (rect.Width < 2 || rect.Height < 2) return;

		if (fillRect.Width < 2)
		{
			using var solid = new SolidBrush(baseColor);
			g.FillRectangle(solid, fillRect);
			return;
		}
		// 4) Draw filled with gradient clipped to pill shape
		// 4) Draw filled with gradient clipped to pill shape
		using (var clipPath = CreateRoundedRectPath(rect, radius))
		{
			// 更安全的方式：Clone 一份旧 clip，用 CombineMode.Replace 恢复
			using (var oldClip = g.Clip?.Clone())
			{
				g.SetClip(clipPath, CombineMode.Replace);

				// 关键：用 rect（整条轨道）创建渐变刷，而不是 fillRect（可能宽度=1）
				using (var grad = new LinearGradientBrush(
					rect,
					left,
					right,
					LinearGradientMode.Horizontal))
				{
					// Clamp 现在就很安全了
					//grad.WrapMode = WrapMode.Clamp;

					// 只填充 fillRect
					g.FillRectangle(grad, fillRect);
				}

				if (oldClip != null)
					g.SetClip(oldClip, CombineMode.Replace);
				else
					g.ResetClip();
			}
		}
		// 5) Draw percent text
		if (ShowPercentText)
		{
			string text = string.Format(PercentTextFormat, _progress);

			// Measure
			Size textSize = TextRenderer.MeasureText(text, PercentFont);

			// Center in rect (inside padding already applied)
			int tx = rect.X + (rect.Width - textSize.Width) / 2;
			int ty = rect.Y + (rect.Height - textSize.Height) / 2;

			Rectangle textRect = new Rectangle(tx, ty, textSize.Width, textSize.Height);

			// Decide background color under text for auto-contrast
			// If text center is inside filled area, use baseColor as bg, otherwise TrackColor
			int cx = textRect.X + textRect.Width / 2;
			bool onFill = cx <= (rect.X + fillWidth);

			Color bg = onFill ? baseColor : TrackColor;
			Color fg = PercentTextColor.IsEmpty ? GetContrastTextColor(bg) : PercentTextColor;

			// Draw with TextRenderer (better crisp text in WinForms)
			TextRenderer.DrawText(
				g,
				text,
				PercentFont,
				textRect,
				fg,
				TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
			);
		}

	}
	private static Color GetContrastTextColor(Color bg)
	{
		// Relative luminance
		double r = bg.R / 255.0;
		double g = bg.G / 255.0;
		double b = bg.B / 255.0;

		double luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
		return luminance < 0.55 ? Color.White : Color.Black;
	}

	private static GraphicsPath CreateRoundedRectPath(Rectangle r, int radius)
	{
		var path = new GraphicsPath();

		if (radius <= 0 || r.Width <= 1 || r.Height <= 1)
		{
			path.AddRectangle(r);
			path.CloseFigure();
			return path;
		}

		int d = radius * 2;
		if (d > r.Width) d = r.Width;
		if (d > r.Height) d = r.Height;

		path.AddArc(r.X, r.Y, d, d, 180, 90);
		path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
		path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
		path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
		path.CloseFigure();
		return path;
	}

	/// <summary>
	/// Map yield t(0..1) to a color: red->yellow->green, biased so that >= greenStartsAt looks green-ish.
	/// </summary>
	private static Color YieldToColorBiased(float t, float greenStartsAt)
	{
		t = Clamp(t, 0f, 1f);
		greenStartsAt = Clamp(greenStartsAt, 0.01f, 0.99f);

		// Piecewise hue mapping:
		// [0, greenStartsAt] : 0..60 (red->yellow)
		// [greenStartsAt, 1] : 60..120 (yellow->green)
		float hue;
		if (t <= greenStartsAt)
		{
			float u = t / greenStartsAt;      // 0..1
			hue = Lerp(0f, 60f, u);
		}
		else
		{
			float u = (t - greenStartsAt) / (1f - greenStartsAt); // 0..1
			hue = Lerp(60f, 120f, u);
		}

		return ColorFromHsl(hue, 0.90f, 0.52f);
	}

	private static Color AdjustLightness(Color c, float deltaL)
	{
		// Convert to HSL, adjust L then convert back
		RgbToHsl(c, out float h, out float s, out float l);
		l = Clamp(l + deltaL, 0f, 1f);
		return ColorFromHsl(h, s, l);
	}

	private static void RgbToHsl(Color c, out float h, out float s, out float l)
	{
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
		float max = Math.Max(r, Math.Max(g, b));
		float min = Math.Min(r, Math.Min(g, b));
		float d = max - min;

		l = (max + min) / 2f;

		if (d == 0f)
		{
			h = 0f; s = 0f;
			return;
		}

		s = d / (1f - Math.Abs(2f * l - 1f));

		if (max == r) h = 60f * (((g - b) / d) % 6f);
		else if (max == g) h = 60f * (((b - r) / d) + 2f);
		else h = 60f * (((r - g) / d) + 4f);

		if (h < 0f) h += 360f;
	}

	private static Color ColorFromHsl(float h, float s, float l)
	{
		// h in degrees [0..360), s,l in [0..1]
		h = (h % 360f + 360f) % 360f;
		s = Clamp(s, 0f, 1f);
		l = Clamp(l, 0f, 1f);

		float c = (1f - Math.Abs(2f * l - 1f)) * s;
		float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
		float m = l - c / 2f;

		float r1, g1, b1;
		if (h < 60f) { r1 = c; g1 = x; b1 = 0; }
		else if (h < 120f) { r1 = x; g1 = c; b1 = 0; }
		else if (h < 180f) { r1 = 0; g1 = c; b1 = x; }
		else if (h < 240f) { r1 = 0; g1 = x; b1 = c; }
		else if (h < 300f) { r1 = x; g1 = 0; b1 = c; }
		else { r1 = c; g1 = 0; b1 = x; }

		int R = (int)Math.Round((r1 + m) * 255);
		int G = (int)Math.Round((g1 + m) * 255);
		int B = (int)Math.Round((b1 + m) * 255);

		return Color.FromArgb(ClampByte(R), ClampByte(G), ClampByte(B));
	}

	private static float Lerp(float a, float b, float t) => a + (b - a) * t;

	private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
	private static int ClampByte(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);
}
