using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class StatusLampLabel : Control
{
	private StatusLamp lamp;

	// ===== 可配置属性 =====

	private int cornerRadius = 10;
	public int CornerRadius
	{
		get => cornerRadius;
		set
		{
			if (cornerRadius == value) return;
			cornerRadius = value;
			Invalidate(); // 只重绘
		}
	}

	private int lampSize = 18;
	public int LampSize
	{
		get => lampSize;
		set
		{
			if (lampSize == value) return;
			lampSize = value;
			UpdateLayout(); // 更新位置和大小
			Invalidate();   // 重绘背景和文字
		}
	}
	private bool autoTextByStatus = true;
	public bool AutoTextByStatus
	{
		get => autoTextByStatus;
		set
		{
			if (autoTextByStatus == value) return;
			autoTextByStatus = value;
			Invalidate();
		}
	}

	private string displayText = "";
	public string DisplayText
	{
		get => displayText;
		set
		{
			if (displayText == value) return;
			displayText = value;
			Invalidate();
		}
	}

	public StatusLamp.LampStatus Status
	{
		get => lamp.Status;
		set
		{
			if (lamp.Status == value) return;
			lamp.Status = value;
			Invalidate();
		}
	}

	// ===== 构造 =====
	public StatusLampLabel()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint |
				 ControlStyles.OptimizedDoubleBuffer |
				 ControlStyles.SupportsTransparentBackColor|
				 ControlStyles.UserPaint, true);

		lamp = new StatusLamp();
		lamp.BackColor = Color.Transparent;
		Controls.Add(lamp);

		Height = 36;
		Width = 180;

		UpdateLayout();
	}

	// ===== 高度变化自动布局 =====
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		UpdateLayout();
	}

	private void UpdateLayout()
	{
		lamp?.Size = new Size(LampSize, LampSize);
		lamp?.Location = new Point(
			8,
			(Height - LampSize) / 2
		);
	}

	// ===== 绘制 =====
	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;

		Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

		Color baseColor = GetBaseColor(Status);
		Color backColor = ControlPaint.Light(baseColor, 0.8f);
		Color borderColor = ControlPaint.Dark(baseColor, 0.4f);

		// 1️⃣ 圆角矩形背景
		using (GraphicsPath path = CreateRoundRect(rect, CornerRadius))
		using (SolidBrush brush = new SolidBrush(backColor))
		using (Pen pen = new Pen(borderColor))
		{
			g.FillPath(brush, path);
			g.DrawPath(pen, path);
		}

		// 2️⃣ 文本
		string text = AutoTextByStatus ? GetStatusText(Status) : DisplayText;

	//	using (Font font = Font)
		using (Brush brush = new SolidBrush(ForeColor))
		{
			if (Width - lamp.Right - 12 <= 0)
			{
				return;
			}
			if (Height < Font.Size)
			{

				return;
			}
			Rectangle textRect = new Rectangle(
				lamp.Right + 8,
				0,
				Width - lamp.Right - 12,
				Height
			);

			StringFormat sf = new StringFormat
			{
				LineAlignment = StringAlignment.Center,
				Alignment = StringAlignment.Near
			};

			g.DrawString(text, Font, brush, textRect, sf);
		}
	}

	// ===== 状态文字 =====
	private string GetStatusText(StatusLamp.LampStatus status)
	{
		switch (status)
		{
			case StatusLamp.LampStatus.CameraOn: return "Camera On";
			case StatusLamp.LampStatus.CameraOff: return "Camera Off";
			case StatusLamp.LampStatus.Recognizing: return "Recognizing";
			case StatusLamp.LampStatus.Paused: return "Paused";
			case StatusLamp.LampStatus.Error: return "Error";
			default: return "";
		}
	}

	// ===== 状态颜色 =====
	private Color GetBaseColor(StatusLamp.LampStatus status)
	{
		switch (status)
		{
			case StatusLamp.LampStatus.CameraOn: return Color.LimeGreen;
			case StatusLamp.LampStatus.Recognizing: return Color.Orange;
			case StatusLamp.LampStatus.Paused: return Color.Gold;
			case StatusLamp.LampStatus.Error: return Color.Red;
			default: return Color.Gray;
		}
	}

	// ===== 圆角路径 =====
	private GraphicsPath CreateRoundRect(Rectangle rect, int radius)
	{
		GraphicsPath path = new GraphicsPath();
		int d = radius * 2;

		path.AddArc(rect.X, rect.Y, d, d, 180, 90);
		path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
		path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
		path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
		path.CloseFigure();

		return path;
	}
}
