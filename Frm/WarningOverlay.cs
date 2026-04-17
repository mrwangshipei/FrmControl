using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace YourNamespace
{
	public static class WarningOverlay
	{
		private static readonly Dictionary<Control, OverlayPanel> _overlays = new Dictionary<Control, OverlayPanel>();
		private static readonly object _lock = new object();

		/// <summary>
		/// 显示警告框
		/// </summary>
		public static void Show(Control parent, string text = "警告")
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
		/// 隐藏警告框
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
			private readonly Timer _fadeTimer;

			private string _text;
			private Size _boxSize = new Size(320, 90);

			private int _currentAlpha = 0;
			private int _targetAlpha = 0;

			// 这里控制是否显示整个遮罩背景
			private int _maxOverlayAlpha = 0;     // 0 = 不显示黑色遮罩
			private int _maxBoxShadowAlpha = 30;
			private int _maxBoxAlpha = 255;
			private int _maxTextAlpha = 255;

			private Action _hideCompleted;

			public OverlayPanel(Control parent, string text)
			{
				_parent = parent;
				_text = string.IsNullOrWhiteSpace(text) ? "警告" : text;

				DoubleBuffered = true;
				SetStyle(ControlStyles.AllPaintingInWmPaint
						 | ControlStyles.OptimizedDoubleBuffer
						 | ControlStyles.UserPaint
						 | ControlStyles.ResizeRedraw
						 | ControlStyles.SupportsTransparentBackColor, true);

				Visible = true;
				TabStop = false;

				_fadeTimer = new Timer();
				_fadeTimer.Interval = 20;
				_fadeTimer.Tick += FadeTimer_Tick;
			}

			public void SetText(string text)
			{
				_text = string.IsNullOrWhiteSpace(text) ? "警告" : text;
				RecalcBoxSize();
				Invalidate();
			}

			public void UpdateLayout()
			{
				if (_parent == null || _parent.IsDisposed) return;
				Bounds = new Rectangle(0, 0, _parent.ClientSize.Width, _parent.ClientSize.Height);
				RecalcBoxSize();
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
				DrawWarningIcon(g, boxRect);
				DrawText(g, boxRect);
			}

			private void DrawBackground(Graphics g)
			{
				int alpha = ScaleAlpha(_maxOverlayAlpha);
				if (alpha <= 0) return;

				using (var brush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
				{
					g.FillRectangle(brush, ClientRectangle);
				}
			}

			private void DrawShadow(Graphics g, Rectangle boxRect)
			{
				Rectangle shadowRect = new Rectangle(boxRect.X + 3, boxRect.Y + 4, boxRect.Width, boxRect.Height);

				using (GraphicsPath path = CreateRoundedRectangle(shadowRect, 16))
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(_maxBoxShadowAlpha), 0, 0, 0)))
				{
					g.FillPath(brush, path);
				}
			}

			private void DrawBox(Graphics g, Rectangle boxRect)
			{
				using (GraphicsPath path = CreateRoundedRectangle(boxRect, 16))
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(_maxBoxAlpha), 255, 255, 255)))
				using (Pen pen = new Pen(Color.FromArgb(ScaleAlpha(225), 225, 225, 225), 1f))
				{
					g.FillPath(brush, path);
					g.DrawPath(pen, path);
				}
			}

			private void DrawWarningIcon(Graphics g, Rectangle boxRect)
			{
				Rectangle iconRect = new Rectangle(boxRect.X + 18, boxRect.Y + (boxRect.Height - 24) / 2, 24, 24);

				using (SolidBrush circleBrush = new SolidBrush(Color.FromArgb(ScaleAlpha(255), 255, 244, 228)))
				using (Pen circlePen = new Pen(Color.FromArgb(ScaleAlpha(255), 255, 170, 60), 1.2f))
				{
					g.FillEllipse(circleBrush, iconRect);
					g.DrawEllipse(circlePen, iconRect);
				}

				using (Pen pen = new Pen(Color.FromArgb(ScaleAlpha(255), 235, 145, 20), 2.2f))
				using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(ScaleAlpha(255), 235, 145, 20)))
				{
					int cx = iconRect.Left + iconRect.Width / 2;
					g.DrawLine(pen, cx, iconRect.Top + 5, cx, iconRect.Top + 14);
					g.FillEllipse(dotBrush, cx - 2, iconRect.Top + 17, 4, 4);
				}
			}

			private void DrawText(Graphics g, Rectangle boxRect)
			{
				Rectangle textRect = new Rectangle(boxRect.X + 52, boxRect.Y + 14, boxRect.Width - 68, boxRect.Height - 28);

				using (Font font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular))
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(_maxTextAlpha), 70, 70, 70)))
				using (StringFormat sf = new StringFormat())
				{
					sf.Alignment = StringAlignment.Near;
					sf.LineAlignment = StringAlignment.Center;
					sf.Trimming = StringTrimming.EllipsisCharacter;
					sf.FormatFlags = 0;

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

			private void RecalcBoxSize()
			{
				if (string.IsNullOrWhiteSpace(_text))
				{
					_boxSize = new Size(220, 90);
					return;
				}

				int maxWidth = Math.Min(420, Math.Max(220, ClientSize.Width - 40));
				if (maxWidth < 220)
					maxWidth = 220;

				Size measured;
				using (Graphics g = CreateGraphics())
				using (Font font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular))
				{
					measured = TextRenderer.MeasureText(
						g,
						_text,
						font,
						new Size(maxWidth - 70, 0),
						TextFormatFlags.WordBreak | TextFormatFlags.Left | TextFormatFlags.NoPadding);
				}

				int width = Math.Max(220, Math.Min(420, measured.Width + 80));
				int height = Math.Max(70, measured.Height + 30);

				_boxSize = new Size(width, height);
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
		}
	}
}
