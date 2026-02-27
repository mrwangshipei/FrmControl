using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class PlaceholderTextBox : TextBox
{
	private const int WM_PAINT = 0x000F;

	public string PlaceholderText { get; set; } = "";
	public Color PlaceholderColor { get; set; } = Color.Gray;

	public PlaceholderTextBox()
	{
		SetStyle(ControlStyles.UserPaint, false);
	}

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);

		if (m.Msg == WM_PAINT)
		{
			DrawPlaceholder();
		}
	}

	private void DrawPlaceholder()
	{
		// 有内容 or 有焦点 → 不画 Placeholder
		if (!string.IsNullOrEmpty(this.Text) || this.Focused)
			return;

		using (Graphics g = Graphics.FromHwnd(this.Handle))
		{
			TextFormatFlags flags =
				TextFormatFlags.Left |
				TextFormatFlags.VerticalCenter |
				TextFormatFlags.NoPadding;

			Rectangle rect = ClientRectangle;
			rect.Offset(1, 1); // 轻微偏移更自然

			TextRenderer.DrawText(
				g,
				PlaceholderText,
				this.Font,
				rect,
				PlaceholderColor,
				flags
			);
		}
	}
}
