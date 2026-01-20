using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FrmControl.C.Base;
namespace UpperComAutoTest.MyControls
{
	public partial class GradientProgressBar : CBaseControl
	{
		private int minimum;
		private int maximum;
		private int value;
		private Color startColor = Color.FromArgb(0, 120, 215);
		private Color endColor = Color.FromArgb(24, 144, 255);
		private System.Windows.Forms.Timer animationTimer;
		private float animationValue;
        private bool useAnimation = false;
		private Color valueColor;
		private float borderStonger = 1f;

		public bool UseAnimation { get => useAnimation; set => useAnimation = value; }
        public Color StartColor { get=> startColor; set=> startColor = value; }
        public Color? ValueColor { get=> valueColor; set {
				if (value == null)
				{
					return;
				}
				if (value == Color.Empty)
				{
					return;

				}
				var co = value.Value;
				valueColor = co;
				startColor = co;
				endColor = co;
				Invalidate();
			}}
        public Color EndColor { get=> endColor; set=> endColor = value; }
        public GradientProgressBar()
		{

			this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			this.Minimum = 0;
			this.Maximum = 100;
			this.Value = 0;
			this.Width = 50;
			this.Height = 50;

			// 初始化动画效果
			animationTimer = new System.Windows.Forms.Timer();
			animationTimer.Interval = 30; // 控制动画速度
			animationTimer.Tick += new EventHandler(OnAnimationTick);
			animationTimer.Start();
		}

		public int Minimum
		{
			get { return minimum; }
			set
			{
				minimum = value;
				if (minimum > maximum) maximum = minimum;
				if (value < minimum) Value = minimum;
				Invalidate();
			}
		}

		public int Maximum
		{
			get { return maximum; }
			set
			{
				maximum = value;
				if (maximum < minimum) minimum = maximum;
				if (value > maximum) Value = maximum;
				Invalidate();
			}
		}

		public int Value
		{
			get { return value; }
			set
			{
				if (value < minimum) value = minimum;
				if (value > maximum) value = maximum;
				this.value = value;
				Invalidate();
			}
		}

		public float BorderStonger { get => borderStonger; set => borderStonger = value; }

		private void OnAnimationTick(object sender, EventArgs e)
		{
			if (UseAnimation)
			{
				// 模拟动画效果
				if (animationValue < value)
				{
					animationValue += 1f;
					Invalidate();
				}
				else if (animationValue > value)
				{
					animationValue -= 1f;
					Invalidate();
				}
			}
			else
			{
				if (animationValue != value)
				{
					animationValue = value;
					Invalidate();
				}
            }
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// 渐变颜色计算
			Color currentColor = InterpolateColor(startColor, endColor, animationValue / (maximum - minimum));

			// 绘制背景
			//e.Graphics.Clear(Color.WhiteSmoke);
            using var so = new SolidBrush(BackColor);
			e.Graphics.FillRectangle(so, ClientRectangle);

            // 计算进度条的尺寸和位置
            int width = (int)((float)(animationValue - minimum) / (maximum - minimum) * ClientRectangle.Width);
			// 绘制渐变进度条
			if (width > 0)
			{
				Rectangle progressRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y, width, ClientRectangle.Height);
				using (LinearGradientBrush brush = new LinearGradientBrush(progressRect, startColor, currentColor, LinearGradientMode.Horizontal))
				{
					e.Graphics.FillRectangle(brush, progressRect);
				}
			}
			if (width >= 0)
			{

				e.Graphics.DrawString((value * 100 )/(maximum - minimum) + "%", Font, new SolidBrush(Color.Black), this.ClientRectangle, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

			}

			// 绘制边框
			using (Pen pen = new Pen(Color.Black, BorderStonger)) // 边框颜色和宽度
			{
				e.Graphics.DrawRectangle(pen, ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width - BorderStonger, ClientRectangle.Height - BorderStonger);
			}
		}

		private Color InterpolateColor(Color start, Color end, float progress)
		{
			int r = (int)(start.R + (end.R - start.R) * progress);
			int g = (int)(start.G + (end.G - start.G) * progress);
			int b = (int)(start.B + (end.B - start.B) * progress);
			return Color.FromArgb(r, g, b);
		}
	}
}