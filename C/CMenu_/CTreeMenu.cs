using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrmControl.C.Base;
using FrmControl.C.CMenu_.Node_;

namespace FrmControl.C.CMenu_
{
    public class CTreeMenu : CBaseControl
    {
        public event EventHandler<ICTreeNode> SelectedNodeChanged;
        private Padding radius;
        volatile private CTreeNodeCollection nodes = new CTreeNodeCollection();
        private Color nodeBackColor = Color.LightGray;
        private Color nodeForeColor = Color.Black;
        private Color selectedNodeBackColor = Color.DimGray;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        public ICTreeNode SelectedNode { get => selectedNode;
            set
            {

                this.DoInvoke(new Action(() =>
                {
                    selectedNode = value;
                }));

                this.OnSelectNodeChanged(this, value);
            } }

      

        private Color selectedNodeForeColor = Color.White;
        private int itemHeight = 60;
        private ICTreeNode selectedNode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        public Color NodeBackColor { get => nodeBackColor; set { nodeBackColor = value; this.Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        public Color NodeForeColor { get => nodeForeColor; set { nodeForeColor = value; this.Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)] 
        public Color SelectedNodeBackColor { get => selectedNodeBackColor; set { selectedNodeBackColor = value; this.Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)] 
        public Color SelectedNodeForeColor { get => selectedNodeForeColor; set { selectedNodeForeColor = value; this.Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        
        public int ItemHeight
        {
            get => itemHeight;
            set
            {
                this.DoInvoke(new Action(() =>
                {
                    itemHeight = value;
                }));
                this.Invalidate();
            }
        }

        public enum Layout {
            V, H
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        public Layout LayoutType { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        public CTreeNodeCollection Nodes { get => nodes;
            set
            {
                this.DoInvoke(new Action(() =>
                {
                    nodes = value;
                    value.ListChanged += NodesChange;

                }));
            } }

        private void NodesChange(object sender, ListChangedEventArgs e)
        {
            this.Invalidate();
        }

        [Browsable(true)]
        [Category("外观")]
        [Description("设置圆角半径（分别代表左上、右上、右下、左下），0 表示无圆角")]
        [CornerRadius("设置圆角半径（分别代表左上、右上、右下、左下），0 表示无圆角")]
        public Padding Radius { get => radius; set
            {
                radius = value;
                this.DoInvoke(new Action(() =>
                {
                    RadiusAngle = Radius;
                }));
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        public Point ClickAt { get; private set; }

        public CTreeMenu() {
            BackColor = Color.Gray;
            Radius = new Padding(5);
        }
       
        private void OnSelectNodeChanged(CTreeMenu cTreeMenu, ICTreeNode value)
        {
            SelectedNodeChanged?.Invoke(this, value);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            ClickAt = e.Location;
            this.Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            int count = nodes.Count;

            for (int i = 0; i < count; i++)
            {
                Rectangle rect;

                if (LayoutType == Layout.V)
                {
                    rect = GetVerticalSplitRect(i, count, this.ClientRectangle);
                }
                else
                {
                    rect = GetHorizontalSplitRect(i, count, this.ClientRectangle);
                }
                if (ClickAt != new Point()) {
                    //把相对窗体左上角的坐标ClickAt转换成相对控件内部的坐标clientpoint
                    Point clientpoint = this.ClientPointToControlPoint(ClickAt);

                    if (rect.Contains(clientpoint)) {
                        SelectedNode = nodes[i];
                        nodes[i].Selected = true;
                    }
                    else
                    {
                        nodes[i].Selected = false;
                    }
                }
                nodes[i].OnPaint(rect, g, NodeBackColor, NodeForeColor, SelectedNodeBackColor, SelectedNodeForeColor, Font);

            }
        }


        /// <summary>
        /// 纵向平分 ClientRectangle
        /// </summary>
        private Rectangle GetVerticalSplitRect(int index, int total, Rectangle client)
        {
            int height = ItemHeight;
            return new Rectangle(
                client.X,
                client.Y + index * height,
                client.Width,
                height
            );
        }

        /// <summary>
        /// 横向平分 ClientRectangle
        /// </summary>
        private Rectangle GetHorizontalSplitRect(int index, int total, Rectangle client)
        {
            int width = ItemHeight;
            return new Rectangle(
                client.X + index * width,
                client.Y,
                width,
                client.Height
            );
        }

        protected override void OnDockChanged(EventArgs e)
        {
            base.OnDockChanged(e);
            if (Dock == DockStyle.Left)
            {
                Radius = new Padding(0, 5, 5, 0);
            }
            else if (Dock == DockStyle.Right)
            {
                Radius = new Padding(5, 0, 0, 5);

            }
            else if (Dock == DockStyle.Top)
            {
                Radius = new Padding(0, 0, 5, 5);

            }
            else if (Dock == DockStyle.Bottom)
            {
                Radius = new Padding(5, 5, 0, 0);

            }
            else if (Dock == DockStyle.None)
            {
                Radius = new Padding(5, 5, 5, 5);

            }
            else if (Dock == DockStyle.Fill)
            {
                Radius = new Padding(5, 5, 5, 5);

            }
        }

        public ICTreeNode SelectNode(string text)
        {
            ICTreeNode t = null;
            DoInvoke(() =>
            {

                if (nodes == null)
                {
                    return ;
                }

                foreach (var item in nodes)
                {
                    if (item.Text == text)
                    {
                        item.Selected = true;
                        OnSelectNodeChanged(this, item);
                        t = item;
                        return ;
                    }
                    else
                    {
                        item.Selected = false;
                    }
                }
                
                return;
            });
            return t;
        }

        public void SelectNode(int selectedIndex)
        {
            DoInvoke(() => {
                if (Nodes == null || Nodes.Count <= selectedIndex || selectedIndex < 0)
                {
                    return;
                }
                Nodes[selectedIndex].Selected = true;
            });
        }
    }
}
