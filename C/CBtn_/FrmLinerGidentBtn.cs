using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FrmControl.C.Base;

namespace FrmControl.C.Btn
{
	namespace UPPERIOC2.UPPER.Util
	{
		public class AnimationUtil
		{
			private bool _isRunning = false;
			private static AnimationUtil _instance;
			private static readonly object _lock = new object();

			public static AnimationUtil Instance
			{
				get
				{
					if (_instance == null)
					{
						lock (_lock)
						{
							if (_instance == null)
							{
								_instance = new AnimationUtil();
							}
						}
					}
					return _instance;
				}
			}

			public class AnimationItem
			{
				public int TimeLeft { get; set; }
				public Action<int> Callback { get; set; }

				public AnimationItem(int timeLeft, Action<int> callback)
				{
					TimeLeft = timeLeft;
					Callback = callback;
				}
			}

			public ConcurrentQueue<AnimationItem> Queue = new ConcurrentQueue<AnimationItem>();

			public AnimationUtil()
			{
				_isRunning = true;

				Task.Factory.StartNew(() =>
				{
					List<AnimationItem> runningList = new List<AnimationItem>();
					List<AnimationItem> removeList = new List<AnimationItem>();

					while (_isRunning)
					{
						while (Queue.TryDequeue(out var item))
						{
							runningList.Add(item);
						}

						foreach (var item in runningList)
						{
							item.TimeLeft -= 20;
							if (item.TimeLeft <= 0)
							{
								item.TimeLeft = 0;
								removeList.Add(item);
							}

							try
							{
								item.Callback?.Invoke(item.TimeLeft);
							}
							catch
							{
								// 避免某个控件销毁或异常把整个动画线程搞死
								removeList.Add(item);
							}
						}

						if (removeList.Count > 0)
						{
							runningList.RemoveAll(x => removeList.Contains(x));
							removeList.Clear();
						}

						Thread.Sleep(16); // 约 60 FPS
					}
				}, TaskCreationOptions.LongRunning);
			}

			~AnimationUtil()
			{
				_isRunning = false;
			}

			public void SetAnimation(int allTime, Action<int> callback)
			{
				if (allTime <= 0 || callback == null) return;
				Queue.Enqueue(new AnimationItem(allTime, callback));
			}
		}
	}

	[DefaultEvent("Click")]
	public partial class FrmLinerGidentBtn : CBaseControl
	{
		public int SizeCLick { get; set; } = 70;
		public bool UseAnimation { get; set; } = true;

		[Description("渐变方向向量，建议范围 0~1")]
		public PointF Direction { get; set; } = new PointF(0, 1);

		public Color Color1 { get; set; } = Color.LightGray;
		public Color Color2 { get; set; } = Color.DimGray;
		public Color Color3 { get; set; } = Color.LightGray;

		public string BtnText { get; set; } = string.Empty;

		private PointF MousePosition = new PointF(-1, -1);
		private PointF Mousein = new PointF(0, 0);      // X=动画进度，Y=是否悬停
		private PointF Mousedown = new PointF(0, 0);    // X=动画进度，Y=是否按下

		public FrmLinerGidentBtn()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			DoubleBuffered = true;
			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.UserPaint, true);

			InitializeComponent();
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			Mousein.Y = 1;
			MousePosition = PointToClient(Control.MousePosition);

			if (UseAnimation)
			{
				UPPERIOC2.UPPER.Util.AnimationUtil.Instance.SetAnimation(100, x =>
				{
					if (IsDisposed || !IsHandleCreated) return;

					float progress = (100 - x) / 100f;
					if (progress < 0f) progress = 0f;
					if (progress > 1f) progress = 1f;

					DoInvoke(() =>
					{
						Mousein.X = progress;
						Invalidate();
					});
				});
			}
			else
			{
				Mousein.X = 1f;
				Invalidate();
			}

			base.OnMouseEnter(e);
		}

		public void PreFormClick()
		{
			OnClick(EventArgs.Empty);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			Mousedown.Y = 1;
			MousePosition = e.Location;

			if (UseAnimation)
			{
				UPPERIOC2.UPPER.Util.AnimationUtil.Instance.SetAnimation(200, x =>
				{
					if (IsDisposed || !IsHandleCreated) return;

					float progress = (200 - x) / 200f;
					if (progress < 0f) progress = 0f;
					if (progress > 1f) progress = 1f;

					DoInvoke(() =>
					{
						Mousedown.X = progress;

						if (x <= 0)
						{
							Mousedown.X = 0f;
							Mousedown.Y = 0f;
						}

						Invalidate();
					});
				});
			}
			else
			{
				Mousedown.X = 1f;
				Invalidate();
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			Mousein.Y = 0;
			Mousein.X = 0;
			MousePosition = new PointF(-1, -1);
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			Rectangle rect = this.ClientRectangle;
			if (rect.Width <= 0 || rect.Height <= 0) return;

			PointF endPoint = new PointF(Direction.X * Width, Direction.Y * Height);

			// 防止起点终点相同导致画刷异常
			if (Math.Abs(endPoint.X) < 0.001f && Math.Abs(endPoint.Y) < 0.001f)
			{
				endPoint = new PointF(0, Height);
			}

			using (var gb = new LinearGradientBrush(
				new PointF(0, 0),
				endPoint,
				Color1,
				Color2))
			{
				if (Mousein.Y == 0)
				{
					gb.InterpolationColors = new ColorBlend
					{
						Colors = new[] { Color1, Color2, Color3 },
						Positions = new[] { 0f, 0.5f, 1f }
					};
				}
				else
				{
					float offset = Math.Min(0.24f, Math.Max(0f, Mousein.X * 0.24f));
					float p1 = 0.5f - offset;
					float p2 = 0.5f + offset;

					gb.InterpolationColors = new ColorBlend
					{
						Colors = new[] { Color1, Color2, Color2, Color3 },
						Positions = new[] { 0f, p1, p2, 1f }
					};
				}

				g.FillRectangle(gb, rect);
			}

			// 点击扩散效果
			if (Mousein.Y != 0 && Mousedown.Y != 0 && MousePosition.X >= 0 && MousePosition.Y >= 0)
			{
				float radius = SizeCLick * Mousedown.X + 1;
				if (radius > 0.1f)
				{
					using (GraphicsPath path = new GraphicsPath())
					{
						path.AddEllipse(
							MousePosition.X - radius / 2f,
							MousePosition.Y - radius / 2f,
							radius,
							radius);

						using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
						{
							int alpha = (int)(255 * (1 - Mousedown.X));
							alpha = Math.Max(0, Math.Min(255, alpha));

							pthGrBrush.CenterColor = Color.FromArgb(alpha, Color1);
							pthGrBrush.SurroundColors = new[] { Color.FromArgb(alpha, Color2) };

							g.FillPath(pthGrBrush, path);
						}
					}
				}
			}

			// 文本绘制
			string text = BtnText ?? string.Empty;
			SizeF textSize = g.MeasureString(text, Font);

			PointF textLocation = new PointF(
				(ClientRectangle.Width - textSize.Width) / 2f,
				(ClientRectangle.Height - textSize.Height) / 2f);

			using (var b = new SolidBrush(ForeColor))
			{
				g.DrawString(text, Font, b, textLocation);
			}
		}
	}
}
