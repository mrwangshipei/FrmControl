using FCT.MyControls;
using FrmControl.C.Base;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace FrmControl.C.Btn
{
	[DefaultEvent("Click")]
	public class FrmLineBtn : CBaseControl
	{
		// 默认背景颜色
		public Color defaultBackColor
		{
			get => defaultBackColor1;
			set
			{
				defaultBackColor1 = value;
				this.BackColor = value;
				this.Invalidate();
			}
		}

		// 鼠标悬停时的背景颜色
		public Color hoverBackColor { get; set; } = ThemeColors.CardHover;

		// 鼠标按下时的背景颜色
		public Color pressedBackColor { get; set; } = ThemeColors.CardPressed;

		// 控制按钮上图标大小
		public float smallimg
		{
			get => smallimg1;
			set
			{
				smallimg1 = value;
				this.Invalidate();
			}
		}

		// 边框宽度
		public float BorderWidth
		{
			get => borderWidth;
			set
			{
				borderWidth = value;
				this.Invalidate();
			}
		}

		// 边框颜色
		public Color BorderColor
		{
			get => borderColor;
			set
			{
				borderColor = value;
				this.Invalidate();
			}
		}

		// 按钮显示的文本
		public string FrmText
		{
			get => frmText;
			set
			{
				frmText = value;
				this.Invalidate();
			}
		}

		// 按钮背景图片
		public Image BackImg { get; set; }

		// 图标缩放比例
		public float ImgPix { get; set; } = 0f;

		private float lastell;
		public float ell;
		private bool IsMouseDown;
		public bool Issquare { get; set; }

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
				return cp;
			}
		}

		// 默认圆角
		public float Radius { get; set; } = 10f;

		// 左侧竖线宽度
		public float LineWidth { get; set; } = 8f;

		private bool isHover = false;

		// 左侧竖线颜色
		public Color LineColor { get; set; } = ThemeColors.Accent;

		// hover 时竖线颜色
		public Color LineHoverColor { get; set; } = ThemeColors.AccentLight;

		private Rectangle Lastr;
		private Color defaultBackColor1 = ThemeColors.PureWhite;
		private string frmText = "按钮";
		private float borderWidth = 1f;
		private Color borderColor = ThemeColors.Border;
		private float smallimg1 = 1f;
		private GraphicsPath lastRegionPath;

		public FrmLineBtn()
		{
			this.SetStyle(
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.UserPaint |
				ControlStyles.ResizeRedraw |
				ControlStyles.SupportsTransparentBackColor,
				true);

			this.DoubleBuffered = true;

			// 统一主题色
			defaultBackColor = ThemeColors.PureWhite;
			hoverBackColor = ThemeColors.CardHover;
			pressedBackColor = ThemeColors.CardPressed;
			BorderColor = ThemeColors.Border;
			ForeColor = ThemeColors.TextPrimary;
			Radius = 10f;

			// 默认字体：优先微软雅黑 UI，更现代；没有则回退
			this.Font = CreateDefaultFont();
		}

		private Font CreateDefaultFont()
		{
			try
			{
				return new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
			}
			catch
			{
				try
				{
					return new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
				}
				catch
				{
					return new Font("宋体", 10f, FontStyle.Regular, GraphicsUnit.Point);
				}
			}
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			g.Clear(Parent?.BackColor ?? ThemeColors.PageBack);

			Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

			using (GraphicsPath btnPath = GraphicsExtensions.GetRoundedRectangle(rect, Radius))
			{
				bool needUpdateRegion = true;

				if (lastRegionPath != null)
				{
					// 比较边界矩形，如果相同就不更新
					var lastBounds = lastRegionPath.GetBounds();
					var newBounds = btnPath.GetBounds();
					needUpdateRegion = !lastBounds.Equals(newBounds);
				}

				if (needUpdateRegion)
				{
					this.Region?.Dispose(); // 释放旧Region
					this.Region = new Region(btnPath);
					lastRegionPath?.Dispose();
					lastRegionPath = (GraphicsPath)btnPath.Clone(); // 缓存Path
				}

				// 背景色
				Color back = IsMouseDown ? pressedBackColor : isHover ? hoverBackColor : defaultBackColor;
				using (SolidBrush backBrush = new SolidBrush(back))
					g.FillPath(backBrush, btnPath);

				// 边框
				if (BorderWidth > 0)
				{
					using (Pen pen = new Pen(BorderColor, BorderWidth))
					{
						pen.Alignment = PenAlignment.Inset;
						g.DrawPath(pen, btnPath);
					}
				}
			}

			// 左侧圆角竖线
			float padding = 4f;
			RectangleF lineRect = new RectangleF(
				padding,
				padding,
				LineWidth,
				Height - padding * 2 - 1
			);

			float rr = LineWidth / 2f;
			using (GraphicsPath linePath = new GraphicsPath())
			{
				linePath.AddArc(lineRect.X, lineRect.Y, rr * 2, rr * 2, 180, 180);
				linePath.AddArc(lineRect.X, lineRect.Bottom - rr * 2, rr * 2, rr * 2, 0, 180);
				linePath.CloseFigure();

				Color lineBase = isHover ? LineHoverColor : LineColor;

				using (LinearGradientBrush lineBrush = new LinearGradientBrush(
					lineRect,
					ControlPaint.Light(lineBase, 0.15f),
					lineBase,
					LinearGradientMode.Vertical))
				{
					g.FillPath(lineBrush, linePath);
				}
			}

			float textLeft = padding + LineWidth + 10f;

			// 如果有图标，文字右移
			if (BackImg != null)
			{
				int imgSize = (int)(Height * 0.45f * (ImgPix > 0 ? ImgPix : 1f));
				int imgX = (int)textLeft;
				int imgY = (Height - imgSize) / 2;

				g.DrawImage(BackImg, imgX, imgY, imgSize, imgSize);
				textLeft += imgSize + 8f;
			}

			// 文字
			RectangleF textRect = new RectangleF(
				textLeft,
				0,
				Width - textLeft - 6,
				Height
			);

			using (StringFormat sf = new StringFormat())
			{
				sf.LineAlignment = StringAlignment.Center;
				sf.Alignment = StringAlignment.Near;
				sf.Trimming = StringTrimming.EllipsisCharacter;
				sf.FormatFlags = StringFormatFlags.NoWrap;

				using (SolidBrush textBrush = new SolidBrush(ForeColor))
				{
					g.DrawString(FrmText, Font, textBrush, textRect, sf);
				}
			}

			base.OnPaint(e);
		}

		//protected override void OnPaint(PaintEventArgs e)
		//{
		//	Graphics g = e.Graphics;

		//	// 高质量抗锯齿与文本渲染
		//	g.SmoothingMode = SmoothingMode.AntiAlias;
		//	g.CompositingQuality = CompositingQuality.HighQuality;
		//	g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		//	g.PixelOffsetMode = PixelOffsetMode.HighQuality;
		//	g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

		//	g.Clear(Parent?.BackColor ?? ThemeColors.PageBack);

		//	Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

		//	// 整体圆角区域
		//	using (GraphicsPath btnPath = GraphicsExtensions.GetRoundedRectangle(rect, Radius))
		//	{
		//		this.Region = new Region(btnPath);

		//		// 背景色
		//		Color back = defaultBackColor;
		//		if (IsMouseDown)
		//			back = pressedBackColor;
		//		else if (isHover)
		//			back = hoverBackColor;

		//		using (SolidBrush backBrush = new SolidBrush(back))
		//		{
		//			g.FillPath(backBrush, btnPath);
		//		}

		//		// 边框
		//		if (BorderWidth > 0)
		//		{
		//			using (Pen pen = new Pen(BorderColor, BorderWidth))
		//			{
		//				pen.Alignment = PenAlignment.Inset;
		//				g.DrawPath(pen, btnPath);
		//			}
		//		}
		//	}

		//	// 左侧圆角竖线
		//	float padding = 4f;
		//	RectangleF lineRect = new RectangleF(
		//		padding,
		//		padding,
		//		LineWidth,
		//		Height - padding * 2 - 1
		//	);

		//	float rr = LineWidth / 2f;
		//	using (GraphicsPath linePath = new GraphicsPath())
		//	{
		//		linePath.AddArc(lineRect.X, lineRect.Y, rr * 2, rr * 2, 180, 180);
		//		linePath.AddArc(lineRect.X, lineRect.Bottom - rr * 2, rr * 2, rr * 2, 0, 180);
		//		linePath.CloseFigure();

		//		Color lineBase = isHover ? LineHoverColor : LineColor;

		//		using (LinearGradientBrush lineBrush = new LinearGradientBrush(
		//			lineRect,
		//			ControlPaint.Light(lineBase, 0.15f),
		//			lineBase,
		//			LinearGradientMode.Vertical))
		//		{
		//			g.FillPath(lineBrush, linePath);
		//		}
		//	}

		//	float textLeft = padding + LineWidth + 10f;

		//	// 如果有图标，文字右移
		//	if (BackImg != null)
		//	{
		//		int imgSize = (int)(Height * 0.45f * (ImgPix > 0 ? ImgPix : 1f));
		//		int imgX = (int)textLeft;
		//		int imgY = (Height - imgSize) / 2;

		//		g.DrawImage(BackImg, imgX, imgY, imgSize, imgSize);
		//		textLeft += imgSize + 8f;
		//	}

		//	// 文字
		//	RectangleF textRect = new RectangleF(
		//		textLeft,
		//		0,
		//		Width - textLeft - 6,
		//		Height
		//	);

		//	using (StringFormat sf = new StringFormat())
		//	{
		//		sf.LineAlignment = StringAlignment.Center;
		//		sf.Alignment = StringAlignment.Near;
		//		sf.Trimming = StringTrimming.EllipsisCharacter;
		//		sf.FormatFlags = StringFormatFlags.NoWrap;

		//		using (SolidBrush textBrush = new SolidBrush(ForeColor))
		//		{
		//			g.DrawString(FrmText, Font, textBrush, textRect, sf);
		//		}
		//	}

		//	base.OnPaint(e);
		//}

		protected override void OnMouseEnter(EventArgs e)
		{
			if (IsDisposed) return;
			isHover = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (IsDisposed) return;
			isHover = false;
			if (!IsMouseDown)
			{
				this.BackColor = defaultBackColor;
			}
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (IsDisposed) return;
			IsMouseDown = true;
			Invalidate();
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (IsDisposed) return;
			IsMouseDown = false;
			isHover = this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition));
			Invalidate();
			base.OnMouseUp(e);
		}
	}
}
