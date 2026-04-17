using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace IndustrialTip
{
	internal class TipForm : Form
	{
		private static TipForm _instance;
		public static TipForm Instance => _instance ?? (_instance = new TipForm());

		private Label lblText;
		private Timer flashTimer;
		private bool flashState;
		private Color normalColor;

		private readonly int expandedWidth = 280;
		private readonly int collapsedWidth = 24;
		private readonly int height = 60;
		private readonly int radius = 12;

		private TipForm()
		{
			InitForm();
			InitControls();
		}

		protected override bool ShowWithoutActivation => true;

		private void InitForm()
		{
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			TopMost = true;
			DoubleBuffered = true;
			StartPosition = FormStartPosition.Manual;

			normalColor = Color.FromArgb(46, 46, 46);
			BackColor = normalColor;

			Size = new Size(collapsedWidth, height);
		}

		private void InitControls()
		{
			lblText = new Label
			{
				Dock = DockStyle.Fill,
				ForeColor = Color.White,
				TextAlign = ContentAlignment.MiddleLeft,
				Padding = new Padding(12, 0, 12, 0),
				AutoEllipsis = true
			};

			Controls.Add(lblText);

			SizeChanged += (s, e) =>
			{
				UpdateRightRoundedRegion(radius);
				StickToBottomRight();
			};
		}

		public void SetText(string text)
		{
			lblText.Text = text;
		}

		public void SetBackColor(Color c)
		{
			normalColor = c;
			BackColor = c;
		}

		public void ShowSafe()
		{
			Expand();
			if (!Visible)
				Show();
			ForceTopMost();
		}

		#region 展开 / 收回（无动画）

		private void Expand()
		{
			SuspendLayout();
			Size = new Size(expandedWidth, height);
			ResumeLayout();
		}

		private void Collapse()
		{
			SuspendLayout();
			Size = new Size(collapsedWidth, height);
			ResumeLayout();
		}

		#endregion

		#region 强制置顶 + 闪烁

		public void StartAlarmFlash(Color alarmColor)
		{
			if (flashTimer == null)
			{
				flashTimer = new Timer { Interval = 500 };
				flashTimer.Tick += (s, e) =>
				{
					flashState = !flashState;
					BackColor = flashState ? alarmColor : normalColor;
					ForceTopMost();
				};
			}

			flashTimer.Start();
		}

		public void StopAlarmFlash()
		{
			flashTimer?.Stop();
			BackColor = normalColor;
		}

		private void ForceTopMost()
		{
			TopMost = false;
			TopMost = true;
		}

		#endregion

		#region 右侧圆角 Region

		private void UpdateRightRoundedRegion(int radius)
		{
			int d = radius * 2;
			GraphicsPath path = new GraphicsPath();

			path.AddLine(0, 0, Width - radius, 0);
			path.AddArc(Width - d, 0, d, d, 270, 90);
			path.AddLine(Width, radius, Width, Height - radius);
			path.AddArc(Width - d, Height - d, d, d, 0, 90);
			path.AddLine(Width - radius, Height, 0, Height);
			path.CloseFigure();

			Region?.Dispose();
			Region = new Region(path);
		}

		#endregion

		#region 贴屏幕右下角（多屏安全）

		private void StickToBottomRight()
		{
			Screen screen = Screen.FromControl(Owner ?? this);
			Rectangle area = screen.WorkingArea;

			Left = area.Right - Width - 2;
			Top = area.Bottom - Height - 2;
		}

		#endregion

		#region 阴影（静态）

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			using (GraphicsPath shadow = new GraphicsPath())
			{
				shadow.AddArc(Width - 18, 4, 14, 14, 270, 90);
				shadow.AddArc(Width - 18, Height - 18, 14, 14, 0, 90);
				shadow.AddLine(Width - 4, 11, Width - 4, Height - 11);

				using (Pen p = new Pen(Color.FromArgb(80, 0, 0, 0), 6))
				{
					e.Graphics.DrawPath(p, shadow);
				}
			}
		}

		#endregion
	}
}
