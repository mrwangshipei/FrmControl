
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

public class TouchListBox : ListBox
{
	private bool isDragging;
	private int lastY;
	private int scrollRemainder;
	private bool hasMoved;
	private const int DragThreshold = 4;

	public bool EnableTouchScroll { get; set; } = true;

	public TouchListBox()
	{
		BorderStyle = BorderStyle.None;
		IntegralHeight = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);

		if (!EnableTouchScroll)
			return;

		isDragging = true;
		hasMoved = false;
		lastY = e.Y;
		scrollRemainder = 0;
		Capture = true;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);

		if (!EnableTouchScroll || !isDragging)
			return;

		int deltaY = e.Y - lastY;

		if (Math.Abs(deltaY) >= DragThreshold)
			hasMoved = true;

		lastY = e.Y;

		if (Items.Count == 0 || ItemHeight <= 0)
			return;

		scrollRemainder += deltaY;

		int step = scrollRemainder / ItemHeight;
		if (step == 0)
			return;

		int newTopIndex = TopIndex - step;

		if (newTopIndex < 0)
			newTopIndex = 0;

		if (newTopIndex > Items.Count - 1)
			newTopIndex = Items.Count - 1;

		if (newTopIndex != TopIndex)
			TopIndex = newTopIndex;

		scrollRemainder %= ItemHeight;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		bool moved = hasMoved;

		base.OnMouseUp(e);

		if (!EnableTouchScroll)
			return;

		isDragging = false;
		Capture = false;

		if (moved)
		{
			// 拖拽结束时尽量不把它当作点击选中
			return;
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);

		if (MouseButtons == MouseButtons.None)
		{
			isDragging = false;
			Capture = false;
		}
	}
}
[DefaultEvent("SelectedIndexChanged")]
public class CFluentComboBox : Control
{
	#region Fields

	private TextBox textBox;
	private Button btn;
	private int buttonSize = 54;
	public int ButtonSize
	{
		get => buttonSize;
		set
		{
			buttonSize = value;
			if (btn != null)
			{
				if (Height == 0)
				{
					return;
				}
				int size = Math.Min(buttonSize, Math.Max(8, Height - 8));
				btn.Size = new Size(size, size);
				UpdateLayout(); // ⭐关键
			}
		}
	}

	private ShadowDropDownForm dropdown;
	private TouchListBox listBox;

	private readonly List<object> itemsSource = new List<object>();
	private List<object> filteredItems = new List<object>();

	private int selectedIndex = -1;
	private bool isFiltering;
	private bool isInitialized;
	private bool suppressTextChanged;

	private bool isHovered;
	private bool isFocused;
	private DropDownDirection currentDropDownDirection = DropDownDirection.Down;

	#endregion

	#region Constructor

	public CFluentComboBox()
	{


		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.OptimizedDoubleBuffer |
			ControlStyles.ResizeRedraw |
			ControlStyles.UserPaint,
			true);

