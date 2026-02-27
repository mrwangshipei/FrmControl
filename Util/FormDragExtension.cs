using System;
using System.Drawing;
using System.Windows.Forms;

public static class FormDragExtension
{
	/// <summary>
	/// 仅指定控件可拖动窗体
	/// </summary>
	public static void EnableDragMove(this Form form, Control control)
	{
		Point mouseOffset = Point.Empty;
		bool isDragging = false;

		control.MouseDown += (s, e) =>
		{
			if (e.Button == MouseButtons.Left)
			{
				isDragging = true;
				mouseOffset = new Point(e.X, e.Y);
			}
		};

		control.MouseMove += (s, e) =>
		{
			if (isDragging)
			{
				form.Location = new Point(
					form.Left + e.X - mouseOffset.X,
					form.Top + e.Y - mouseOffset.Y);
			}
		};

		control.MouseUp += (s, e) =>
		{
			isDragging = false;
		};
	}

	/// <summary>
	/// 指定控件及其所有子控件均可拖动窗体（递归）
	/// </summary>
	public static void EnableDragMoveWithChildren(this Form form, Control rootControl)
	{
		// root 本身
		form.EnableDragMove(rootControl);

		// 所有子控件递归注册
		foreach (Control child in rootControl.Controls)
		{
			form.EnableDragMoveWithChildren(child);
		}
	}
}
