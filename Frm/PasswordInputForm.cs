using System;
using System.Drawing;
using System.Windows.Forms;

namespace YourApp
{
	public class PasswordInputForm : Form
	{
		private readonly TextBox _txt;
		private readonly TableLayoutPanel _pad;

		private readonly Button _btnBack;
		private readonly Button _btnClear;
		private readonly Button _btnOk;
		private readonly Button _btnCancel;

		public event EventHandler PasswordChanged;

		public bool EnableEnterEscHotkey { get; set; } = true;

		public string PasswordText
		{
			get => _txt.Text;
			set => _txt.Text = value ?? "";
		}

		public PasswordInputForm()
		{
			// === Form 设置 ===
			this.Text = "输入密码";
			this.StartPosition = FormStartPosition.CenterParent;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.ShowInTaskbar = false;
			this.ClientSize = new Size(360, 320);
			this.KeyPreview = true; // 关键：窗体先收到按键，Enter/Esc 稳定

			this.BackColor = SystemColors.Control;

			// === 顶部密码框 ===
			_txt = new TextBox
			{
				Dock = DockStyle.Top,
				Font = new Font("Segoe UI", 12f),
				UseSystemPasswordChar = true,
				ShortcutsEnabled = true,
				Margin = new Padding(8),
			};
			_txt.TextChanged += (s, e) => PasswordChanged?.Invoke(this, EventArgs.Empty);

			// === 数字键盘 ===
			_pad = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 3,
				RowCount = 4,
				Padding = new Padding(8),
			};
			for (int i = 0; i < 3; i++) _pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
			for (int i = 0; i < 4; i++) _pad.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

			// === 右侧按钮区 ===
			var rightPanel = new Panel
			{
				Dock = DockStyle.Right,
				Width = 96,
				Padding = new Padding(8),
			};

			// === 右侧按钮颜色调整 ===
			_btnBack = new Button
			{
				Text = "⌫",
				Dock = DockStyle.Top,
				Height = 42,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				BackColor = Color.Red,  // 删除按钮标记为红色
				ForeColor = Color.White,
			};
			_btnBack.Click += (s, e) => Backspace();

			_btnClear = new Button
			{
				Text = "清空",
				Dock = DockStyle.Top,
				Height = 42,
				Margin = new Padding(0, 8, 0, 0),
				Font = new Font("Segoe UI", 10f),
				BackColor = Color.Orange,  // 清空按钮标记为橙色
				ForeColor = Color.White,
			};
			_btnClear.Click += (s, e) => Clear();

			_btnOk = new Button
			{
				Text = "确认",
				Dock = DockStyle.Top,
				Height = 42,
				Margin = new Padding(0, 14, 0, 0),
				Font = new Font("Segoe UI", 10f, FontStyle.Bold),
				BackColor = Color.Green,  // 确认按钮标记为绿色
				ForeColor = Color.White,
			};
			_btnOk.Click += (s, e) => ConfirmOk();

			_btnCancel = new Button
			{
				Text = "取消",
				Dock = DockStyle.Top,
				Height = 42,
				Margin = new Padding(0, 8, 0, 0),
				Font = new Font("Segoe UI", 10f),
				BackColor = Color.Red,  // 取消按钮标记为红色
				ForeColor = Color.White,
			};
			_btnCancel.Click += (s, e) => CancelClose();

			rightPanel.Controls.Add(_btnCancel);
			rightPanel.Controls.Add(_btnOk);
			rightPanel.Controls.Add(_btnClear);
			rightPanel.Controls.Add(_btnBack);

			// === 主区 ===
			var main = new Panel { Dock = DockStyle.Fill };
			main.Controls.Add(_pad);
			main.Controls.Add(rightPanel);

			this.Controls.Add(main);
			this.Controls.Add(_txt);

			BuildKeypad();

			// === 热键（窗体级）=== 
			this.KeyDown += PasswordInputForm_KeyDown;
		}

		private void PasswordInputForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (!EnableEnterEscHotkey) return;

			if (e.KeyCode == Keys.Enter)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				ConfirmOk();
				return;
			}
			if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				CancelClose();
				return;
			}
		}

		private void BuildKeypad()
		{
			_pad.Controls.Clear();

			int n = 1;
			for (int r = 0; r < 3; r++)
			{
				for (int c = 0; c < 3; c++)
				{
					_pad.Controls.Add(CreatePadButton(n.ToString()), c, r);
					n++;
				}
			}

			_pad.Controls.Add(CreatePadButton("."), 0, 3);
			_pad.Controls.Add(CreatePadButton("0"), 1, 3);
			_pad.Controls.Add(new Label { Dock = DockStyle.Fill }, 2, 3); // 占位
		}

		private Button CreatePadButton(string text)
		{
			var b = new Button
			{
				Text = text,
				Dock = DockStyle.Fill,
				Margin = new Padding(4),
				Font = new Font("Segoe UI", 12f, FontStyle.Bold),
				Tag = text
			};
			b.Click += (s, e) =>
			{
				var t = (string)((Control)s).Tag;
				AppendFromKeypad(t);
			};
			return b;
		}

		public void AppendFromKeypad(string token)
		{
			if (string.IsNullOrEmpty(token)) return;
			if (!(token == "." || (token.Length == 1 && char.IsDigit(token[0])))) return;
			InsertAtCaret(token);
		}

		private void InsertAtCaret(string s)
		{
			_txt.Focus();

			int selStart = _txt.SelectionStart;
			string before = _txt.Text.Substring(0, selStart);
			string after = _txt.Text.Substring(selStart + _txt.SelectionLength);

			string candidate = before + s + after;
			if (_txt.MaxLength > 0 && candidate.Length > _txt.MaxLength) return;

			_txt.Text = candidate;
			_txt.SelectionStart = selStart + s.Length;
		}

		public void Backspace()
		{
			_txt.Focus();

			int selStart = _txt.SelectionStart;
			int selLen = _txt.SelectionLength;

			if (selLen > 0)
			{
				string before = _txt.Text.Substring(0, selStart);
				string after = _txt.Text.Substring(selStart + selLen);
				_txt.Text = before + after;
				_txt.SelectionStart = selStart;
			}
			else if (selStart > 0)
			{
				string before = _txt.Text.Substring(0, selStart - 1);
				string after = _txt.Text.Substring(selStart);
				_txt.Text = before + after;
				_txt.SelectionStart = selStart - 1;
			}
		}

		public void Clear()
		{
			_txt.Text = "";
			_txt.Focus();
		}

		private void ConfirmOk()
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void CancelClose()
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}
