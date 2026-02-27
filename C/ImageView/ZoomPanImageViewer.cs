using System.Drawing.Drawing2D;

public class ZoomPanImageViewer : Control
{
	private Image _image;
	private float _zoom = 1f;
	private PointF _pan = new PointF(0, 0); // in screen pixels
	private bool _panning;
	private Point _lastMouse;
	// --- Selection (Ctrl + Drag) ---
	private bool _selecting;
	private Point _selectStartScreen;
	private Point _selectEndScreen;

	// Image-space ROI (最终你要用的)
	public RectangleF? SelectedImageRect { get; private set; }
	// --- Selection in IMAGE space (关键！) ---
	private PointF _selectStartImage;
	private PointF _selectEndImage;

	// Zoom limits (industrial big image: keep reasonable bounds)
	public float MinZoom { get; set; } = 0.02f;
	public float MaxZoom { get; set; } = 80f;

	// For pixel-accurate viewing
	public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.NearestNeighbor;
	public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.Half;

	public Image Image
	{
		get => _image;
		set
		{
			_image = value;
			FitToWindow();
			Invalidate();
		}
	}

	public float Zoom => _zoom;
	public PointF Pan => _pan;

	public ZoomPanImageViewer()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint |
				 ControlStyles.OptimizedDoubleBuffer |
				 ControlStyles.UserPaint |
				 ControlStyles.ResizeRedraw, true);

		BackColor = Color.FromArgb(30, 30, 30);
		Cursor = Cursors.Hand;

		MouseWheel += ZoomPanImageViewer_MouseWheel;
		MouseDown += ZoomPanImageViewer_MouseDown;
		MouseMove += ZoomPanImageViewer_MouseMove;
		MouseUp += ZoomPanImageViewer_MouseUp;
		MouseDoubleClick += ZoomPanImageViewer_MouseDoubleClick;
		MouseClick += ZoomPanImageViewer_MouseClick;
	}
	private void ZoomPanImageViewer_MouseDoubleClick(object sender, MouseEventArgs e)
	{ 
		// Double click = fit 
		FitToWindow();
	}
	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		
			e.Graphics.Clear(BackColor);
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.InterpolationMode = InterpolationMode;
			e.Graphics.PixelOffsetMode = PixelOffsetMode;
			e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;

			var imgToDraw = DisplayImage;
			if (imgToDraw == null) return;

			using (var m = GetImageToScreenMatrix())
			{
				e.Graphics.Transform = m;

				
				using (var safe = (Bitmap)imgToDraw.Clone())
				{
					e.Graphics.DrawImage(
						safe,
						new RectangleF(0, 0, safe.Width, safe.Height)
					);
				}

				e.Graphics.ResetTransform();
			}
			if (DisplayImage == _processedImage)
			{
				return;
			}
			// Optional: HUD (zoom)
			DrawHud(e.Graphics); 
			if (_selecting || SelectedImageRect != null)
			{ 
				DrawSelectionOverlay(e.Graphics);
			}

	}

	private void DrawHud(Graphics g) { 
		string text = _image == null ? "No Image" : $"Zoom: {_zoom * 100:0.##}%"; 
		using var br = new SolidBrush(Color.FromArgb(200, 240, 240, 240)); 
		using var bg = new SolidBrush(Color.FromArgb(140, 0, 0, 0));
		var size = g.MeasureString(text, Font); 
		var rect = new RectangleF(8, 8, size.Width + 10, size.Height + 6); 
		g.FillRectangle(bg, rect);
		g.DrawString(text, Font, br, 13, 11);
	}
	private void DrawSelectionOverlay(Graphics g)
	{
		Rectangle r = GetSelectionScreenRect();
		if (r.Width <= 0 || r.Height <= 0) return;

		using (var dark = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
		{
			g.FillRectangle(dark, new Rectangle(ClientRectangle.Left, ClientRectangle.Top,
												ClientRectangle.Width, r.Top - ClientRectangle.Top));
			g.FillRectangle(dark, new Rectangle(ClientRectangle.Left, r.Bottom,
												ClientRectangle.Width, ClientRectangle.Bottom - r.Bottom));
			g.FillRectangle(dark, new Rectangle(ClientRectangle.Left, r.Top,
												r.Left - ClientRectangle.Left, r.Height));
			g.FillRectangle(dark, new Rectangle(r.Right, r.Top,
												ClientRectangle.Right - r.Right, r.Height));
		}

		using (var pen = new Pen(Color.Lime, 1) { DashStyle = DashStyle.Dash })
		{
			g.DrawRectangle(pen, r);
		}
	}

	private Rectangle GetSelectionScreenRect()
	{
		RectangleF? imgRect = null;

		if (_selecting)
		{
			// 选区进行中：用临时 image rect（避免 MouseUp 前 SelectedImageRect 为空）
			float x = Math.Min(_selectStartImage.X, _selectEndImage.X);
			float y = Math.Min(_selectStartImage.Y, _selectEndImage.Y);
			float w = Math.Abs(_selectStartImage.X - _selectEndImage.X);
			float h = Math.Abs(_selectStartImage.Y - _selectEndImage.Y);
			if (w >= 1 && h >= 1) imgRect = new RectangleF(x, y, w, h);
		}
		else
		{
			imgRect = SelectedImageRect;
		}

		if (_image == null || imgRect == null) return Rectangle.Empty;

		var r = imgRect.Value;

		// 把 image rect 四角变换到 screen
		PointF p1 = ImageToScreen(new PointF(r.Left, r.Top));
		PointF p2 = ImageToScreen(new PointF(r.Right, r.Top));
		PointF p3 = ImageToScreen(new PointF(r.Left, r.Bottom));
		PointF p4 = ImageToScreen(new PointF(r.Right, r.Bottom));

		float minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
		float minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
		float maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
		float maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

		var screenRectF = RectangleF.FromLTRB(minX, minY, maxX, maxY);
		screenRectF = RectangleF.Intersect(screenRectF, ClientRectangle);
		if (screenRectF.IsEmpty) return Rectangle.Empty;

		return Rectangle.Round(screenRectF);
	}

	private RectangleF GetImageScreenBounds()
	{
		if (_image == null) return RectangleF.Empty;

		using var m = GetImageToScreenMatrix();
		PointF[] pts =
		{
		new PointF(0, 0),
		new PointF(_image.Width, 0),
		new PointF(0, _image.Height),
		new PointF(_image.Width, _image.Height)
	};
		m.TransformPoints(pts);

		float minX = pts.Min(p => p.X);
		float minY = pts.Min(p => p.Y);
		float maxX = pts.Max(p => p.X);
		float maxY = pts.Max(p => p.Y);

		return RectangleF.FromLTRB(minX, minY, maxX, maxY);
	}

	private Point ClampToImageScreen(Point p)
	{
		var r = GetImageScreenBounds();
		if (r.IsEmpty) return p;

		// 同时限制在控件内部（避免越界）
		r = RectangleF.Intersect(r, ClientRectangle);
		if (r.IsEmpty) return p;

		int x = (int)Math.Round(Math.Max(r.Left, Math.Min(p.X, r.Right)));
		int y = (int)Math.Round(Math.Max(r.Top, Math.Min(p.Y, r.Bottom)));
		return new Point(x, y);
	}


	/// <summary>
	/// 构建【图像坐标 → 屏幕坐标】的变换矩阵
	/// 图像坐标：以图像左上角为 (0,0)，单位是 image pixel
	/// 屏幕坐标：控件 client 区域像素坐标
	/// </summary>
	/// <remarks>
	/// 变换顺序：
	/// 1. 先平移（Pan）
	/// 2. 再缩放（Zoom）
	///
	/// 对任意 image 点 (ix, iy)，映射关系为：
	/// screenX = ix * _zoom + _pan.X
	/// screenY = iy * _zoom + _pan.Y
	/// </remarks>
	private Matrix GetImageToScreenMatrix()
	{
		var m = new Matrix();

		// 平移：控制图像在屏幕上的偏移量（单位：屏幕像素）
		// 等价于把整个图像整体拖动
		m.Translate(_pan.X, _pan.Y);

		// 缩放：控制图像的缩放比例
		// 缩放中心是 image 的 (0,0)
		m.Scale(_zoom, _zoom);

		return m;
	}

	/// <summary>
	/// 将【屏幕坐标】转换为【图像坐标】
	/// 常用于：
	/// - 鼠标点击 / 拖拽时，反算当前指向的是图像的哪个像素
	/// - ROI 选区（image-space selection）
	/// </summary>
	/// <param name="screenPt">
	/// 屏幕坐标（控件 client 区域中的像素点）
	/// </param>
	/// <returns>
	/// 对应的图像坐标（image pixel，允许为小数）
	/// </returns>
	public PointF ScreenToImage(Point screenPt)
	{
		// 获取 image → screen 的矩阵
		using var m = GetImageToScreenMatrix();

		// 反转矩阵，得到 screen → image 的变换
		// 这是整个“坐标反算”的关键
		m.Invert();

		// Graphics.TransformPoints 只能处理 PointF[]
		PointF[] pts = { new PointF(screenPt.X, screenPt.Y) };

		// 执行坐标变换（screen → image）
		m.TransformPoints(pts);

		return pts[0];
	}
	public PointF ImageToScreen(PointF imagePt)
	{
		using var m = GetImageToScreenMatrix();
		PointF[] pts = { imagePt };
		m.TransformPoints(pts);
		return pts[0];
	}

	// --- Interaction ---
	private void ZoomPanImageViewer_MouseWheel(object sender, MouseEventArgs e)
	{
		if (_image == null) return;

		// Zoom factor per wheel notch
		float factor = e.Delta > 0 ? 1.15f : 1f / 1.15f;

		// Keep image point under cursor stable:
		// Before zoom: cursor maps to image point P
		PointF imgBefore = ScreenToImage(e.Location);

		float newZoom = Clamp(_zoom * factor, MinZoom, MaxZoom);
		if (Math.Abs(newZoom - _zoom) < 1e-6) return;

		_zoom = newZoom;

		// After zoom: find where P would go, adjust pan so it stays under cursor
		PointF screenAfter = ImageToScreen(imgBefore);
		_pan = new PointF(_pan.X + (e.Location.X - screenAfter.X), _pan.Y + (e.Location.Y - screenAfter.Y));

		Invalidate();
	}

	//private void ZoomPanImageViewer_MouseDown(object sender, MouseEventArgs e)
	//{
	//	if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
	//	{
	//		_panning = true;
	//		_lastMouse = e.Location;
	//		Cursor = Cursors.SizeAll;
	//	}
	//}
	private void ZoomPanImageViewer_MouseDown(object sender, MouseEventArgs e)
	{
		if (_image == null) return;

		if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control))
		{
			_selecting = true;

			Point clamped = ClampToImageScreen(e.Location);
			//_selectStartImage = ScreenToImage(clamped);
			//_selectEndImage = _selectStartImage;
			// 鼠标按下时，记录选区起点（image-space）
			// 注意：不是屏幕坐标，而是图像坐标
			_selectStartImage = ScreenToImage(e.Location);

			// 初始化选区终点为起点
			// 后续在 MouseMove 中持续更新 _selectEndImage
			_selectEndImage = _selectStartImage;
			UpdateSelectedImageRect();
			Cursor = Cursors.Cross;
			Invalidate();
			return;
		}

		// 原有拖拽平移逻辑
		if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
		{
			_panning = true;
			_lastMouse = e.Location;
			Cursor = Cursors.SizeAll;
		}
	}

	//private void ZoomPanImageViewer_MouseMove(object sender, MouseEventArgs e)
	//{
	//	if (_panning)
	//	{
	//		int dx = e.X - _lastMouse.X;
	//		int dy = e.Y - _lastMouse.Y;
	//		_pan = new PointF(_pan.X + dx, _pan.Y + dy);
	//		_lastMouse = e.Location;
	//		Invalidate();
	//	}
	//}
	private void ZoomPanImageViewer_MouseMove(object sender, MouseEventArgs e)
	{
		if (_selecting)
		{
			//Point clamped = ClampToImageScreen(e.Location);
		//	_selectEndImage = ScreenToImage(clamped);
			_selectEndImage = ScreenToImage(e.Location);

			UpdateSelectedImageRect();
			Invalidate();
			return;
		}

		if (_panning)
		{
			int dx = e.X - _lastMouse.X;
			int dy = e.Y - _lastMouse.Y;
			_pan = new PointF(_pan.X + dx, _pan.Y + dy);
			_lastMouse = e.Location;
			Invalidate(); // ✅ 因为选区是 image-space，重画就会跟着pan一起动
		}
	}


	//private void ZoomPanImageViewer_MouseUp(object sender, MouseEventArgs e)
	//{
	//	_panning = false;
	//	Cursor = Cursors.Hand;
	//}
	private void ZoomPanImageViewer_MouseUp(object sender, MouseEventArgs e)
	{
		if (_selecting)
		{
			_selecting = false;
			Cursor = Cursors.Hand;

			if (SelectedImageRect != null)
			{
				ZoomToImageRect(SelectedImageRect.Value);
			}

			Invalidate();
			return;
		}

		_panning = false;
		Cursor = Cursors.Hand;
	}

	public void ZoomToImageRect(RectangleF roi, float margin = 0.05f)
	{
		if (_image == null) return;
		if (roi.Width <= 0 || roi.Height <= 0) return;

		// 1️⃣ 计算目标 zoom（保持比例）
		float zx = ClientSize.Width / roi.Width;
		float zy = ClientSize.Height / roi.Height;
		float targetZoom = Math.Min(zx, zy);

		// 留边距（5% 很舒服）
		targetZoom *= (1f - margin);

		targetZoom = Clamp(targetZoom, MinZoom, MaxZoom);

		// 2️⃣ ROI 中心（image-space）
		float cx = roi.Left + roi.Width / 2f;
		float cy = roi.Top + roi.Height / 2f;

		// 3️⃣ 让 ROI 中心落在屏幕中心
		_zoom = targetZoom;

		_pan = new PointF(
			ClientSize.Width / 2f - cx * _zoom,
			ClientSize.Height / 2f - cy * _zoom
		);

		Invalidate();
	}

	private void UpdateSelectedImageRect()
	{
		if (_image == null)
		{
			SelectedImageRect = null;
			return;
		}

		float x1 = _selectStartImage.X;
		float y1 = _selectStartImage.Y;
		float x2 = _selectEndImage.X;
		float y2 = _selectEndImage.Y;

		float left = (float)Math.Floor(Math.Min(x1, x2));
		float top = (float)Math.Floor(Math.Min(y1, y2));
		float right = (float)Math.Ceiling(Math.Max(x1, x2));
		float bottom = (float)Math.Ceiling(Math.Max(y1, y2));

		// Clamp to image bounds
		//left = Math.Max(0, left);
		//top = Math.Max(0, top);
		//right = Math.Min(_image.Width, right);
		//bottom = Math.Min(_image.Height, bottom);
		// Clamp to IMAGE bounds (0,0) 是图像左上角
		left = Math.Max(0, left);
		top = Math.Max(0, top);
		right = Math.Min(_image.Width, right);
		bottom = Math.Min(_image.Height, bottom);

		float w = right - left;
		float h = bottom - top;

		if (w < 1 || h < 1)
		{
			SelectedImageRect = null;
			return;
		}

		SelectedImageRect = new RectangleF(left, top, w, h);
	}



	private void ZoomPanImageViewer_MouseClick(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right)
		{
			// Right click = 100% (actual size), centered at cursor
			SetZoomAtPoint(1f, e.Location);
		}
	}

	public void FitToWindow()
	{
		if (_image == null || ClientSize.Width <= 0 || ClientSize.Height <= 0) { Invalidate(); return; }

		float zx = (float)ClientSize.Width / _image.Width;
		float zy = (float)ClientSize.Height / _image.Height;
		_zoom = Clamp(Math.Min(zx, zy), MinZoom, MaxZoom);

		// center
		float w = _image.Width * _zoom;
		float h = _image.Height * _zoom;
		_pan = new PointF((ClientSize.Width - w) / 2f, (ClientSize.Height - h) / 2f);

		Invalidate();
	}
	private Image _processedImage;
	private Image DisplayImage => _processedImage ?? _image;

	/// <summary>
	/// 可选的处理后图像：
	/// 不为 null 时显示它；
	/// 为 null 时显示原始 Image
	/// </summary>
	public Image ProcessedImage
	{
		get => _processedImage;
		set
		{
			_processedImage = value;
			Invalidate(); // 只需重绘，不动 zoom / pan
		}
	}

	public void SetZoomAtPoint(float zoom, Point screenPoint)
	{
		if (_image == null) return;

		PointF imgBefore = ScreenToImage(screenPoint);
		_zoom = Clamp(zoom, MinZoom, MaxZoom);

		PointF screenAfter = ImageToScreen(imgBefore);
		_pan = new PointF(_pan.X + (screenPoint.X - screenAfter.X), _pan.Y + (screenPoint.Y - screenAfter.Y));

		Invalidate();
	}

	private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
}
