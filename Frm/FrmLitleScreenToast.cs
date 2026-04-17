using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

public sealed class FrmLitleScreenToast : Form
{
	private readonly Timer _timer;
	private readonly Label _titleLabel;
	private readonly Label _msgLabel;
	private readonly Panel _contentPanel;

	private FrmLitleScreenToast(string title, string message, int durationMs, bool isSuccess)
	{
		InitializeComponent();
		// ===== Form 基本属性（无动画、无任务栏图标、置顶）=====
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
	
		TopMost = true;
		DoubleBuffered = true;

	_contentPanel = new Panel
		{
			BackColor = Color.Transparent,
			Size = new Size(520, 180)
	};
		_contentPanel.Parent = this;

		// ===== 标题 =====
		_titleLabel = new Label
		{
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			Font = new Font("Microsoft YaHei UI", 16f, FontStyle.Bold),
			ForeColor = Color.FromArgb(34, 34, 34),
			Text = title ?? "提示",
			BackColor = Color.Transparent,
			Location = new Point(28, 24),
			Size = new Size(_contentPanel.Width - 56, 34),
		};

		// ===== 内容 =====
		_msgLabel = new Label
		{
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft,
			Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular),
			ForeColor = Color.FromArgb(80, 80, 80),
			Text = message ?? "",
			BackColor = Color.Transparent,
			Location = new Point(28, 72),
			Size = new Size(_contentPanel.Width - 56, 64),
		};

		// ===== 右上角关闭按钮（可选）=====
		var closeBtn = new Button
		{
			Text = "×",
			Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold),
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.Transparent,
			ForeColor = Color.FromArgb(120, 120, 120),
			Size = new Size(40, 34),
			Location = new Point(_contentPanel.Width - 52, 16),
			TabStop = false
		};
		closeBtn.FlatAppearance.BorderSize = 0;
		closeBtn.Click += (s, e) => Close();

		// ===== 成功图标（简单画个绿圈对勾）=====
	


		_contentPanel.Size = new Size(520, 220);

		// 组装
		_contentPanel.Controls.Add(_titleLabel);
		_contentPanel.Controls.Add(_msgLabel);
		_contentPanel.Controls.Add(closeBtn);
	

		Controls.Add(_contentPanel);
		_contentPanel.Dock = DockStyle.Fill;
		_contentPanel.Paint += (s, e) =>
		{
			// 画圆角卡片边框
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			var rect = new Rectangle(0, 0, _contentPanel.Width - 1, _contentPanel.Height - 1);
			using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
			using (var brush = new SolidBrush(Color.White))
			{
				using (var path = RoundedRect(rect, 18))
				{
					e.Graphics.FillPath(brush, path);
					e.Graphics.DrawPath(pen, path);
				}
			}
		};
		var iconBox = new PictureBox
		{
			Size = new Size(52, 52),
			Location = new Point(
			(_contentPanel.Width - 52) / 2,
			118
		),
			BackColor = Color.Transparent
		};
		iconBox.Location = new Point(((_contentPanel.Width - 52) / 2), 200);

		//	_contentPanel.Size = new Size(520, 200);

		//iconBox.Paint += (s, e) =>
		//{
		//	e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		//	using (var pen = new Pen(Color.FromArgb(0, 168, 84), 4))
		//	{
		//		e.Graphics.DrawEllipse(pen, 6, 6, 40, 40);
		//		// 对勾
		//		e.Graphics.DrawLines(pen, new[]
		//		{
		//			new Point(16, 28),
		//			new Point(24, 36),
		//			new Point(38, 20)
		//		});
		//	}
		//};
		iconBox.Paint += (s, e) =>
		{
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			if (isSuccess)
			{
				using (var pen = new Pen(Color.FromArgb(0, 168, 84), 4))
				{
					e.Graphics.DrawEllipse(pen, 6, 6, 40, 40);

					// ✔
					e.Graphics.DrawLines(pen, new[]
					{
				new Point(16, 28),
				new Point(24, 36),
				new Point(38, 20)
			});
				}
			}
			else
			{
				using (var pen = new Pen(Color.FromArgb(220, 53, 69), 4))
				{
					e.Graphics.DrawEllipse(pen, 6, 6, 40, 40);

					// ✖
					e.Graphics.DrawLine(pen, 16, 16, 36, 36);
					e.Graphics.DrawLine(pen, 36, 16, 16, 36);
				}
			}
		};
		_titleLabel.ForeColor = isSuccess
	? Color.FromArgb(34, 34, 34)
	: Color.FromArgb(220, 53, 69);

		_contentPanel.Controls.Add(iconBox);
		iconBox.BringToFront();
		// 点击任意地方关闭
		Click += (s, e) => Close();
		_contentPanel.Click += (s, e) => Close();
		foreach (Control c in _contentPanel.Controls)
			c.Click += (s, e) => Close();

		// 定时关闭
		_timer = new Timer();
		_timer.Interval = Math.Max(200, durationMs);
		_timer.Tick += (s, e) =>
		{
			_timer.Stop();
			Close();
		};

		Shown += (s, e) =>
		{
			_timer.Start();
		};

		// 屏幕变化时（多显示器/分辨率变化）重新布局
		SystemEvents_DisplaySettingsChangedHook();
	}
	public static void ShowFail(string message, int durationMs = 2200, string title = "产品测试失败")
	{
		if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
		{
			Application.OpenForms[0].BeginInvoke(new Action(() => ShowFail(message, durationMs, title)));
			return;
		}

		var toast = new FrmLitleScreenToast(title, message, durationMs, false);
		toast.Show();
	}

	// ===== 外部静态调用 =====
	public static void ShowSuccess(string message, int durationMs = 1800, string title = "产品测试成功")
	{
		// 确保在 UI 线程显示
		if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
		{
			Application.OpenForms[0].BeginInvoke(new Action(() => ShowSuccess(message, durationMs, title)));
			return;
		}

		var toast = new FrmLitleScreenToast(title, message, durationMs,true);
		toast.Show();
	}


	// ===== 不抢焦点（可选）=====
	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			const int WS_EX_NOACTIVATE = 0x08000000;
			const int WS_EX_TOOLWINDOW = 0x00000080;
			var cp = base.CreateParams;
			cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
			return cp;
		}
	}

	// ===== 圆角路径 =====
	private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
	{
		int d = radius * 2;
		var path = new System.Drawing.Drawing2D.GraphicsPath();
		path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
		path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
		path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
		path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
		path.CloseFigure();
		return path;
	}

	// ===== 监听显示设置变化（避免分辨率改变位置不对）=====
	private void SystemEvents_DisplaySettingsChangedHook()
	{
		try
		{
			//Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (s, e) =>
			//{
			//	if (!IsDisposed && Visible)
			//		LayoutToHalfScreenBottom();
			//};
		}
		catch
		{
			// 某些受限环境可能抛异常，忽略即可
		}
	}

	private void InitializeComponent()
	{
			this.SuspendLayout();
			// 
			// FrmHalfScreenToast
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.ClientSize = new System.Drawing.Size(582, 282);
			this.Name = "FrmHalfScreenToast";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.TransparencyKey = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.ResumeLayout(false);

	}
}
