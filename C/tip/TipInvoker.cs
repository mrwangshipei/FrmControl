using System;
using System.Windows.Forms;

namespace IndustrialTip
{
	internal static class TipInvoker
	{
		public static void Invoke(Form form, Action action)
		{
			if (form.IsDisposed) return;

			if (form.InvokeRequired)
				form.BeginInvoke(action);
			else
				action();
		}
	}
}
