using FCT.Model;
using FrmControl.C.Base;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace FrmControl.C.CPanel_
{
    public class CPanel:CBasePanel
    {
        public Color backTColor ;
        private float backTColorTran = 0.25f;
        private int radius = 5;

        public Color BackTColor 
        { 
            get => backTColor; 
            set 
            { 
                backTColor = value;
                Invalidate(); 
            } 
        }
        public float BackTColorTran 
        { 
            get => backTColorTran; 
            set 
            { 
                backTColorTran = value;
            Invalidate();

            }
        }
        public int Radius
        { 
            get => radius;
            set 
            {
                radius = value;
                Invalidate();
            }
        }
    
      
        // ------- 构造函数 -------
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
          
        }
        // —— 私有字段 —— 

        private Bitmap _originalSnapshot = null;
        private Bitmap _currentBlurred = null;

        /// <summary>
        /// 动画进度：从 0.0f → 1.0f 线性增长。0 表示“最模糊+最小”；1 表示“无模糊+原始大小”。
        /// </summary>
        private float _animationProgress = 0f;

        private Timer _animationTimer;

        /// <summary>
        /// 每帧进度增量（你可以根据需要改成 0.02 或 0.04 等），
        /// 这决定了动画多少帧完成以及时长（Interval × 帧数 ≈ 总时长）。
        /// </summary>
        private const float ProgressStep = 0.05f; // 大约 20 帧从 0→1

        // —— 构造函数 —— 

        public CPanel()
        {
            // 打开双缓冲，减少闪烁
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer|
                ControlStyles.SupportsTransparentBackColor,
                true);
            DoubleBuffered = true;
			this.UpdateStyles();

		}


	

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            Invalidate();            
        }
		//protected override void OnPaintBackground(PaintEventArgs e)
		//{
		//    base.OnPaintBackground(e);
		//    var g = e.Graphics;
		//    Rectangle rectangle = this.ClientRectangle;
		//    rectangle.ChangeRectangle(this.Padding);
		//    using (var path = rectangle.CreateRoundedRectanglePath(Radius))
		//    {
		//        using (Brush br = new SolidBrush(Color.FromArgb((int)(255*BackTColorTran),BackTColor)))
		//        {
		//            g.FillPath(br, path);
		//        }
		//    }

		//}
		private GraphicsPath _cachedPath;
		private Rectangle _cachedRect;
		private int _cachedRadius = -1;
		private Color _cachedColor;
		private Brush _cachedBrush;

		private void EnsureCache()
		{
			var rect = this.ClientRectangle;
			rect.ChangeRectangle(this.Padding);
			var color = Color.FromArgb((int)(255 * BackTColorTran), BackTColor);

			if (_cachedPath != null &&
				rect == _cachedRect &&
				Radius == _cachedRadius &&
				color == _cachedColor)
				return;

			_cachedPath?.Dispose();
			_cachedBrush?.Dispose();

			_cachedRect = rect;
			_cachedRadius = Radius;
			_cachedColor = color;

			_cachedPath = rect.CreateRoundedRectanglePath(Radius);
			_cachedBrush = new SolidBrush(color);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			InvalidateCache();
		}

		private void InvalidateCache()
		{
			_cachedPath?.Dispose(); _cachedPath = null;
			_cachedBrush?.Dispose(); _cachedBrush = null;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			EnsureCache();
			if (_cachedPath != null && _cachedBrush != null)
				e.Graphics.FillPath(_cachedBrush, _cachedPath);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) InvalidateCache();
			base.Dispose(disposing);
		}


	}
}
