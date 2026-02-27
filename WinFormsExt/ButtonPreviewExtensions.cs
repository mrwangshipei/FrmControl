using System;
using System.Drawing;
using System.Windows.Forms;

namespace FrmControl.WinFormsExt
{

	public static class ButtonPreviewExtensions
	{

		/// <summary>
		/// 鼠标移入按钮时，在按钮上方显示临时 Bitmap 预览；移开时销毁释放。
		/// </summary>
		/// <param name="button">目标按钮</param>
		/// <param name="bitmapFactory">移入时生成 Bitmap 的函数（返回的 Bitmap 会在隐藏时 Dispose）</param>
		/// <param name="maxWidth">预览最大宽度（像素）</param>
		/// <param name="maxHeight">预览最大高度（像素）</param>
		/// <param name="offsetY">预览距离按钮上方的间距（像素，正数表示更往上）</param>
		public static void EnableBitmapHoverPreview(
			this Button button,
			Func<Bitmap> bitmapFactory,
			int maxWidth = 400,
			int maxHeight = 300,
			int offsetY = 8)
		{

			if (button == null) throw new ArgumentNullException(nameof(button));
			if (bitmapFactory == null) throw new ArgumentNullException(nameof(bitmapFactory));

			HoverPreviewForm previewForm = null;
			Bitmap currentBmp = null;

			void ShowPreview()
			{
				if (previewForm != null && !previewForm.IsDisposed) return;

				// 生成 Bitmap
				currentBmp = bitmapFactory.Invoke();
				if (currentBmp == null) return;

				// 限制尺寸并按比例缩放（不修改原图，生成显示用副本）
				var displayBmp = ScaleToFit(currentBmp, maxWidth, maxHeight);

				previewForm = new HoverPreviewForm(displayBmp);
				//previewForm.PShowWithoutActivation = true;

				// 定位到按钮上方
				PositionPreviewAboveButton(button, previewForm, offsetY);

				// 显示
				previewForm.Show();

				// 当鼠标离开预览区域，也关掉（避免用户从按钮移到预览时关不掉/或相反）
				previewForm.MouseLeave += (_, __) => HidePreview();
				previewForm.Deactivate += (_, __) => HidePreview();
			}

			void HidePreview()
			{
				if (previewForm != null)
				{
					try
					{
						previewForm.Close();
						previewForm.Dispose();
					}
					catch { /* ignore */ }
					previewForm = null;
				}

				// Dispose 当前生成的 Bitmap（工厂返回的那张）
				if (currentBmp != null)
				{
					try { currentBmp.Dispose(); } catch { /* ignore */ }
					currentBmp = null;
				}
			}

			// 鼠标移入显示
			button.MouseEnter += (_, __) => ShowPreview();

			// 鼠标移出隐藏：如果鼠标正在进入预览窗体，会先触发按钮 MouseLeave，
			// 所以这里做一个小延迟判断，避免闪烁
			button.MouseLeave += (_, __) => {
				button.BeginInvoke((Action)(() => {
					if (previewForm == null || previewForm.IsDisposed)
					{
						HidePreview();
						return;
					}

					// 如果鼠标在预览窗体上，就不关（让用户能看清）
					var p = Control.MousePosition;
					if (!previewForm.Bounds.Contains(p))
					{
						HidePreview();
					}
				}));
			};

			// 按钮销毁时，清理资源
			button.Disposed += (_, __) => HidePreview();
		}

		private static void PositionPreviewAboveButton(Button button, Form previewForm, int offsetY)
		{
			var buttonScreen = button.PointToScreen(Point.Empty);

			int x = buttonScreen.X + (button.Width - previewForm.Width) / 2;
			int y = buttonScreen.Y - previewForm.Height - offsetY;

			// 防止出屏幕
			var screen = Screen.FromControl(button).WorkingArea;

			if (x < screen.Left) x = screen.Left;
			if (x + previewForm.Width > screen.Right) x = screen.Right - previewForm.Width;

			if (y < screen.Top)
			{
				// 如果上方放不下，就放到按钮下方
				y = buttonScreen.Y + button.Height + offsetY;
				if (y + previewForm.Height > screen.Bottom)
				{
					y = screen.Bottom - previewForm.Height;
				}
			}

			previewForm.StartPosition = FormStartPosition.Manual;
			previewForm.Location = new Point(x, y);
		}

		private static Bitmap ScaleToFit(Bitmap src, int maxW, int maxH)
		{
			if (src == null) return null;
			if (maxW <= 0) maxW = src.Width;
			if (maxH <= 0) maxH = src.Height;

			float sx = maxW / (float)src.Width;
			float sy = maxH / (float)src.Height;
			float s = Math.Min(1.0f, Math.Min(sx, sy)); // 只缩小不放大（避免糊）

			int w = Math.Max(1, (int)Math.Round(src.Width * s));
			int h = Math.Max(1, (int)Math.Round(src.Height * s));

			if (w == src.Width && h == src.Height)
			{
				// 返回副本，避免外部 Dispose 影响显示
				return new Bitmap(src);
			}

			var dst = new Bitmap(w, h);
			using (var g = Graphics.FromImage(dst))
			{
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				g.DrawImage(src, new Rectangle(0, 0, w, h));
			}
			return dst;
		}

		/// <summary>
		/// 临时预览窗体：无边框、置顶、不抢焦点
		/// </summary>
		private sealed class HoverPreviewForm : Form
		{
			private readonly PictureBox _pb;


			public HoverPreviewForm(Bitmap bmp)
			{
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				TopMost = true;
				DoubleBuffered = true;
				BackColor = Color.White;

				_pb = new PictureBox
				{
					Dock = DockStyle.Fill,
					SizeMode = PictureBoxSizeMode.Normal,
					Image = bmp
				};

				Controls.Add(_pb);

				// 留一点边框阴影感（可选）
				Padding = new Padding(2);
			//	AutoSize = true;
				//AutoSizeMode = AutoSizeMode.GrowAndShrink;

				// 根据图片大小设置窗体
				ClientSize = new Size(bmp.Width + Padding.Horizontal, bmp.Height + Padding.Vertical);
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get
				{
					const int WS_EX_TOOLWINDOW = 0x00000080;
					const int WS_EX_NOACTIVATE = 0x08000000;

					var cp = base.CreateParams;
					cp.ExStyle |= WS_EX_TOOLWINDOW;
					cp.ExStyle |= WS_EX_NOACTIVATE;
					return cp;
				}
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					if (_pb?.Image != null)
					{
						try { _pb.Image.Dispose(); } catch { /* ignore */ }
						_pb.Image = null;
					}
					_pb?.Dispose();
				}
				base.Dispose(disposing);
			}
		}
	}
}