		InitUI();
		RefreshFilter();
	}

	#endregion

	#region Public Properties
	[Browsable(false)]
	public DropDownDirection CurrentDropDownDirection => currentDropDownDirection;

	[Browsable(true)]
	[Category("Data")]
	[Description("用于显示文本的属性名，例如 Name。")]
	public string DisplayMember { get; set; }

	[Browsable(true)]
	[Category("Data")]
	[Description("用于取值的属性名，例如 Id。")]
	public string ValueMember { get; set; }

	[Browsable(false)]
	public object SelectedItem
	{
		get
		{
			if (selectedIndex < 0 || selectedIndex >= itemsSource.Count)
				return null;

			return itemsSource[selectedIndex];
		}
	}

	[Browsable(false)]
	public object SelectedValue
	{
		get
		{
			var item = SelectedItem;
			if (item == null) return null;
			return GetValueMemberValue(item);
		}
	}

	[Browsable(true)]
	[Category("Behavior")]
	[DefaultValue(-1)]
	public int SelectedIndex
	{
		get => selectedIndex;
		set
		{
			if (value < -1 || value >= itemsSource.Count)
				return;

			if (selectedIndex == value)
				return;

			selectedIndex = value;
			SyncTextFromSelectedItem();
			SyncListBoxSelectionFromSelectedIndex();

			OnSelectedIndexChanged(EventArgs.Empty);
			OnSelectedValueChanged(EventArgs.Empty);
		}
	}
	[Browsable(true)]
	[Category("Appearance")]
	[DefaultValue(32)]
	[Description("下拉项高度。")]
	public int ItemHeight
	{
		get => itemHeight;
		set
		{
			if (value < 18)
				value = 18;

			if (itemHeight == value)
				return;

			itemHeight = value;

			if (listBox != null)
				listBox.ItemHeight = itemHeight;

			if (dropdown != null && dropdown.Visible)
				AdjustDropDownBounds();
		}
	}
	private int itemHeight = 32;


	[Browsable(false)]
	public string SelectedText => GetItemDisplayText(SelectedItem);

	[Browsable(false)]
	public IReadOnlyList<object> ItemsSource => itemsSource;

	#endregion

	#region Events

	public event EventHandler SelectedIndexChanged;
	public event EventHandler SelectedValueChanged;

	protected virtual void OnSelectedIndexChanged(EventArgs e)
	{
		Text = SelectedText.ToString();
		SelectedIndexChanged?.Invoke(this, e);
	}

	protected virtual void OnSelectedValueChanged(EventArgs e)
	{
		SelectedValueChanged?.Invoke(this, e);
	}

	#endregion

	#region Init UI
	private void UpdateLayout()
	{
		if (textBox == null || btn == null) return;

		int leftPadding = 12;
		int gap = 6;
		int rightPadding = 6;

		int buttonAreaWidth = btn.Width + gap + rightPadding;

		// TextBox 位置 & 宽度
		textBox.Location = new Point(
			leftPadding,
			Math.Max(8, (Height - textBox.Height) / 2)
		);

		textBox.Width = Math.Max(20, Width - leftPadding - buttonAreaWidth);

		// Button 位置
		btn.Location = new Point(
			Width - rightPadding - btn.Width,
			(Height - btn.Height) / 2
		);
	}

	private void InitUI()
	{
		textBox = new TextBox
		{
			BorderStyle = BorderStyle.None,
			Location = new Point(12, 10),
			Width = Width - 40,
			Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
		};
		TextChanged += TextBox_TextChanged;
		textBox.TextChanged += TextBox_TextChanged;
		textBox.KeyDown += TextBox_KeyDown;
		textBox.GotFocus += (s, e) =>
		{
			isFocused = true;
			Invalidate();
		};
		textBox.LostFocus += (s, e) =>
		{
			isFocused = false;
			Invalidate();
		};

		Controls.Add(textBox);

		btn = new Button
		{
			Text = "▼",
			FlatStyle = FlatStyle.Flat,
			Width = buttonSize,
			Height = buttonSize,
			Anchor = AnchorStyles.Top | AnchorStyles.Right,
			TabStop = false
		};

		btn.FlatAppearance.BorderSize = 0;
		btn.BackColor = Color.White;
		btn.Click += (s, e) => ToggleDropdown();
		btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
		btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 235, 235);

		Controls.Add(btn);

		MouseEnter += (s, e) =>
		{
			isHovered = true;
			Invalidate();
		};
		MouseLeave += (s, e) =>
		{
			isHovered = false;
			Invalidate();
		};
		UpdateLayout();
		isInitialized = true;

	}
	private void UpdateButtonLayout()
	{
		UpdateLayout();
	}

	#endregion

	#region Data Binding

	public void SetDataSource(IEnumerable data)
	{
		itemsSource.Clear();

		if (data != null)
		{
			foreach (var item in data)
			{
				itemsSource.Add(item);
			}
		}

		if (itemsSource.Count == 0)
		{
			selectedIndex = -1;
		}
		else if (selectedIndex >= itemsSource.Count)
		{
			selectedIndex = 0;
		}

		RefreshFilter();
		SyncTextFromSelectedItem();
		SyncListBoxSelectionFromSelectedIndex();
	}

	public void ClearDataSource()
	{
		SetDataSource(null);
	}

	#endregion

	#region Dropdown
	private void AdjustDropDownBounds()
	{
		if (dropdown == null)
			return;

		int popupHeight;
		Point popupLocation;

		CalculateDropDownBounds(out popupLocation, out popupHeight);

		if (popupHeight <= 0)
			return;

		dropdown.Height = popupHeight;
		dropdown.Location = popupLocation;
	}



	private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
	{
		e.DrawBackground();

		if (e.Index < 0 || e.Index >= filteredItems.Count)
			return;

		var item = filteredItems[e.Index];
		string text = GetItemDisplayText(item);

		bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

		bool isMatched = string.IsNullOrWhiteSpace(currentKeyword) ||
			text.IndexOf(currentKeyword, StringComparison.OrdinalIgnoreCase) >= 0;

		using (var backBrush = new SolidBrush(
			isSelected ? Color.FromArgb(230, 240, 255) : Color.White))
		{
			e.Graphics.FillRectangle(backBrush, e.Bounds);
		}

		var textRect = new Rectangle(
			e.Bounds.X + 10,
			e.Bounds.Y,
			e.Bounds.Width - 20,
			e.Bounds.Height);

		Color textColor = isMatched ? Color.Black : Color.Gray;

		TextRenderer.DrawText(
			e.Graphics,
			text,
			e.Font,
			textRect,
			textColor,
			TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

		e.DrawFocusRectangle();
	}

	private void EnsureDropdown()
	{
		if (dropdown != null) return;

		dropdown = new ShadowDropDownForm
		{
			Padding = new Padding(6)
		};

		listBox = new TouchListBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.None,
			IntegralHeight = false,
			Font = Font,
			DrawMode = DrawMode.OwnerDrawFixed,
			ItemHeight = itemHeight,
			EnableTouchScroll = true
		};

		listBox.DrawItem += ListBox_DrawItem;
		listBox.Click += (s, e) => CommitSelection();
		listBox.DoubleClick += (s, e) => CommitSelection();

		dropdown.Controls.Add(listBox);
		dropdown.Deactivate += (s, e) => HideDropdown();
	}

	private void ToggleDropdown()
	{
		if (dropdown != null && dropdown.Visible)
			HideDropdown();
		else
			ShowDropdown();
	}

	private void ShowDropdown()
	{
		if (!UIReady) return;
		if (itemsSource.Count == 0) return;

		EnsureDropdown();
		RefreshFilter(textBox.Text);

		if (filteredItems.Count == 0)
			return;

		int popupHeight;
		Point popupLocation;

		CalculateDropDownBounds(out popupLocation, out popupHeight);

		if (popupHeight <= 0)
			return;

		dropdown.Width = Width;
		dropdown.Height = popupHeight;
		dropdown.Location = popupLocation;

		if (!dropdown.Visible)
			dropdown.Show();

		listBox.Focus();
	}

	private int maxDropDownHeight = 220;
	private RectangleF lastbound;

	[Browsable(true)]
	[Category("Appearance")]
	[DefaultValue(220)]
	[Description("下拉框最大高度。")]
	public int MaxDropDownHeight
	{
		get => maxDropDownHeight;
		set
		{
			if (value < itemHeight + 12)
				value = itemHeight + 12;

			maxDropDownHeight = value;

			if (dropdown != null && dropdown.Visible)
				AdjustDropDownBounds();
		}
	}

	private void HideDropdown()
	{
		dropdown?.Hide();
	}

	#endregion

	#region Filtering

	private void RefreshFilter(string keyword = null)
	{
		if (isFiltering) return;
		currentKeyword = keyword ?? string.Empty;

		isFiltering = true;
		try
		{
			if (string.IsNullOrWhiteSpace(keyword))
			{
				filteredItems = itemsSource.ToList();
			}
			else
			{
				var matchedItems = itemsSource
					.Where(x => GetItemDisplayText(x)
						.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
					.ToList();

				var unmatchedItems = itemsSource
					.Where(x => GetItemDisplayText(x)
						.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
					.ToList();

				filteredItems = matchedItems
					.Concat(unmatchedItems)
					.ToList();
			}

			RebindListBox();
		}
		finally
		{
			isFiltering = false;
		}
	}
	private string currentKeyword = string.Empty;

	private void RebindListBox()
	{
		if (listBox == null) return;

		listBox.BeginUpdate();
		try
		{
			listBox.DataSource = null;
			listBox.DisplayMember = string.Empty;
			listBox.ValueMember = string.Empty;
			listBox.ItemHeight = itemHeight;
			listBox.DataSource = filteredItems;

			if (!string.IsNullOrWhiteSpace(DisplayMember))
				listBox.DisplayMember = DisplayMember;

			if (!string.IsNullOrWhiteSpace(ValueMember))
				listBox.ValueMember = ValueMember;

			if (filteredItems.Count > 0)
				listBox.SelectedIndex = 0;
		}
		finally
		{
			listBox.EndUpdate();
		}
	}

	#endregion

	#region Text / Selection Sync

	private bool UIReady => isInitialized && textBox != null && btn != null;

	private void SyncTextFromSelectedItem()
	{
		if (!UIReady) return;

		var text = GetItemDisplayText(SelectedItem);

		suppressTextChanged = true;
		try
		{
			textBox.Text = text ?? string.Empty;
			textBox.SelectionStart = textBox.TextLength;
		}
		finally
		{
			suppressTextChanged = false;
		}
	}
	private void SyncSelectedIndexFromText(string text)
	{
		if (itemsSource == null || itemsSource.Count == 0)
		{
			if (selectedIndex != -1)
				SelectedIndex = -1;
			return;
		}

		if (string.IsNullOrWhiteSpace(text))
		{
			SelectedIndex = -1;
			return;
		}

		// 1. 先做精确匹配
		int exactIndex = itemsSource.FindIndex(x =>
			string.Equals(GetItemDisplayText(x), text, StringComparison.OrdinalIgnoreCase));

		if (exactIndex >= 0)
		{
			SelectedIndex = exactIndex;
			return;
		}

		// 2. 再做包含匹配（可选）
		int fuzzyIndex = itemsSource.FindIndex(x =>
			GetItemDisplayText(x)?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

		if (fuzzyIndex >= 0)
		{
			SelectedIndex = fuzzyIndex;
			return;
		}

		// 3. 都找不到就清空选中
		SelectedIndex = -1;
	}

	private void SyncListBoxSelectionFromSelectedIndex()
	{
		if (listBox == null) return;
		if (selectedIndex < 0 || selectedIndex >= itemsSource.Count) return;

		var selected = itemsSource[selectedIndex];
		var indexInFiltered = filteredItems.IndexOf(selected);
		if (indexInFiltered >= 0 && indexInFiltered < listBox.Items.Count)
		{
			listBox.SelectedIndex = indexInFiltered;
		}
	}

	private void CommitSelection()
	{
		if (listBox == null || listBox.SelectedItem == null)
			return;

		var selectedObject = listBox.SelectedItem;
		var realIndex = itemsSource.IndexOf(selectedObject);
		if (realIndex < 0)
			return;

		SelectedIndex = realIndex;
		HideDropdown();

		if (UIReady)
			textBox.Focus();
	}

	#endregion

	#region Reflection Helpers
	private void CalculateDropDownBounds(out Point location, out int height)
	{
		location = Point.Empty;
		height = 0;

		if (Parent == null)
			return;

		var screenLocation = Parent.PointToScreen(Location);
		var controlBounds = new Rectangle(screenLocation, Size);

		var screen = Screen.FromControl(this);
		var workingArea = screen.WorkingArea;

		int desiredHeight = Math.Min(
			(filteredItems?.Count ?? 0) * itemHeight + 12,
			maxDropDownHeight);

		if (desiredHeight <= 0)
			desiredHeight = itemHeight + 12;

		int spaceBelow = workingArea.Bottom - (controlBounds.Bottom + 2);
		int spaceAbove = (controlBounds.Top - 2) - workingArea.Top;

		// 优先向下
		if (spaceBelow >= desiredHeight)
		{
			currentDropDownDirection = DropDownDirection.Down;
			height = desiredHeight;
			location = new Point(controlBounds.Left, controlBounds.Bottom + 2);
			return;
		}

		// 下方放不下，但上方能放下
		if (spaceAbove >= desiredHeight)
		{
			currentDropDownDirection = DropDownDirection.Up;
			height = desiredHeight;
			location = new Point(controlBounds.Left, controlBounds.Top - height - 2);
			return;
		}

		// 两边都放不下，选空间更大的一边
		if (spaceBelow >= spaceAbove)
		{
			currentDropDownDirection = DropDownDirection.Down;
			height = Math.Max(itemHeight + 12, spaceBelow);
			height = Math.Min(height, desiredHeight);
			location = new Point(controlBounds.Left, controlBounds.Bottom + 2);
		}
		else
		{
			currentDropDownDirection = DropDownDirection.Up;
			height = Math.Max(itemHeight + 12, spaceAbove);
			height = Math.Min(height, desiredHeight);
			location = new Point(controlBounds.Left, controlBounds.Top - height - 2);
		}

		// 左右也顺手保护一下，避免超出屏幕
		int width = Width;

		int x = location.X;
		if (x + width > workingArea.Right)
			x = workingArea.Right - width;

		if (x < workingArea.Left)
			x = workingArea.Left;

		location = new Point(x, location.Y);
	}

	private string GetItemDisplayText(object item)
	{
		if (item == null) return string.Empty;

		if (string.IsNullOrWhiteSpace(DisplayMember))
			return item.ToString() ?? string.Empty;

		var value = GetMemberValue(item, DisplayMember);
		return value?.ToString() ?? string.Empty;
	}

	private object GetValueMemberValue(object item)
	{
		if (item == null) return null;

		if (string.IsNullOrWhiteSpace(ValueMember))
			return item;

		return GetMemberValue(item, ValueMember);
	}

	private object GetMemberValue(object obj, string memberName)
	{
		if (obj == null || string.IsNullOrWhiteSpace(memberName))
			return null;

		var type = obj.GetType();

		var prop = type.GetProperty(memberName,
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (prop != null)
			return prop.GetValue(obj);

		var field = type.GetField(memberName,
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (field != null)
			return field.GetValue(obj);

		return null;
	}

	#endregion

	#region Events

	private void TextBox_TextChanged(object sender, EventArgs e)
	{
		if (!UIReady) return;
		if (suppressTextChanged) return;
		var text = (sender as Control).Text ;
		RefreshFilter(text);

		// 新增：根据输入文本同步 SelectedIndex
		SyncSelectedIndexFromText(text);

		// 顺便同步 listBox 当前高亮项
		if (listBox != null)
		{
			var selected = SelectedItem;
			if (selected != null)
			{
				int indexInFiltered = filteredItems.IndexOf(selected);
				if (indexInFiltered >= 0 && indexInFiltered < listBox.Items.Count)
				{
					listBox.SelectedIndex = indexInFiltered;
				}
			}
			else
			{
				listBox.ClearSelected();
			}
		}

		if (FocusedOrChildFocused() && filteredItems.Count > 0)
		{
			if (dropdown == null || !dropdown.Visible)
			{
				ShowDropdown();
			}
			else
			{
				AdjustDropDownBounds();
			}
		}
		else if (filteredItems.Count == 0)
		{
			HideDropdown();
		}
	}


	private void TextBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Down)
		{
			if (dropdown == null || !dropdown.Visible)
			{
				ShowDropdown();
			}
			else if (listBox != null && listBox.SelectedIndex < listBox.Items.Count - 1)
			{
				listBox.SelectedIndex++;
			}

			e.Handled = true;
			e.SuppressKeyPress = true;
			return;
		}

		if (e.KeyCode == Keys.Up)
		{
			if (dropdown != null && dropdown.Visible && listBox != null && listBox.SelectedIndex > 0)
			{
				listBox.SelectedIndex--;
			}

			e.Handled = true;
			e.SuppressKeyPress = true;
			return;
		}

		if (e.KeyCode == Keys.Enter)
		{
			if (dropdown != null && dropdown.Visible)
			{
				CommitSelection();
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
			return;
		}

		if (e.KeyCode == Keys.Escape)
		{
			HideDropdown();
			e.Handled = true;
			e.SuppressKeyPress = true;
		}
	}

	private bool FocusedOrChildFocused()
	{
		return Focused || (textBox != null && textBox.Focused) || (listBox != null && listBox.Focused);
	}

	#endregion

	#region Paint

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		var g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;

		var rect = new Rectangle(0, 0, Width - 1, Height - 1);

		using (var path = CreateRoundRect(rect, 6))
		{
			Color borderColor = Color.FromArgb(200, 200, 200);

			if (isFocused || (textBox != null && textBox.Focused))
				borderColor = Color.FromArgb(0, 120, 215);
			else if (isHovered)
				borderColor = Color.FromArgb(150, 150, 150);

			using (var pen = new Pen(borderColor, 1))
			{
				g.DrawPath(pen, path);
			}
			if (lastbound != path.GetBounds())
			{
				Region = new Region(path);
			}
			lastbound = path.GetBounds();
		}
	}

	private GraphicsPath CreateRoundRect(Rectangle rect, int radius)
	{
		var path = new GraphicsPath();

		path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
		path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
		path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
		path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
		path.CloseFigure();

		return path;
	}

	#endregion

	#region Override
	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateLayout();
	}


	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);

		if (textBox != null)
			textBox.Font = Font;

		if (listBox != null)
			listBox.Font = Font;
	}

	#endregion
}
public class ShadowDropDownForm : Form
{
	private RectangleF lastbound;

	public int CornerRadius { get; set; } = 8;

	public ShadowDropDownForm()
	{
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		ShowInTaskbar = false;
		TopMost = true;
		BackColor = Color.White;
		Padding = new Padding(6);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

		var rect = new Rectangle(0, 0, Width - 1, Height - 1);

		using (var path = CreateRoundRect(rect, CornerRadius))
		{

			if (lastbound != path.GetBounds())
			{
				Region = new Region(path);
			}
			lastbound = path.GetBounds();
			using (var shadowPen = new Pen(Color.FromArgb(25, 0, 0, 0), 6))
			{
				e.Graphics.DrawPath(shadowPen, path);
			}

			using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
			{
				e.Graphics.DrawPath(borderPen, path);
			}
		}
	}

	private GraphicsPath CreateRoundRect(Rectangle rect, int radius)
	{
		var path = new GraphicsPath();

		path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
		path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
		path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
		path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
		path.CloseFigure();

		return path;
	}
}

public enum DropDownDirection
{
	Down,
	Up
}
