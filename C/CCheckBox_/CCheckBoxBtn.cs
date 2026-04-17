
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FrmControl.C.Base;

namespace FrmControl.C
{
    /// <summary>
    /// Class UCBtnExt.
    /// Implements the <see cref="HZH_Controls.Controls.UCControlBase" />
    /// </summary>
    /// <seealso cref="HZH_Controls.Controls.UCControlBase" />
    [DefaultEvent("BtnClick")]

    public partial class UCBtnCheckBox : CBaseControl
    {
        public event EventHandler CheckedChanged;
		#region 字段属性
		private Brush uncheckcolor = new SolidBrush(Color.LightGray);
        public bool Radio { get; set; }
        public Brush UnCheckColor
		{
			get { return uncheckcolor; }
			set { uncheckcolor = value; 
            this.Checked = this.Checked;
            }
        }
        private Brush checkcolor = new SolidBrush(Color.Gray);

        public Brush CheckColor
        {
            get { return checkcolor; }
            set { checkcolor = value;
            }

        }
        private string unchecklabel;

        public string UnCheckLabel
        {
            get { return unchecklabel; }
            set { unchecklabel = value;
				if (!ischecked)
				{
					lbl.Text = unchecklabel;
				}
			}
        }
        private string checklabel;

        public string CheckLabel
        {
            get { return checklabel; }
            set { checklabel = value;
                if (ischecked)
                {
                    lbl.Text = checklabel;
                }
            }

        }
        private bool ischecked = false;
		public bool Checked { get {
                return ischecked;
            } 
            set {
                this.ischecked = value;
                if (ischecked)
                {
                    this.lbl.Text = CheckLabel;
                }
                else { 
                    this.lbl.Text = UnCheckLabel;
                }
                Invalidate();
                CheckedChanged?.Invoke(this,null);
         } }
		private bool enabledMouseEffect = false;
        [Description("是否启用鼠标效果"), Category("自定义")]
        public bool EnabledMouseEffect
        {
            get { return enabledMouseEffect; }
            set { enabledMouseEffect = value; }
        }

        /// <summary>
        /// 是否显示角标
        /// </summary>
        /// <value><c>true</c> if this instance is show tips; otherwise, <c>false</c>.</value>
        [Description("是否显示角标"), Category("自定义")]
        public bool IsShowTips
        {
            get
            {
                return this.lblTips.Visible;
            }
            set
            {
                this.lblTips.Visible = value;
            }
        }
        /// <summary>
        /// 角标文字
        /// </summary>
        /// <value>The tips text.</value>
        [Description("角标文字"), Category("自定义")]
        public string TipsText
        {
            get
            {
                return this.lblTips.Text;
            }
            set
            {
                this.lblTips.Text = value;
            }
        }

        /// <summary>
        /// The BTN back color
        /// </summary>
        private Color _btnBackColor = Color.White;
        /// <summary>
        /// 按钮背景色
        /// </summary>
        /// <value>The color of the BTN back.</value>
        [Description("按钮背景色"), Category("自定义")]
        public Color BtnBackColor
        {
            get { return _btnBackColor; }
            set
            {
                _btnBackColor = value;
                this.BackColor = value;
            }
        }

        /// <summary>
        /// The BTN fore color
        /// </summary>
        private Color _btnForeColor = Color.White;
        /// <summary>
        /// 按钮字体颜色
        /// </summary>
        /// <value>The color of the BTN fore.</value>
        [Description("按钮字体颜色"), Category("自定义")]
        public virtual Color BtnForeColor
        {
            get { return _btnForeColor; }
            set
            {
                _btnForeColor = value;
                this.lbl.ForeColor = value;
            }
        }

        /// <summary>
        /// The BTN font
        /// </summary>
        private Font _btnFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
        /// <summary>
        /// 按钮字体
        /// </summary>
        /// <value>The BTN font.</value>
        [Description("按钮字体"), Category("自定义")]
        public Font BtnFont
        {
            get { return _btnFont; }
            set
            {
                _btnFont = value;
                this.lbl.Font = value;
            }
        }

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        [Description("按钮点击事件"), Category("自定义")]
        public event EventHandler BtnClick;

        /// <summary>
        /// The BTN text
        /// </summary>
        private string _btnText;
        /// <summary>
        /// 按钮文字
        /// </summary>
        /// <value>The BTN text.</value>
        [Description("按钮文字"), Category("自定义")]
        public virtual string BtnText
        {
            get { return _btnText; }
            set
            {
                _btnText = value;
                lbl.Text = value;
            }
        }

