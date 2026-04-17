
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace UpperMachine.Controls
{
	/// <summary>
	/// 上位机首页/子菜单导航控件（稳定修复版）
	/// .NET Framework 4.8 / WinForms
	/// </summary>
	[DefaultEvent(nameof(ControlInitialized))]
	public class UpperMachineMenuControl : UserControl
	{
		#region Events

		/// <summary>
		/// 控件初始化完成事件（句柄创建完成后触发一次）
		/// </summary>
		public event EventHandler ControlInitialized;

		/// <summary>
		/// 菜单点击事件
		/// </summary>
		public event EventHandler<MenuNodeEventArgs> MenuClicked;

		/// <summary>
		/// 菜单切换事件
		/// </summary>
		public event EventHandler<MenuNodeEventArgs> MenuChanged;

		#endregion

		#region Fields

		private readonly Panel _breadcrumbPanel;
	//	private readonly Label _titleLabel;
		//private readonly Label _subTitleLabel;
		private readonly Panel _menuHostPanel;
		private readonly Panel _contentHostPanel;
		private readonly Panel _headerPanel;
		private readonly Panel _menuContainerPanel;

		private readonly Stack<MenuNode> _navigationStack = new Stack<MenuNode>();
		private List<MenuNode> _rootMenus = new List<MenuNode>();
		private bool _initializedRaised;
		private bool _layoutRefreshPending;
		private MenuNode _currentNode;

		#endregion

		#region Enum

		private enum MenuViewMode
		{
			Home,
			SubMenu,
			Content
		}

		#endregion

		#region Constructor

		public UpperMachineMenuControl()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.ResizeRedraw, true);

			DoubleBuffered = true;
			//BackColor = Color.FromArgb(240, 243, 248);
			BackColor = ThemeColors.PageBack;
			Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
			Padding = new Padding(14);
			MinimumSize = new Size(730, 250);

			_headerPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 72,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};

			_breadcrumbPanel = new Panel
			{
				Dock = DockStyle.Fill,
	//			Height = 50,
				BackColor = Color.Transparent,
				Padding = new Padding(0, 0, 0, 4)
			};

		
			_headerPanel.Controls.Add(_breadcrumbPanel);

			_menuContainerPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Height = 170,
				BackColor = Color.Transparent,
				Padding = new Padding(0, 6, 0, 10)
			};

			_menuHostPanel = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};

			_menuContainerPanel.Controls.Add(_menuHostPanel);

			_contentHostPanel = new Panel
			{
				Dock = DockStyle.Fill,
				//BackColor = Color.White,
				BackColor = ThemeColors.PanelBack,
				BorderStyle = BorderStyle.FixedSingle,
				Padding = new Padding(0),
				Visible = false
			};

			Controls.Add(_contentHostPanel);
			Controls.Add(_menuContainerPanel);
			Controls.Add(_headerPanel);
		}

		#endregion

		#region Public Properties

		[Browsable(true)]
		[Category("Appearance")]
		[Description("菜单按钮最小宽度")]
		[DefaultValue(220)]
		public int MenuButtonMinWidth { get; set; } = 220;

		[Browsable(true)]
		[Category("Appearance")]
		[Description("菜单按钮最小高度")]
		[DefaultValue(72)]
		public int MenuButtonHeight { get; set; } = 72;

		[Browsable(true)]
		[Category("Appearance")]
		[Description("首页菜单列数，建议 2")]
		[DefaultValue(2)]
		public int MenuColumns { get; set; } = 2;

		[Browsable(false)]
		public MenuNode CurrentNode => _currentNode;

		[Browsable(false)]
		public Control ContentView => _contentHostPanel.Controls.Count > 0 ? _contentHostPanel.Controls[0] : null;

		#endregion

		#region Lifecycle

		protected override void OnCreateControl()
		{
			base.OnCreateControl();
			RequestRefreshLayout();
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (!_initializedRaised)
			{
				_initializedRaised = true;
				ControlInitialized?.Invoke(this, EventArgs.Empty);
			}

			RequestRefreshLayout();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (Visible)
			{
				RequestRefreshLayout();
			}
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (IsDisposed || _menuHostPanel == null || _breadcrumbPanel == null)
				return;

			if (_rootMenus == null)
				return;

			// 尺寸过小时跳过，避免布局抖动或出现“看起来没加载”
			if (Width < 200 || Height < 120)
				return;

			RequestRefreshLayout();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// 初始化根菜单
		/// </summary>
		public void InitializeMenus(IEnumerable<MenuNode> menus)
		{
			_rootMenus = menus?.ToList() ?? new List<MenuNode>();

			foreach (var menu in _rootMenus)
			{
				BindParent(menu, null);
			}

			_navigationStack.Clear();
			_currentNode = null;
			_contentHostPanel.Controls.Clear();

			//_titleLabel.Text = "首页菜单";
			///_subTitleLabel.Text = "请选择一个功能模块";

			RequestRefreshLayout();
		}

		/// <summary>
		/// 丢一个业务控件进内容区，并自动 Dock.Fill
		/// </summary>
		public void FillControl(Control control)
		{
			_contentHostPanel.SuspendLayout();
			try
			{
				_contentHostPanel.Controls.Clear();

				if (control != null)
				{
					control.Dock = DockStyle.Fill;
					_contentHostPanel.Controls.Add(control);
				}
			}
			finally
			{
				_contentHostPanel.ResumeLayout();
			}

			UpdateViewState(MenuViewMode.Content);
		}

		/// <summary>
		/// 外部直接丢菜单进去
		/// </summary>
		public void FillMenus(IEnumerable<MenuNode> menus)
		{
			InitializeMenus(menus);
		}

		/// <summary>
		/// 返回首页
		/// </summary>
		public void BackHome()
		{
			_navigationStack.Clear();
			_currentNode = null;
			_contentHostPanel.Controls.Clear();

		//	//_titleLabel.Text = "首页菜单";
		//	_subTitleLabel.Text = "请选择一个功能模块";

			RequestRefreshLayout();
		}

		/// <summary>
		/// 进入指定菜单节点
		/// </summary>
		public void NavigateTo(MenuNode node)
		{
			if (node == null) return;

			_currentNode = node;
			_navigationStack.Clear();

			var path = new Stack<MenuNode>();
			var temp = node;
			while (temp != null)
			{
				path.Push(temp);
				temp = temp.Parent;
			}

			while (path.Count > 0)
			{
				_navigationStack.Push(path.Pop());
			}

			RenderBreadcrumb();

			if (node.Children != null && node.Children.Count > 0)
			{
				//_titleLabel.Text = node.Text;
			//	_subTitleLabel.Text = "请选择子功能";

				_contentHostPanel.Controls.Clear();

				AdjustMenuContainerHeight();
				RenderMenuButtons(node.Children, false);
				UpdateViewState(MenuViewMode.SubMenu);
			}
			else
			{
			//	_titleLabel.Text = node.Text;
			//	_subTitleLabel.Text = "当前功能页面";

				Control view = null;
				if (node.ViewFactory != null)
				{
					view = node.ViewFactory.Invoke();
				}

				FillControl(view);
				UpdateViewState(MenuViewMode.Content);
			}

			MenuChanged?.Invoke(this, new MenuNodeEventArgs(node));
		}

		#endregion

		#region Rendering

		private void RequestRefreshLayout()
		{
			if (IsDisposed || !IsHandleCreated)
				return;

			if (_layoutRefreshPending)
				return;

			_layoutRefreshPending = true;

			BeginInvoke((Action)(() =>
			{
				_layoutRefreshPending = false;

				if (IsDisposed || !IsHandleCreated)
					return;

				if (!Visible)
					return;

				if (Width < 200 || Height < 120)
					return;

				RefreshLayoutCore();
			}));
		}

		private void RefreshLayoutCore()
		{
			AdjustMenuContainerHeight();
			RenderBreadcrumb();

			if (_currentNode == null)
			{
				RenderMenuButtons(_rootMenus, true);
				UpdateViewState(MenuViewMode.Home);
			}
			else if (_currentNode.Children != null && _currentNode.Children.Count > 0)
			{
				RenderMenuButtons(_currentNode.Children, false);
				UpdateViewState(MenuViewMode.SubMenu);
			}
			else
			{
				UpdateViewState(MenuViewMode.Content);
			}
		}

		private void AdjustMenuContainerHeight()
		{
			int available = Height - _headerPanel.Height - Padding.Top - Padding.Bottom - 10;
			available = Math.Max(100, available);

			int target = Math.Min(Math.Max(150, available / 2), available);
			_menuContainerPanel.Height = target;
		}

		private void UpdateViewState(MenuViewMode mode)
		{
			switch (mode)
			{
				case MenuViewMode.Home:
					_menuContainerPanel.Visible = true;
					_contentHostPanel.Visible = false;
					break;

				case MenuViewMode.SubMenu:
					_menuContainerPanel.Visible = true;
					_contentHostPanel.Visible = false;
					break;

				case MenuViewMode.Content:
					_menuContainerPanel.Visible = false;
					_contentHostPanel.Visible = true;
					break;
			}
		}

		//private void RenderBreadcrumb()
		//{
		//	if (_breadcrumbPanel == null || IsDisposed)
		//		return;

		//	_breadcrumbPanel.SuspendLayout();
		//	try
		//	{
		//		_breadcrumbPanel.Controls.Clear();

		//		var flow = new FlowLayoutPanel
		//		{
		//			Dock = DockStyle.Fill,
		//			FlowDirection = FlowDirection.LeftToRight,
		//			WrapContents = false,
		//			AutoScroll = true,
		//			BackColor = Color.Transparent,
		//			Margin = new Padding(0),
		//			Padding = new Padding(0)
		//		};

		//		flow.Controls.Add(CreateBreadcrumbButton("首页", null, _currentNode == null));

		//		if (_navigationStack.Count > 0)
		//		{
		//			var path = _navigationStack.Reverse().ToList();
		//			foreach (var node in path)
		//			{
		//				flow.Controls.Add(CreateSeparatorLabel());
		//				var isLast = node == path.Last();
		//				flow.Controls.Add(CreateBreadcrumbButton(node.Text, node, isLast));
		//			}
		//		}

		//		_breadcrumbPanel.Controls.Add(flow);
		//	}
		//	finally
		//	{
		//		_breadcrumbPanel.ResumeLayout();
		//	}
		//}
		private void RenderBreadcrumb()
		{
			if (_breadcrumbPanel == null || IsDisposed)
				return;

			_breadcrumbPanel.SuspendLayout();
			try
			{
				_breadcrumbPanel.Controls.Clear();

				// 右侧按钮容器
				var rightPanel = new Panel
				{
					Dock = DockStyle.Right,
					Width = 90,
					BackColor = Color.Transparent,
					Margin = new Padding(0),
					Padding = new Padding(0)
				};

				// 返回按钮
				var backButton = new Button
				{
					Text = "返回",
					Width = 82,
					Height = 48,
					Anchor = AnchorStyles.Top | AnchorStyles.Right,
					Location = new Point(rightPanel.Width - 82, 0),
					FlatStyle = FlatStyle.Flat,
					BackColor = ThemeColors.PureWhite,
					ForeColor = ThemeColors.TextPrimary,
					Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
					Cursor = Cursors.Hand,
					Margin = new Padding(0)
				};

				backButton.FlatAppearance.BorderSize = 1;
				backButton.FlatAppearance.BorderColor = ThemeColors.Border;
				backButton.FlatAppearance.MouseOverBackColor = ThemeColors.BreadcrumbHover;
				backButton.FlatAppearance.MouseDownBackColor = ThemeColors.BreadcrumbDown;

				backButton.Click += (s, e) =>
				{
					// 这里你可以按需要改成返回上一级，而不是回首页
					BackHome();
				};

				rightPanel.Controls.Add(backButton);

				// 左侧面包屑流式布局
				var flow = new FlowLayoutPanel
				{
					Dock = DockStyle.Fill,
					FlowDirection = FlowDirection.LeftToRight,
					WrapContents = false,
					AutoScroll = true,
					BackColor = Color.Transparent,
					Margin = new Padding(0),
					Padding = new Padding(0, 2, 0, 0)
				};

				flow.Controls.Add(CreateBreadcrumbButton("首页", null, _currentNode == null));

				if (_navigationStack.Count > 0)
				{
					var path = _navigationStack.Reverse().ToList();
					foreach (var node in path)
					{
						flow.Controls.Add(CreateSeparatorLabel());
						var isLast = node == path.Last();
						flow.Controls.Add(CreateBreadcrumbButton(node.Text, node, isLast));
					}
				}

				// 注意添加顺序：先加 right，再加 fill
				_breadcrumbPanel.Controls.Add(flow);
				_breadcrumbPanel.Controls.Add(rightPanel);
			}
			finally
			{
				_breadcrumbPanel.ResumeLayout();
			}
		}

		private Control CreateBreadcrumbButton(string text, MenuNode node, bool isCurrent)
		{
			var btn = new Button
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Height = 26,
				Padding = new Padding(10, 2, 10, 2),
				Margin = new Padding(0),
				FlatStyle = FlatStyle.Flat,
				Text = text,
				//BackColor = isCurrent ? Color.FromArgb(225, 232, 242) : Color.White,
				//ForeColor = isCurrent ? Color.FromArgb(32, 40, 52) : Color.FromArgb(98, 108, 120),
				BackColor = isCurrent ? ThemeColors.BreadcrumbCurrent : ThemeColors.PureWhite,
				ForeColor = isCurrent ? ThemeColors.TextPrimary : ThemeColors.TextSecondary,
				Font = new Font("Microsoft YaHei UI", 9F, isCurrent ? FontStyle.Bold : FontStyle.Regular),
				Cursor = Cursors.Hand
			};

			btn.FlatAppearance.BorderSize = 1;
			//btn.FlatAppearance.BorderColor = isCurrent
			//	? Color.FromArgb(189, 202, 221)
			//	: Color.FromArgb(220, 226, 234);
			btn.FlatAppearance.BorderColor = isCurrent
	? ThemeColors.BorderActive
	: ThemeColors.Border;

			//btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 248, 252);
			//btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 240, 247);
			btn.FlatAppearance.MouseOverBackColor = ThemeColors.BreadcrumbHover;
			btn.FlatAppearance.MouseDownBackColor = ThemeColors.BreadcrumbDown;

			btn.Click += (s, e) =>
			{
				if (node == null)
				{
					BackHome();
					return;
				}

				if (node.Children != null && node.Children.Count > 0)
				{
					NavigateTo(node);
				}
				else if (node.ViewFactory != null)
				{
					var view = node.ViewFactory.Invoke();
					FillControl(view);

					_currentNode = node;
					SyncNavigationStack(node);
					RenderBreadcrumb();

				//	_titleLabel.Text = node.Text;
				//	_subTitleLabel.Text = "当前功能页面";

					UpdateViewState(MenuViewMode.Content);
					MenuChanged?.Invoke(this, new MenuNodeEventArgs(node));
				}
			};

			return btn;
		}

		private Control CreateSeparatorLabel()
		{
			return new Label
			{
				AutoSize = true,
				Margin = new Padding(8, 5, 8, 0),
				Text = ">",
				//ForeColor = Color.FromArgb(160, 168, 178),
				ForeColor = ThemeColors.TextMuted,
				Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
			};
		}

		private void RenderMenuButtons(IList<MenuNode> menus, bool isRoot)
		{
			_menuHostPanel.SuspendLayout();
			try
			{
				_menuHostPanel.Controls.Clear();

				var layout = new TableLayoutPanel
				{
					Dock = DockStyle.Fill,
					BackColor = Color.Transparent,
					Margin = new Padding(0),
					Padding = new Padding(0)
				};

				var sourceMenus = menus ?? new List<MenuNode>();
				var buttonItems = new List<MenuNode>();

				if (!isRoot)
				{
					buttonItems.Add(new MenuNode { Text = "返回首页", Tag = "__back_home__" });
				}

				buttonItems.AddRange(sourceMenus);

				int columnCount = GetBestColumnCount(buttonItems.Count);
				layout.ColumnCount = columnCount;

				for (int i = 0; i < columnCount; i++)
				{
					layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnCount));
				}

				int rowCount = (int)Math.Ceiling(buttonItems.Count / (double)columnCount);
				rowCount = Math.Max(rowCount, 1);
				layout.RowCount = rowCount;

				int rowHeight = GetBestButtonHeight(rowCount);
				for (int i = 0; i < rowCount; i++)
				{
					if (i == 0)
					{
					//	layout.RowStyles.Add(new RowStyle(SizeType.AutoSize,1 ));

					}
					layout.RowStyles.Add(new RowStyle(SizeType.Percent, rowHeight));
				}

				for (int index = 0; index < buttonItems.Count; index++)
				{
					var node = buttonItems[index];
					int row = index / columnCount;
					int col = index % columnCount;

					var btn = CreateMenuButton(node);
					layout.Controls.Add(btn, col, row);
				}
				layout.Dock = DockStyle.Fill;
				_menuHostPanel.Controls.Add(layout);
			}
			finally
			{
				_menuHostPanel.ResumeLayout();
			}
		}

		private int GetBestColumnCount(int itemCount)
		{
			int availableWidth = Math.Max(Width - Padding.Left - Padding.Right - 20, 300);

			if (availableWidth < 760)
				return 2;

			if (itemCount <= 2)
				return 2;

			return availableWidth > 1100 ? 3 : 2;
		}

		private int GetBestButtonHeight(int rowCount)
		{
			int usableHeight = Math.Max(_menuContainerPanel.Height - 18, 4);
			int h = usableHeight / Math.Max(rowCount, 1);

			h = Math.Min(h, 96);
			h = Math.Max(h, 50);

			return h;
		}

		private Control CreateMenuButton(MenuNode node)
		{
			bool isBack = node.Tag != null && node.Tag.ToString() == "__back_home__";

			var btn = new MenuCardButton
			{
				Dock = DockStyle.Fill,
				Margin = new Padding(8),
				Text = node.Text,
				Subtitle = isBack
					? "Return to home page"
					: (node.Children != null && node.Children.Count > 0 ? "Open submenu" : "Open function page"),
				IsBackButton = isBack,
				Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 134),
				Cursor = Cursors.Hand
			};

			btn.Click += (s, e) =>
			{
				if (node.Premission != null)
				{
					if (node.Premission?.Invoke() == false)
					{
						return;
					}
				}
				if (isBack)
				{
					BackHome();
					return;
				}

				MenuClicked?.Invoke(this, new MenuNodeEventArgs(node));

				if (node.Children != null && node.Children.Count > 0)
				{
					NavigateTo(node);
				}
				else if (node.ViewFactory != null)
				{
					
					var view = node.ViewFactory.Invoke();
					FillControl(view);

					_currentNode = node;
					SyncNavigationStack(node);
					RenderBreadcrumb();

				//	_titleLabel.Text = node.Text;
				//	_subTitleLabel.Text = "当前功能页面";

					UpdateViewState(MenuViewMode.Content);
					MenuChanged?.Invoke(this, new MenuNodeEventArgs(node));
				}
			};

			return btn;
		}

		#endregion

		#region Helpers

		private void BindParent(MenuNode node, MenuNode parent)
		{
			if (node == null) return;

			node.Parent = parent;
			if (node.Children == null) return;

			foreach (var child in node.Children)
			{
				BindParent(child, node);
			}
		}

		private void SyncNavigationStack(MenuNode node)
		{
			_navigationStack.Clear();

			var path = new Stack<MenuNode>();
			var current = node;

			while (current != null)
			{
				path.Push(current);
				current = current.Parent;
			}

			while (path.Count > 0)
			{
				_navigationStack.Push(path.Pop());
			}
		}

		#endregion
	}

	/// <summary>
	/// 大卡片菜单按钮
	/// </summary>
	internal class MenuCardButton : Control
	{
		private bool _hover;
		private bool _pressed;

		public string Subtitle { get; set; }
		public bool IsBackButton { get; set; }

		public MenuCardButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.ResizeRedraw |
					 ControlStyles.SupportsTransparentBackColor, true);

			Size = new Size(220, 76);
			BackColor = Color.Transparent;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_hover = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hover = false;
			_pressed = false;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left)
			{
				_pressed = true;
				Invalidate();
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			_pressed = false;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			var rect = new Rectangle(1, 1, Width - 3, Height - 3);
			int radius = 12;

			Color backColor;
			Color borderColor;
			Color titleColor;
			Color subColor;
			Color badgeColor;

			//if (IsBackButton)
			//{
			//	backColor = _pressed
			//		? Color.FromArgb(228, 235, 244)
			//		: _hover ? Color.FromArgb(240, 245, 251) : Color.FromArgb(246, 248, 251);
			//	borderColor = Color.FromArgb(206, 216, 228);
			//	titleColor = Color.FromArgb(34, 42, 52);
			//	subColor = Color.FromArgb(116, 124, 136);
			//	badgeColor = Color.FromArgb(208, 221, 238);
			//}
			//else
			//{
			//	backColor = _pressed
			//		? Color.FromArgb(222, 234, 248)
			//		: _hover ? Color.FromArgb(235, 243, 252) : Color.FromArgb(248, 251, 255);
			//	borderColor = _hover
			//		? Color.FromArgb(135, 170, 215)
			//		: Color.FromArgb(214, 224, 236);
			//	titleColor = Color.FromArgb(26, 36, 48);
			//	subColor = Color.FromArgb(112, 122, 134);
			//	badgeColor = Color.FromArgb(197, 222, 247);
			//}
			if (IsBackButton)
			{
				backColor = _pressed
					? ThemeColors.CardBackPressed
					: _hover ? ThemeColors.CardBackHover : ThemeColors.CardBackBack;

				borderColor = ThemeColors.Border;
				titleColor = ThemeColors.TextPrimary;
				subColor = ThemeColors.TextSecondary;
				badgeColor = ThemeColors.Badge;
			}
			else
			{
				backColor = _pressed
					? ThemeColors.CardPressed
					: _hover ? ThemeColors.CardHover : ThemeColors.CardBack;

				borderColor = _hover
					? ThemeColors.BorderHover
					: ThemeColors.Border;

				titleColor = ThemeColors.TextPrimary;
				subColor = ThemeColors.TextSecondary;
				badgeColor = ThemeColors.Badge;
			}

			using (var path = CreateRoundedRectangle(rect, radius))
			using (var brush = new SolidBrush(backColor))
			using (var pen = new Pen(borderColor, 1f))
			{
				g.FillPath(brush, path);
				g.DrawPath(pen, path);
			}

			var accentRect = new Rectangle(rect.X + 12, rect.Y + 14, 6, rect.Height - 28);
			using (var accentPath = CreateRoundedRectangle(accentRect, 3))
			//using (var accentBrush = new SolidBrush(IsBackButton
			//	? Color.FromArgb(162, 179, 198)
			//	: Color.FromArgb(87, 140, 201)))
			using (var accentBrush = new SolidBrush(IsBackButton
			? ThemeColors.AccentLight
			: ThemeColors.Accent))
			{
				g.FillPath(accentBrush, accentPath);
			}

			var badgeRect = new Rectangle(rect.Right - 26, rect.Y + 12, 10, 10);
			using (var badgeBrush = new SolidBrush(badgeColor))
			{
				g.FillEllipse(badgeBrush, badgeRect);
			}

			var textLeft = rect.X + 28;
			var titleRect = new Rectangle(textLeft, rect.Y + 12, rect.Width - 56, 24);
			var subRect = new Rectangle(textLeft, rect.Y + 36, rect.Width - 56, 22);

			using (var sf = new StringFormat
			{
				Alignment = StringAlignment.Near,
				LineAlignment = StringAlignment.Center,
				Trimming = StringTrimming.EllipsisCharacter,
				FormatFlags = StringFormatFlags.NoWrap
			})
			{
				using (var titleBrush = new SolidBrush(titleColor))
				{
					g.DrawString(Text, Font, titleBrush, titleRect, sf);
				}

				using (var subBrush = new SolidBrush(subColor))
				using (var subFont = new Font("Segoe UI", 8.8f, FontStyle.Regular))
				{
					g.DrawString(Subtitle ?? "", subFont, subBrush, subRect, sf);
				}
			}
		}

		private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
		{
			var path = new GraphicsPath();
			int d = radius * 2;

			path.AddArc(rect.X, rect.Y, d, d, 180, 90);
			path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
			path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
			path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
			path.CloseFigure();

			return path;
		}
	}
	#region Theme Colors (Gray Theme)

	
	#endregion

	public class MenuNode
	{
		/// <summary>
		/// 菜单显示文本
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// 子菜单
		/// </summary>
		public List<MenuNode> Children { get; set; } = new List<MenuNode>();

		/// <summary>
		/// 菜单附带数据
		/// </summary>
		public object Tag { get; set; }

		/// <summary>
		/// 叶子节点对应的业务界面工厂
		/// </summary>
		public Func<Control> ViewFactory { get; set; }

		/// <summary>
		/// 父节点（内部绑定）
		/// </summary>
		[Browsable(false)]
		public MenuNode Parent { get; internal set; }
		public Func<bool> Premission { get;  set; }

		public override string ToString()
		{
			return Text ?? base.ToString();
		}
	}

	public class MenuNodeEventArgs : EventArgs
	{
		public MenuNodeEventArgs(MenuNode node)
		{
			Node = node;
		}

		public MenuNode Node { get; }
	}
}