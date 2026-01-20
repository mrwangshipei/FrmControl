using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrmControl.C.Btn
{
	public partial class Apple3Btn : UserControl
	{
      
        public Padding _btnPad;
        public Padding BtnPad
        {
            get => cPanel1.Padding;
            set
            {
                cPanel1.Padding = value;
                cPanel2.Padding = value;
                cPanel3.Padding = value;
                _btnPad = value;
            }
        }
        public Apple3Btn()
		{
            InitializeComponent();
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint|ControlStyles.SupportsTransparentBackColor, true);
			this.DoubleBuffered = true;
		}

		private void frmBtn1_Click(object sender, EventArgs e)
		{
			this.FindForm().WindowState = FormWindowState.Minimized;
		}

		private void frmBtn3_Click(object sender, EventArgs e)
		{
			this.FindForm().Close();
		}

		private void frmBtn2_Click(object sender, EventArgs e)
		{
			if (this.FindForm().WindowState == FormWindowState.Maximized)
			{
				this.FindForm().WindowState = FormWindowState.Normal;
			}
			else
			{
				this.FindForm().WindowState = FormWindowState.Maximized;
			}

		}

		private void Apple3Btn_SizeChanged(object sender, EventArgs e)
		{
		//	this.frmBtn1.Radius = this.frmBtn1.Width / 2;
			//this.frmBtn2.Radius = this.frmBtn2.Width / 2;
		//	this.frmBtn3.Radius = this.frmBtn3.Width / 2;
		}

		private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
		{

		}
        // 动态计算高光和阴影颜色
        private Color GetHighlightColor(FrmBtn btn)
        {
            return AdjustColor(btn.BackColor, 1.2f); // 增加20%亮度
        }

        private Color GetShadowColor(FrmBtn btn)
        {
            return AdjustColor(btn.BackColor, 0.8f); // 减少20%亮度
        }

        // 颜色调整辅助方法
        private Color AdjustColor(Color color, float factor)
        {
            int r = (int)(color.R * factor);
            int g = (int)(color.G * factor);
            int b = (int)(color.B * factor);
            return Color.FromArgb(
                Math.Min(r, 255),
                Math.Min(g, 255),
                Math.Min(b, 255)
            );
        }

        private void frmBtn3_Paint(object sender, PaintEventArgs e)
        {
            var btn = sender as FrmBtn;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // 抗锯齿

            // 1. 绘制圆形区域
            int diameter = Math.Min(btn.ClientSize.Width, btn.ClientSize.Height);
            Rectangle rect = new Rectangle(0, 0, diameter, diameter);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(rect);
               // btn.Region = new Region(path); // 裁剪为圆形
            }

            // 2. 绘制立体阴影（模拟Mac按钮凹陷效果）
            using (LinearGradientBrush shadowBrush = new LinearGradientBrush(
                new Rectangle(2, 2, diameter, diameter),
               Color.FromArgb(255, GetShadowColor(btn)),
                Color.Transparent,
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(shadowBrush, 2, 2, diameter, diameter);
            }

            // 3. 绘制主按钮
            using (LinearGradientBrush mainBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(255, GetHighlightColor(btn)), // 高光色
               Color.FromArgb(255, GetShadowColor(btn)), // 底色
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(mainBrush, 0, 0, diameter, diameter);
            }

            // 4. 绘制边框
           // ControlPaint.DrawBorder(g, rect, Color.FromArgb(100, 100, 100), ButtonBorderStyle.Solid);

        }

        private void cPanel3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