        /// <summary>
        /// The m tips color
        /// </summary>
        private Color m_tipsColor = Color.FromArgb(232, 30, 99);
        /// <summary>
        /// 角标颜色
        /// </summary>
        /// <value>The color of the tips.</value>
        [Description("角标颜色"), Category("自定义")]
        public Color TipsColor
        {
            get { return m_tipsColor; }
            set { m_tipsColor = value; }
        }
        [Description("鼠标效果生效时发生，需要和MouseEffected同时使用，否则无效"), Category("自定义")]
        public event EventHandler MouseEffecting;
        [Description("鼠标效果结束时发生，需要和MouseEffecting同时使用，否则无效"), Category("自定义")]
        public event EventHandler MouseEffected;
        #endregion
        /// <summary>
        /// Initializes a new instance of the <see cref="UCBtnExt" /> class.
        /// </summary>
        public UCBtnCheckBox()
        {
            InitializeComponent();
            this.TabStop = false;
            BackColor = Color.Transparent;
            lblTips.Paint += lblTips_Paint;
            this.lbl.MouseEnter += lbl_MouseEnter;
            this.lbl.MouseLeave += lbl_MouseLeave;
            this.BtnClick += ChangeCheck;
            CheckedChanged += thisCheckChanged;
        }

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			// 圆角矩形
			int radius = 8; // 圆角半径
			var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
			var path = RoundedRect(rect, radius);

			// 背景颜色
			Brush bgBrush;
			if (ischecked)
			{
				bgBrush = new SolidBrush((checkcolor as SolidBrush)?.Color ?? Color.Gray);
			}
			else
			{
				bgBrush = new SolidBrush((uncheckcolor as SolidBrush)?.Color ?? Color.LightGray);
			}

			// 鼠标悬停高亮效果
			if (enabledMouseEffect && m_cacheColor != Color.Empty)
			{
				bgBrush = new SolidBrush(m_cacheColor);
			}

			g.FillPath(bgBrush, path);

			// 边框
			using (Pen pen = new Pen(Color.FromArgb(200, 200, 200)))
			{
				g.DrawPath(pen, path);
			}

			// 文字居中绘制
			//TextRenderer.DrawText(
			//	g,
			//	lbl.Text,
			//	lbl.Font,
			//	rect,
			//	lbl.ForeColor,
			//	TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
			//);

			// 绘制右上角的圆形标识
			int circleSize = 12; // 圆形标识大小
			int x = this.Width - circleSize - 4; // X 坐标
			int y = 4; // Y 坐标
			Brush circleBrush = ischecked ? new SolidBrush(Color.LightGray) : new SolidBrush(Color.DarkGray);

			g.FillEllipse(circleBrush, x, y, circleSize, circleSize);

			// 可绘制角标
			if (lblTips.Visible)
			{
				int tipSize = Math.Min(lblTips.Width, lblTips.Height);
				Rectangle tipRect = new Rectangle(this.Width - tipSize - 2, 2, tipSize, tipSize);
				using (Brush tipBrush = new SolidBrush(m_tipsColor))
				{
					g.FillEllipse(tipBrush, tipRect);
				}
				if (!string.IsNullOrEmpty(TipsText))
				{
					var sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
					g.DrawString(TipsText, lblTips.Font, new SolidBrush(lblTips.ForeColor), tipRect, sf);
				}
			}
		}

		// 创建圆角矩形路径
		private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle rect, int radius)
		{
			var path = new System.Drawing.Drawing2D.GraphicsPath();
			path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
			path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
			path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
			path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
			path.CloseFigure();
			return path;
		}

		// 鼠标进入/离开修改颜色
		void lbl_MouseEnter(object sender, EventArgs e)
		{
			if (enabledMouseEffect)
			{
				m_cacheColor = (ischecked ? (checkcolor as SolidBrush)?.Color ?? Color.Gray : (uncheckcolor as SolidBrush)?.Color ?? Color.LightGray)
					.ChangeColor(-0.15f); // 高亮
				Invalidate();
			}
		}

		void lbl_MouseLeave(object sender, EventArgs e)
		{
			if (enabledMouseEffect)
			{
				m_cacheColor = Color.Empty;
				Invalidate();
			}
		}

		private void thisCheckChanged(object sender, EventArgs e)
        {
            if (Parent == null) {
                return;
            }
            if (Radio == false)
            {
                return;
            }
            if (!Checked)
            {
                return;
            }
            var cs = Parent.Controls;
            foreach (var item in cs)
            {
                if (item is UCBtnCheckBox ck)
                {
                    if (item == this)
                    {
                        continue;
                    }
                    if (Checked)
                    {
                        ck.Checked = false;
                        continue;
                    }
                }
            }
           
        }

        private void ChangeCheck(object sender, EventArgs e)
		{
            this.Checked = !this.Checked;
		}

        public Color FillColor { get=>BackColor; set=> BackColor = value; }

        Color m_cacheColor = Color.Empty;
   

        /// <summary>
        /// Handles the Paint event of the lblTips control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs" /> instance containing the event data.</param>
        void lblTips_Paint(object sender, PaintEventArgs e)
        {
         }

        /// <summary>
        /// Handles the MouseDown event of the lbl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private void lbl_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.BtnClick != null)
                BtnClick(this, e);
        }
    }
}
