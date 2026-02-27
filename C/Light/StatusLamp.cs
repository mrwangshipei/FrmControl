using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class StatusLamp : Control
{
	public enum LampStatus
	{
		CameraOn,      // 绿色
		CameraOff,     // 灰色
		Recognizing,   // 橙色
		Paused,        // 黄色
		Error          // 红色
	}

	private LampStatus _status = LampStatus.CameraOff;
	public LampStatus Status
	{
		get => _status;
		set { _status = value; Invalidate(); }
	}

	public StatusLamp()
	{
		Size = new Size(24, 24);
		SetStyle(ControlStyles.AllPaintingInWmPaint |
				 ControlStyles.OptimizedDoubleBuffer |
				 ControlStyles.SupportsTransparentBackColor|
				 ControlStyles.UserPaint, true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;

		Rectangle rect = new Rectangle(1, 1, Width - 2, Height - 2);

		Color baseColor = GetBaseColor(Status);

		// ===== 1️⃣ 画凸起的主体（径向渐变） =====
		using (GraphicsPath path = new GraphicsPath())
		{
			path.AddEllipse(rect);
			using (PathGradientBrush pgb = new PathGradientBrush(path))
			{
				pgb.CenterColor = ControlPaint.Light(baseColor, 0.6f);
				pgb.SurroundColors = new[] { ControlPaint.Dark(baseColor, 0.5f) };
				g.FillEllipse(pgb, rect);
			}
		}

		// ===== 2️⃣ 外圈描边（增强立体感） =====
		using (Pen pen = new Pen(ControlPaint.Dark(baseColor, 0.7f), 1))
		{
			g.DrawEllipse(pen, rect);
		}

		// ===== 3️⃣ 高光（左上角反光） =====
		Rectangle highlight = new Rectangle(
			rect.X + rect.Width / 5,
			rect.Y + rect.Height / 5,
			rect.Width / 3,
			rect.Height / 3);

		using (GraphicsPath hp = new GraphicsPath())
		{
			hp.AddEllipse(highlight);
			using (PathGradientBrush hBrush = new PathGradientBrush(hp))
			{
				hBrush.CenterColor = Color.FromArgb(180, Color.White);
				hBrush.SurroundColors = new[] { Color.Transparent };
				g.FillEllipse(hBrush, highlight);
			}
		}
	}

	private Color GetBaseColor(LampStatus status)
	{
		switch (status)
		{
			case LampStatus.CameraOn:
				return Color.LimeGreen;
			case LampStatus.Recognizing:
				return Color.Orange;
			case LampStatus.Paused:
				return Color.Gold;
			case LampStatus.Error:
				return Color.Red;
			case LampStatus.CameraOff:
			default:
				return Color.Gray;
		}
	}
}
