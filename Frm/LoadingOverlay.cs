using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace YourNamespace
{
	public static class LoadingOverlay
	{
		private static readonly Dictionary<Control, OverlayPanel> _overlays = new Dictionary<Control, OverlayPanel>();
		private static readonly object _lock = new object();

		/// <summary>
		/// 显示加载框
		/// </summary>
		public static void Show(Control parent, string text = "加载中...")
		{
			if (parent == null || parent.IsDisposed)
				return;

			if (parent.InvokeRequired)
			{
				try
				{
					parent.BeginInvoke(new Action(() => Show(parent, text)));
				}
				catch
				{
				}
				return;
			}

			lock (_lock)
			{
				if (parent.IsDisposed)
					return;

				OverlayPanel overlay;
				if (_overlays.TryGetValue(parent, out overlay))
				{
					if (overlay != null && !overlay.IsDisposed)
					{
						overlay.SetText(text);
						overlay.BringToFront();
						overlay.UpdateLayout();
						overlay.ShowAnimated();
						return;
					}

					_overlays.Remove(parent);
				}

				overlay = new OverlayPanel(parent, text);
				overlay.Dock = DockStyle.Fill;

				parent.Controls.Add(overlay);
				overlay.BringToFront();
				overlay.UpdateLayout();
				overlay.ShowAnimated();

				_overlays[parent] = overlay;

				parent.Disposed -= Parent_Disposed;
				parent.Disposed += Parent_Disposed;

				parent.SizeChanged -= Parent_SizeChanged;
				parent.SizeChanged += Parent_SizeChanged;
			}
		}

		/// <summary>
		/// 隐藏加载框
		/// </summary>
		public static void Hide(Control parent)
		{
			if (parent == null || parent.IsDisposed)
				return;

			if (parent.InvokeRequired)
			{
				try
				{
					parent.BeginInvoke(new Action(() => Hide(parent)));
				}
				catch
				{
				}
				return;
			}

			lock (_lock)
			{
				OverlayPanel overlay;
				if (!_overlays.TryGetValue(parent, out overlay))
					return;

				_overlays.Remove(parent);

				parent.Disposed -= Parent_Disposed;
				parent.SizeChanged -= Parent_SizeChanged;

				if (overlay != null && !overlay.IsDisposed)
				{
					overlay.HideAnimated(() =>
					{
						try
						{
							if (!overlay.IsDisposed)
							{
								if (overlay.Parent != null)
									overlay.Parent.Controls.Remove(overlay);

								overlay.Dispose();
							}
						}
						catch
						{
						}
					});
				}
			}
		}

		public static bool IsShowing(Control parent)
		{
			if (parent == null || parent.IsDisposed)
				return false;

			lock (_lock)
			{
				OverlayPanel overlay;
				return _overlays.TryGetValue(parent, out overlay)
					   && overlay != null
					   && !overlay.IsDisposed
					   && overlay.Visible;
			}
		}

		private static void Parent_Disposed(object sender, EventArgs e)
		{
			var parent = sender as Control;
			if (parent == null) return;

			lock (_lock)
			{
				OverlayPanel overlay;
				if (_overlays.TryGetValue(parent, out overlay))
				{
					_overlays.Remove(parent);
					if (overlay != null && !overlay.IsDisposed)
						overlay.Dispose();
				}
			}
		}

		private static void Parent_SizeChanged(object sender, EventArgs e)
		{
			var parent = sender as Control;
			if (parent == null) return;

			lock (_lock)
			{
				OverlayPanel overlay;
				if (_overlays.TryGetValue(parent, out overlay))
				{
					if (overlay != null && !overlay.IsDisposed)
						overlay.UpdateLayout();
				}
			}
		}

		private sealed class OverlayPanel : Control
		{
			private readonly Control _parent;

			private readonly Timer _spinnerTimer;
			private readonly Timer _fadeTimer;

			private int _angle;
			private string _text;

			private readonly Size _boxSize = new Size(180, 130);

			private int _currentAlpha = 0;
			private int _targetAlpha = 0;
			private int _maxOverlayAlpha = 35;
			private int _maxBoxShadowAlpha = 35;
			private int _maxBoxAlpha = 255;
			private int _maxTextAlpha = 255;
			private int _maxSpinnerBaseAlpha = 255;

			private Action _hideCompleted;

			public OverlayPanel(Control parent, string text)
			{
				_parent = parent;
				_text = string.IsNullOrWhiteSpace(text) ? "加载中..." : text;
				DoubleBuffered = true;
				SetStyle(ControlStyles.AllPaintingInWmPaint
					| ControlStyles.OptimizedDoubleBuffer
						 | ControlStyles.UserPaint
						 | ControlStyles.OptimizedDoubleBuffer
						 | ControlStyles.ResizeRedraw
						 | ControlStyles.SupportsTransparentBackColor, true);

				//BackColor = Color.Transparent;
				Visible = true;
				TabStop = false;

				_spinnerTimer = new Timer();
				_spinnerTimer.Interval = 60;
				_spinnerTimer.Tick += (s, e) =>
				{
					_angle += 30;
					if (_angle >= 360) _angle = 0;
					Invalidate(GetCenteredRect(_boxSize));
					//Invalidate();
				};
				_spinnerTimer.Start();

				_fadeTimer = new Timer();
				_fadeTimer.Interval = 20;
				_fadeTimer.Tick += FadeTimer_Tick;
			}

			public void SetText(string text)
			{
				_text = string.IsNullOrWhiteSpace(text) ? "加载中..." : text;
				Invalidate();
			}

			public void UpdateLayout()
			{
				if (_parent == null || _parent.IsDisposed) return;
				Bounds = new Rectangle(0, 0, _parent.ClientSize.Width, _parent.ClientSize.Height);
				Invalidate();
			}

			public void ShowAnimated()
			{
				_hideCompleted = null;
				Visible = true;
				BringToFront();
				_targetAlpha = 255;

				if (!_fadeTimer.Enabled)
					_fadeTimer.Start();

				Invalidate();
			}

			public void HideAnimated(Action onCompleted)
			{
				_hideCompleted = onCompleted;
				_targetAlpha = 0;

				if (!_fadeTimer.Enabled)
					_fadeTimer.Start();
			}

			private void FadeTimer_Tick(object sender, EventArgs e)
			{
				const int step = 25;

				if (_currentAlpha < _targetAlpha)
				{
					_currentAlpha += step;
					if (_currentAlpha > _targetAlpha)
						_currentAlpha = _targetAlpha;

					Invalidate();
				}
				else if (_currentAlpha > _targetAlpha)
				{
					_currentAlpha -= step;
					if (_currentAlpha < _targetAlpha)
						_currentAlpha = _targetAlpha;

					Invalidate();
				}

				if (_currentAlpha == _targetAlpha)
				{
					_fadeTimer.Stop();

					if (_targetAlpha == 0)
					{
						Visible = false;
						var callback = _hideCompleted;
						_hideCompleted = null;
						if (callback != null)
							callback();
					}
				}
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					if (_spinnerTimer != null)
					{
						_spinnerTimer.Stop();
						_spinnerTimer.Dispose();
					}

					if (_fadeTimer != null)
					{
						_fadeTimer.Stop();
						_fadeTimer.Dispose();
					}
				}
				base.Dispose(disposing);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				if (_currentAlpha <= 0)
					return;

				Graphics g = e.Graphics;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

				DrawBackground(g);

				Rectangle boxRect = GetCenteredRect(_boxSize);

				DrawShadow(g, boxRect);
				DrawBox(g, boxRect);
				DrawSpinner(g, boxRect);
				DrawText(g, boxRect);
			}

			private void DrawBackground(Graphics g)
			{
				int alpha = ScaleAlpha(_maxOverlayAlpha);
				using (var brush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
				{
					g.FillRectangle(brush, ClientRectangle);
				}
			}

			private void DrawShadow(Graphics g, Rectangle boxRect)
			{
				Rectangle shadowRect = new Rectangle(boxRect.X + 3, boxRect.Y + 4, boxRect.Width, boxRect.Height);

				using (GraphicsPath path = CreateRoundedRectangle(shadowRect, 18))
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(_maxBoxShadowAlpha), 0, 0, 0)))
				{
					g.FillPath(brush, path);
				}
			}

			private void DrawBox(Graphics g, Rectangle boxRect)
			{
				using (GraphicsPath path = CreateRoundedRectangle(boxRect, 18))
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(_maxBoxAlpha), 255, 255, 255)))
				using (Pen pen = new Pen(Color.FromArgb(ScaleAlpha(230), 230, 230, 230), 1f))
				{
					g.FillPath(brush, path);
					g.DrawPath(pen, path);
				}
			}

			private void DrawSpinner(Graphics g, Rectangle boxRect)
			{
				int centerX = boxRect.X + boxRect.Width / 2;
				int centerY = boxRect.Y + 42;
				int outerRadius = 18;
				int innerOffset = 10;

				GraphicsState state = g.Save();

				g.TranslateTransform(centerX, centerY);
				g.RotateTransform(_angle);

				for (int i = 0; i < 12; i++)
				{
					int alpha = ScaleAlpha(30 + i * 18);
					if (alpha > 255) alpha = 255;

					using (Pen pen = new Pen(Color.FromArgb(alpha, 90, 90, 90), 3f))
					{
						pen.StartCap = LineCap.Round;
						pen.EndCap = LineCap.Round;
						g.DrawLine(pen, 0, -outerRadius, 0, -(outerRadius - innerOffset));
					}

					g.RotateTransform(30f);
				}

				g.Restore(state);
			}

			private void DrawText(Graphics g, Rectangle boxRect)
			{
				Rectangle textRect = new Rectangle(boxRect.X + 15, boxRect.Y + 72, boxRect.Width - 30, 36);

				using (Font font = new Font("Microsoft YaHei", 10f, FontStyle.Regular))
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(_maxTextAlpha), 80, 80, 80)))
				using (StringFormat sf = new StringFormat())
				{
					sf.Alignment = StringAlignment.Center;
					sf.LineAlignment = StringAlignment.Center;
					sf.Trimming = StringTrimming.EllipsisCharacter;
					sf.FormatFlags = StringFormatFlags.NoWrap;

					g.DrawString(_text, font, brush, textRect, sf);
				}
			}

			private int ScaleAlpha(int maxAlpha)
			{
				return (int)(maxAlpha * (_currentAlpha / 255.0));
			}

			private Rectangle GetCenteredRect(Size size)
			{
				int x = (ClientSize.Width - size.Width) / 2;
				int y = (ClientSize.Height - size.Height) / 2;
				return new Rectangle(x, y, size.Width, size.Height);
			}

			private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
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

			protected override void OnMouseDown(MouseEventArgs e) { }
			protected override void OnMouseMove(MouseEventArgs e) { }
			protected override void OnMouseUp(MouseEventArgs e) { }
			protected override void OnClick(EventArgs e) { }
			protected override void OnDoubleClick(EventArgs e) { }
			protected override void OnMouseWheel(MouseEventArgs e) { }

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					//Scp.ExStyle |= 0x00000020;
					return cp;
				}
			}
		}
	}
}
