using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YourNamespace
{
	public static class TabTipExtensions
	{
		private static readonly string TabTipExePath =
			@"C:\Program Files\Common Files\Microsoft Shared\ink\TabTip.exe";

		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_CLOSE = 0xF060;

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		public static void BindTabTip(this TextBox textBox)
		{
			if (textBox == null) return;

			textBox.GotFocus -= Input_GotFocus;
			textBox.LostFocus -= Input_LostFocus;

			textBox.GotFocus += Input_GotFocus;
			textBox.LostFocus += Input_LostFocus;
		}

		public static void BindTabTip(this NumericUpDown numericUpDown)
		{
			if (numericUpDown == null) return;

			numericUpDown.GotFocus -= Input_GotFocus;
			numericUpDown.LostFocus -= Input_LostFocus;

			numericUpDown.GotFocus += Input_GotFocus;
			numericUpDown.LostFocus += Input_LostFocus;

			TextBox innerTextBox = FindInnerTextBox(numericUpDown);
			if (innerTextBox != null)
			{
				innerTextBox.GotFocus -= Input_GotFocus;
				innerTextBox.LostFocus -= Input_LostFocus;

				innerTextBox.GotFocus += Input_GotFocus;
				innerTextBox.LostFocus += Input_LostFocus;
			}
		}

		private static void Input_GotFocus(object sender, EventArgs e)
		{
			ShowTabTipByExe();
		}

		private static void Input_LostFocus(object sender, EventArgs e)
		{
			CloseTabTip();
		}

		private static void ShowTabTipByExe()
		{
			try
			{
				if (!File.Exists(TabTipExePath))
					return;

				Process.Start(new ProcessStartInfo
				{
					FileName = TabTipExePath,
					UseShellExecute = true
				});
			}
			catch
			{
			}
		}

		private static void CloseTabTip()
		{
			try
			{
				IntPtr hWnd = FindWindow("IPTip_Main_Window", null);
				if (hWnd != IntPtr.Zero)
				{
					SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)SC_CLOSE, IntPtr.Zero);
				}

				foreach (var p in Process.GetProcessesByName("TabTip").ToList())
				{
					try
					{
						if (!p.HasExited)
						{
							p.Kill();
							p.WaitForExit(500);
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}

		private static TextBox FindInnerTextBox(Control parent)
		{
			foreach (Control c in parent.Controls)
			{
				if (c is TextBox tb)
					return tb;

				if (c.HasChildren)
				{
					TextBox child = FindInnerTextBox(c);
					if (child != null)
						return child;
				}
			}

			return null;
		}
	}
}
