public partial class MultiGroupCheckForm : Form
{
	private static List<string> _options;
	private static int _groupCount;

	private TableLayoutPanel tableLayout;

	public MultiGroupCheckForm()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
			this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
			this.SuspendLayout();
			// 
			// tableLayout
			// 
			this.tableLayout.AutoScroll = true;
			this.tableLayout.ColumnCount = 1;
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayout.Location = new System.Drawing.Point(0, 0);
			this.tableLayout.Name = "tableLayout";
			this.tableLayout.Padding = new System.Windows.Forms.Padding(10);
			this.tableLayout.Size = new System.Drawing.Size(684, 411);
			this.tableLayout.TabIndex = 0;
			// 
			// MultiGroupCheckForm
			// 
			this.ClientSize = new System.Drawing.Size(684, 411);
			this.Controls.Add(this.tableLayout);
			this.Name = "MultiGroupCheckForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Multi Group CheckBox";
			this.ResumeLayout(false);

	}

	/// <summary>
	/// 初始化界面
	/// </summary>
	private void BuildUI()
	{
		tableLayout.RowStyles.Clear();
		tableLayout.Controls.Clear();

		tableLayout.ColumnCount = 1;
		tableLayout.RowCount = _groupCount * 2; // 每组两行：分割线 + 内容

		int currentRow = 0;

		for (int i = 0; i < _groupCount; i++)
		{
			// ===== 分割线 Label =====
			Label line = new Label();
			line.Height = 1;
			line.Dock = DockStyle.Fill;
			line.BackColor = Color.Black;
			line.Margin = new Padding(40, 15, 40, 10); // 居中效果
			line.Anchor = AnchorStyles.Left | AnchorStyles.Right;

			tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tableLayout.Controls.Add(line, 0, currentRow++);

			// ===== 复选框网格 =====
			TableLayoutPanel grid = new TableLayoutPanel();
			grid.AutoSize = true;
			grid.Dock = DockStyle.Top;
			grid.Margin = new Padding(30, 5, 30, 15);

			int columnCount = 4; // 每行 4 个
			grid.ColumnCount = columnCount;

			for (int c = 0; c < columnCount; c++)
			{
				grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnCount));
			}

			int row = 0;
			int col = 0;

			foreach (var opt in _options)
			{
				if (col >= columnCount)
				{
					col = 0;
					row++;
				}

				if (grid.RowCount <= row)
				{
					grid.RowCount = row + 1;
					grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				}

				CheckBox cb = new CheckBox();
				cb.Text = opt;
				cb.AutoSize = true;
				cb.Margin = new Padding(8, 6, 8, 6);

				grid.Controls.Add(cb, col, row);
				col++;
			}

			tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tableLayout.Controls.Add(grid, 0, currentRow++);
		}
	}


	/// <summary>
	/// 读取当前所有组选中结果
	/// </summary>
	private List<GroupSelectionResult> ReadSelections()
	{
		var result = new List<GroupSelectionResult>();
		int groupIndex = 0;

		for (int i = 0; i < tableLayout.Controls.Count; i++)
		{
			var grid = tableLayout.Controls[i] as TableLayoutPanel;
			if (grid == null) continue; // 跳过分割线

			var selected = new List<string>();

			for (int j = 0; j < grid.Controls.Count; j++)
			{
				if (grid.Controls[j] is CheckBox cb && cb.Checked)
				{
					selected.Add(cb.Text);
				}
			}

			result.Add(new GroupSelectionResult
			{
				GroupIndex = groupIndex++,
				SelectedOptions = selected
			});
		}

		return result;
	}


	/// <summary>
	/// 对外静态方法：显示窗体并返回结果
	/// </summary>
	public static List<GroupSelectionResult> ShowDialogAndGetResult(
		List<string> options,
		int groupCount)
	{
		_options = options;
		_groupCount = groupCount;

		using (var form = new MultiGroupCheckForm())
		{
			form.BuildUI();

			// 添加一个确认按钮
			
			Button btnOk = new Button();
			btnOk.Text = "OK";
			btnOk.Height = 40;
			btnOk.Dock = DockStyle.Bottom;

			// Flat 风格美化
			btnOk.FlatStyle = FlatStyle.Flat;
			btnOk.BackColor = Color.FromArgb(60, 60, 60);   // 深灰
			btnOk.ForeColor = Color.White;
			btnOk.Font = new Font("Segoe UI", 10, FontStyle.Bold);

			btnOk.FlatAppearance.BorderColor = Color.Black;
			btnOk.FlatAppearance.BorderSize = 1;
			btnOk.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
			btnOk.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);

			btnOk.Margin = new Padding(10);

			List<GroupSelectionResult> finalResult = null;

			btnOk.Click += (s, e) =>
			{
				finalResult = form.ReadSelections();
				form.DialogResult = DialogResult.OK;
				form.Close();
			};

			form.Controls.Add(btnOk);

			if (form.ShowDialog() == DialogResult.OK)
			{
				return finalResult;
			}
		}

		return new List<GroupSelectionResult>();
	}

}
public class GroupSelectionResult
{
	public int GroupIndex { get; set; }          // 第几组（从 0 开始）
	public List<string> SelectedOptions { get; set; }
}
