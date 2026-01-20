using FrmControl.C.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrmControl.C.CBtn_
{
    [DefaultEvent("MouseClickNotDrag")]
    public partial class FrmTips : CBaseControl
    {
        public event MouseEventHandler MouseClickNotDrag;
        private bool _isMouseDown = false;       // 鼠标是否按下
        private bool _isDragging = false;        // 是否正在拖拽
        private Point _mouseDownPoint;           // 鼠标按下点
        private DateTime _mouseDownTime;         // 鼠标按下时间
        private const int HoldThreshold = 300;   // 长按阈值（毫秒）

        public FrmTips()
        {
            InitializeComponent();

            this.MouseDown += FrmTips_MouseDown;
            this.MouseMove += FrmTips_MouseMove;
            this.MouseUp += FrmTips_MouseUp;
            this.MouseClick += FrmTips_MouseClick;
        }

        private void FrmTips_MouseDown(object sender, MouseEventArgs e)
        {
            _isMouseDown = true;
            _isDragging = false;
            _mouseDownPoint = e.Location;
            _mouseDownTime = DateTime.Now;
        }

        private void FrmTips_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                // 判断是否达到长按时间
                if (!_isDragging)
                {
                    if ((DateTime.Now - _mouseDownTime).TotalMilliseconds >= HoldThreshold)
                    {
                        _isDragging = true; // 开始进入拖拽模式
                    }
                }

                if (_isDragging)
                {
                    // 执行拖拽移动
                    var dx = e.X - _mouseDownPoint.X;
                    var dy = e.Y - _mouseDownPoint.Y;

                    this.Left += dx;
                    this.Top += dy;
                }
            }
        }

        private void FrmTips_MouseUp(object sender, MouseEventArgs e)
        {
           
            _isMouseDown = false;
            // 若正在拖拽，则松开时不触发点击事件

            if (_isDragging)
            {
                _isDragging = false;
                SnapToEdge();   
            }
        }
        public void SnapToEdge()
        {
            if (this.Parent == null) return;

            int parentWidth = this.Parent.ClientSize.Width;
            int parentHeight = this.Parent.ClientSize.Height;

            int leftDist = this.Left;
            int rightDist = parentWidth - (this.Left + this.Width);
            int topDist = this.Top;
            int bottomDist = parentHeight - (this.Top + this.Height);

            // 找出最近的边
            int minDist = Math.Min(Math.Min(leftDist, rightDist), Math.Min(topDist, bottomDist));

            if (minDist == leftDist)
            {
                this.Left = 0;
            }
            else if (minDist == rightDist)
            {
                this.Left = parentWidth - this.Width;
            }
            else if (minDist == topDist)
            {
                this.Top = 0;
            }
            else if (minDist == bottomDist)
            {
                this.Top = parentHeight - this.Height;
            }
        }

        private void FrmTips_MouseClick(object sender, MouseEventArgs e)
        {
            // 只有未进入拖拽模式时才认为是点击
            if (!_isDragging)
            {
                MouseClickNotDrag?.Invoke(this, e);
            }
        }
    }

}
