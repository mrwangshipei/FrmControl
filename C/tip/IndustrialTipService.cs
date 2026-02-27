using FrmControl.C;
using System.Drawing;
using System.Windows.Forms;

namespace IndustrialTip
{
	public static class IndustrialTipService
	{
		public static void ShowSuccess(string message)
		{
			var f = TipForm.Instance;
			TipInvoker.Invoke(f, () =>
			{
				f.SetText("✔ " + message);
				f.SetBackColor(f.SuccessColor);
				f.StopAlarmFlash();
				f.ShowSafe();
			});
		}
		public static void ShowError(string message)
		{
			var f = TipForm.Instance;
			TipInvoker.Invoke(f, () =>
			{
				f.SetText("✖ " + message);
				f.SetBackColor(f.ErrorColor);
				f.StartAlarmFlash(f.ErrorColor); // 允许闪
				f.ShowSafe();
			});
		}

		public static void ShowMessage(string message)
		{
			var f = TipForm.Instance;
			TipInvoker.Invoke(f, () =>
			{
				f.SetText(message);
				f.StopAlarmFlash();
				f.ShowSafe();
			});
		}

		public static void ShowAlarm(string message, AlarmLevel level)
		{
			var f = TipForm.Instance;
			TipInvoker.Invoke(f, () =>
			{
				f.SetText(message);

				if (level == AlarmLevel.Error)
				{
					f.StartAlarmFlash(Color.FromArgb(120, 30, 30));
				}
				else if (level == AlarmLevel.Warning)
				{
					f.SetBackColor(Color.FromArgb(120, 90, 20));
					f.StopAlarmFlash();
				}

				f.ShowSafe();
			});
		}

		public static void ClearAlarm()
		{
			var f = TipForm.Instance;
			TipInvoker.Invoke(f, () =>
			{
				f.StopAlarmFlash();
				f.Hide();
			});
		}
	}
}
