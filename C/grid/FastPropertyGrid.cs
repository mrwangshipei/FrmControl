using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
class PropertyRow
{
	public string DisplayName;
	public PropertyDescriptor Descriptor;
	public object Owner;
	public int Level;
	public bool IsExpandable;
	public bool IsExpanded;

	// 🔥 新增：用于循环检测
	public HashSet<object> ObjectPath;
}
class ReferenceEqualityComparer : IEqualityComparer<object>
{
	public new bool Equals(object x, object y)
		=> ReferenceEquals(x, y);

	public int GetHashCode(object obj)
		=> System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}

public class FastPropertyGrid : UserControl
{
	private readonly DataGridView _grid = new DataGridView();
	private readonly List<PropertyRow> _rows = new List<PropertyRow>();
	private object _root;

	public FastPropertyGrid()
	{
	//	Dock = DockStyle.Fill;

		_grid.Dock = DockStyle.Fill;
		_grid.VirtualMode = true;
		_grid.AllowUserToAddRows = false;
		_grid.RowHeadersVisible = false;
		_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_grid.EditMode = DataGridViewEditMode.EditOnEnter;

		_grid.Columns.Add("Name", "Name");
		_grid.Columns.Add("Value", "Value");
		_grid.EditingControlShowing += Grid_EditingControlShowing;
		_grid.CellBeginEdit += Grid_CellBeginEdit;

		_grid.CellValueNeeded += Grid_CellValueNeeded;
		_grid.CellValuePushed += Grid_CellValuePushed;
		_grid.CellDoubleClick += Grid_CellDoubleClick;

		Controls.Add(_grid);
	}
	private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
	{
		int rowIndex = _grid.CurrentCell.RowIndex;
		var row = _rows[rowIndex];
		var prop = row.Descriptor;

		// 先清理遗留事件（非常关键）
		if (e.Control is ComboBox oldCombo)
		{
			oldCombo.SelectedIndexChanged -= EnumCombo_SelectedIndexChanged;
		}

		// ===== bool =====
		if (IsBool(prop))
		{
			var chk = new CheckBox
			{
				Dock = DockStyle.Fill,
				Checked = Convert.ToBoolean(prop.GetValue(row.Owner))
			};

			chk.CheckedChanged += (s, _) =>
			{
				prop.SetValue(row.Owner, chk.Checked);
				_grid.EndEdit();
				_grid.InvalidateRow(rowIndex);
			};

			e.Control.Dispose();
			_grid.Controls.Add(chk);
			chk.BringToFront();
			return;
		}

		// ===== enum =====
		if (IsEnum(prop) && e.Control is ComboBox combo)
		{
			combo.DropDownStyle = ComboBoxStyle.DropDownList;
			combo.DataSource = Enum.GetValues(prop.PropertyType);
			combo.SelectedItem = prop.GetValue(row.Owner);

			combo.SelectedIndexChanged += EnumCombo_SelectedIndexChanged;
			return;
		}

		// ===== default (TextBox) =====
		if (e.Control is TextBox tb)
		{
			tb.BorderStyle = BorderStyle.None;
		}
	}
	private void EnumCombo_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (!(sender is ComboBox combo)) return;

		int rowIndex = _grid.CurrentCell.RowIndex;
		var row = _rows[rowIndex];

		row.Descriptor.SetValue(row.Owner, combo.SelectedItem);
		_grid.EndEdit();
		_grid.InvalidateRow(rowIndex);
	}

	private void Grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
	{
		var row = _rows[e.RowIndex];

		// 对象节点 / Name 列 一律不可编辑
		if (row.IsExpandable || e.ColumnIndex != 1)
		{
			e.Cancel = true;
		}
	}
	private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

		var row = _rows[e.RowIndex];
		if (!row.IsExpandable) return;

		if (row.IsExpanded)
			Collapse(e.RowIndex);
		else
			Expand(e.RowIndex);
	}
	private void Expand(int index)
	{
		var row = _rows[index];
		var value = row.Descriptor.GetValue(row.Owner);
		if (value == null) return;

		// 🔒 循环引用检测
		if (row.ObjectPath.Contains(value))
		{

			row.DisplayName += " (循环引用)";
			return;
		}

		int insertIndex = index + 1;

		// 生成子路径
		var childPath = new HashSet<object>(
			row.ObjectPath,
			new ReferenceEqualityComparer())
	{
		value
	};

		var props = TypeDescriptor.GetProperties(value);
		foreach (PropertyDescriptor p in props)
		{
			_rows.Insert(insertIndex++, new PropertyRow
			{
				DisplayName = p.DisplayName,
				Descriptor = p,
				Owner = value,
				Level = row.Level + 1,
				IsExpandable = IsComplexObject(p.PropertyType),
				IsExpanded = false,
				ObjectPath = childPath
			});
		}

		row.IsExpanded = true;
		_grid.RowCount = _rows.Count;
		_grid.Invalidate();
	}

	private void Grid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
	{
		var row = _rows[e.RowIndex];

		if (e.ColumnIndex == 0)
		{
			e.Value = new string(' ', row.Level * 4) +
					  (row.IsExpandable ? (row.IsExpanded ? "▼ " : "▶ ") : "") +
					  row.DisplayName;
		}
		else
		{
			if (!row.IsExpandable)
				e.Value = row.Descriptor.GetValue(row.Owner);
			else
				e.Value = "";
		}
	}

	private void Grid_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
	{
		var row = _rows[e.RowIndex];
		if (e.ColumnIndex != 1 || row.IsExpandable) return;

		row.Descriptor.SetValue(row.Owner, e.Value);
	}

private void Collapse(int index)
	{
		var level = _rows[index].Level;
		int removeIndex = index + 1;

		while (removeIndex < _rows.Count && _rows[removeIndex].Level > level)
		{
			_rows.RemoveAt(removeIndex);
		}

		_rows[index].IsExpanded = false;
		_grid.RowCount = _rows.Count;
		_grid.Invalidate();
	}
	public void SetObject(object obj)
	{
		_root = obj;
		_rows.Clear();

		var rootPath = new HashSet<object>(new ReferenceEqualityComparer())
	{
		obj
	};

		BuildRows(obj, 0, rootPath);

		_grid.RowCount = _rows.Count;
		_grid.Invalidate();
	}

	private void BuildRows(object obj, int level, HashSet<object> path)
	{
		var props = TypeDescriptor.GetProperties(obj);

		foreach (PropertyDescriptor p in props)
		{
			bool expandable = IsComplexObject(p.PropertyType);

			_rows.Add(new PropertyRow
			{
				DisplayName = p.DisplayName,
				Descriptor = p,
				Owner = obj,
				Level = level,
				IsExpandable = expandable,
				IsExpanded = false,

				// 🔥 复制一份路径（不可共享）
				ObjectPath = new HashSet<object>(path, new ReferenceEqualityComparer())
			});
		}
	}

	private static bool IsBool(PropertyDescriptor p)
	=> p.PropertyType == typeof(bool);

	private static bool IsEnum(PropertyDescriptor p)
		=> p.PropertyType.IsEnum;

	private bool IsComplexObject(Type type)
	{
		return type.IsClass && type != typeof(string);
	}
}
