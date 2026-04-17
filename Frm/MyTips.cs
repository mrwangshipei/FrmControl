using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FrmControl.FrmBase_;

namespace FrmControl.Frm
{
	public partial class MyTips : FrmBase
	{
		public MyTips()
		{
			InitializeComponent();
			ShowInTaskbar = false;
			FormClosed += MyTips_FormClosed;
		}

		public float radius { get; set; } = 10f;

		private int _ImageIndex;
		public int ImageIndex
		{
			get => _ImageIndex;
			set
			{
				_ImageIndex = value;
				if (!IsDisposed)
					Invalidate();
			}
		}

		private const int MaxCount = 4;
		private const int Margin = 10;
		private const int DuplicateInterval = 300;

		private sealed class ToastContext
		{
			public int UiThreadId;
			public Form HostForm;
			public List<MyTips> ActiveTips = new List<MyTips>();
			public Dictionary<string, DateTime> RecentTips = new Dictionary<string, DateTime>();
		}

		private static readonly object syncRoot = new object();
		private static readonly Dictionary<int, ToastContext> contexts = new Dictionary<int, ToastContext>();
		private static Form _defaultHostForm;

		private int _ownerThreadId = -1;

		#region 对外 API

		public static void SetDefaultHost(Form form)
		{
			if (form == null || form.IsDisposed) return;

			lock (syncRoot)
			{
				_defaultHostForm = form;
			}

			RegisterThreadHost(form);
		}

		public static void SetThreadHost(Form form)
		{
			if (form == null || form.IsDisposed) return;
			RegisterThreadHost(form);
		}

		public static MyTips ShowSuccess(string msg, int duration = 2000)
			=> Show(null, Tipstype.Success, msg, duration);

		public static MyTips ShowInfo(string msg, int duration = 2000)
			=> Show(null, Tipstype.Tip, msg, duration);

		public static MyTips ShowWarn(string msg, int duration = 2000)
			=> Show(null, Tipstype.Warn, msg, duration);

		public static MyTips ShowError(string msg, int duration = 2000)
			=> Show(null, Tipstype.Error, msg, duration);

		public static MyTips ShowSuccess(Form owner, string msg, int duration = 2000)
			=> Show(owner, Tipstype.Success, msg, duration);

		public static MyTips ShowInfo(Form owner, string msg, int duration = 2000)
			=> Show(owner, Tipstype.Tip, msg, duration);

		public static MyTips ShowWarn(Form owner, string msg, int duration = 2000)
			=> Show(owner, Tipstype.Warn, msg, duration);

		public static MyTips ShowError(Form owner, string msg, int duration = 2000)
			=> Show(owner, Tipstype.Error, msg, duration);

		// 兼容旧接口
		public static MyTips ShowTipSuccess(string msg, int wait = 2000)
			=> ShowSuccess(msg, wait);

		public static MyTips ShowTipTip(string msg, int wait = 2000)
			=> ShowInfo(msg, wait);

		public static MyTips ShowTipWarn(string msg, int wait = 2000)
			=> ShowWarn(msg, wait);

		public static MyTips ShowTipError(string msg, int wait = 2000)
			=> ShowError(msg, wait);

		// 新增：兼容旧命名 + 指定宿主
		public static MyTips ShowTipSuccess(Form owner, string msg, int wait = 2000)
			=> ShowSuccess(owner, msg, wait);

		public static MyTips ShowTipTip(Form owner, string msg, int wait = 2000)
			=> ShowInfo(owner, msg, wait);

		public static MyTips ShowTipWarn(Form owner, string msg, int wait = 2000)
			=> ShowWarn(owner, msg, wait);

		public static MyTips ShowTipError(Form owner, string msg, int wait = 2000)
			=> ShowError(owner, msg, wait);

		public static new MyTips Show(Form baseForm, Tipstype type, string msg, int duration = 2000)
		{
			Form host = ResolveHostForm(baseForm);
			if (host == null) return null;

			if (host.InvokeRequired)
			{
				try
				{
					return (MyTips)host.Invoke(new Func<MyTips>(() => ShowCore(host, type, msg, duration)));
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex);
					return null;
				}
			}

			return ShowCore(host, type, msg, duration);
		}

		#endregion

		#region 宿主与上下文

		private static void RegisterThreadHost(Form form)
		{
			if (form == null || form.IsDisposed) return;

			if (!form.IsHandleCreated)
			{
				try
				{
					var _ = form.Handle;
				}
				catch
				{
					return;
				}
			}

			int threadId = GetFormThreadId(form);

			lock (syncRoot)
			{
				if (!contexts.TryGetValue(threadId, out var ctx))
				{
					ctx = new ToastContext
					{
						UiThreadId = threadId,
						HostForm = form
					};
					contexts[threadId] = ctx;
				}
				else
				{
					ctx.HostForm = form;
				}
			}

			form.FormClosed -= HostForm_FormClosed;
			form.FormClosed += HostForm_FormClosed;
		}

		private static void HostForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			var form = sender as Form;
			if (form == null) return;

			int threadId = GetFormThreadId(form);

			lock (syncRoot)
			{
				if (contexts.TryGetValue(threadId, out var ctx) && ReferenceEquals(ctx.HostForm, form))
				{
					ctx.HostForm = null;
				}

				if (_defaultHostForm == form)
				{
					_defaultHostForm = null;
				}
			}
		}

		private static Form ResolveHostForm(Form inputForm)
		{
			if (IsValidHost(inputForm))
			{
				RegisterThreadHost(inputForm);
				return inputForm;
			}

			int currentThreadId = Thread.CurrentThread.ManagedThreadId;

			lock (syncRoot)
			{
				if (contexts.TryGetValue(currentThreadId, out var ctx) && IsValidHost(ctx.HostForm))
					return ctx.HostForm;

				if (IsValidHost(_defaultHostForm))
					return _defaultHostForm;
			}

			var openForm = Application.OpenForms
				.Cast<Form>()
				.FirstOrDefault(IsValidHost);

			if (openForm != null)
			{
				RegisterThreadHost(openForm);
				return openForm;
			}

			return null;
		}

		private static bool IsValidHost(Form form)
		{
			return form != null && !form.IsDisposed && form.IsHandleCreated;
		}

		private static int GetFormThreadId(Form form)
		{
			if (form == null) return -1;
			return form.InvokeRequired
				? GetWindowThreadId(form.Handle)
				: Thread.CurrentThread.ManagedThreadId;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

		private static int GetWindowThreadId(IntPtr handle)
		{
			GetWindowThreadProcessId(handle, out _);
			return GetWindowThreadProcessId(handle, out _);
		}

		private static ToastContext GetContextByThreadId(int threadId)
		{
			lock (syncRoot)
			{
				contexts.TryGetValue(threadId, out var ctx);
				return ctx;
			}
		}

		#endregion

		#region 核心逻辑

		private static MyTips ShowCore(Form host, Tipstype type, string msg, int duration)
		{
			if (host == null || host.IsDisposed) return null;

			int threadId = GetFormThreadId(host);

			ToastContext ctx;
			lock (syncRoot)
			{
				if (!contexts.TryGetValue(threadId, out ctx))
				{
					ctx = new ToastContext
					{
						UiThreadId = threadId,
						HostForm = host
					};
					contexts[threadId] = ctx;
				}
				else
				{
					ctx.HostForm = host;
				}
			}

			lock (ctx)
			{
				string key = $"{type}_{msg}";
				DateTime now = DateTime.Now;

				var expiredKeys = ctx.RecentTips
					.Where(kv => (now - kv.Value).TotalMilliseconds > DuplicateInterval)
					.Select(kv => kv.Key)
					.ToList();

				foreach (var k in expiredKeys)
					ctx.RecentTips.Remove(k);

				if (ctx.RecentTips.ContainsKey(key))
					return null;

				ctx.RecentTips[key] = now;

				var alive = ctx.ActiveTips.Where(t => t != null && !t.IsDisposed).ToList();

				while (alive.Count >= MaxCount)
				{
					try
					{
						var old = alive[0];
						if (old != null && !old.IsDisposed)
							old.Close();
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine(ex);
					}
					alive.RemoveAt(0);
				}

				ctx.ActiveTips = ctx.ActiveTips.Where(t => t != null && !t.IsDisposed).ToList();

				MyTips tip = new MyTips();
				tip._ownerThreadId = threadId;
				tip.panel3.BackgroundImageLayout = ImageLayout.Zoom;

				switch (type)
				{
					case Tipstype.Success:
						tip.BackColor = ColorTranslator.FromHtml("#27AE60");
						tip.panel3.BackgroundImage = tip.imageList1.Images[0];
						break;
					case Tipstype.Warn:
						tip.BackColor = ColorTranslator.FromHtml("#D68910");
						tip.panel3.BackgroundImage = tip.imageList1.Images[1];
						break;
					case Tipstype.Tip:
						tip.BackColor = ColorTranslator.FromHtml("#2980B9");
						tip.panel3.BackgroundImage = tip.imageList1.Images[2];
						break;
					case Tipstype.Error:
						tip.BackColor = ColorTranslator.FromHtml("#C0392B");
						tip.panel3.BackgroundImage = tip.imageList1.Images[3];
						break;
				}

				tip.ForeColor = Color.White;
				tip.ImageIndex = (int)type;
				tip.label1.Text = msg ?? "";
				tip.StartPosition = FormStartPosition.Manual;
				tip.TopMost = true;

				int baseHeight = tip.Height;
				int line = Math.Max(1, tip.GetTextLineCount(tip.label1));

				if (line == 1)
					tip.Height = (int)(baseHeight * 0.7f);
				else
					tip.Height = (int)(baseHeight * (0.7f + (line - 1) * 0.9f));

				Rectangle area = Screen.FromControl(host).WorkingArea;

				int totalOffsetY = 0;
				foreach (var t in ctx.ActiveTips.Where(t => t != null && !t.IsDisposed))
				{
					totalOffsetY += t.Height + Margin;
				}

				int centerX = area.Left + (area.Width - tip.Width) / 2;
				int centerY = area.Top + (area.Height - tip.Height) / 2;
				tip.Location = new Point(centerX, centerY + totalOffsetY);

				ctx.ActiveTips.Add(tip);
				tip.ShowToast(duration);

				return tip;
			}
		}

		#endregion

		#region 动画

		private async void ShowToast(int duration)
		{
			if (IsDisposed) return;

			if (InvokeRequired)
			{
				try
				{
					Invoke(new Action<int>(ShowToast), duration);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex);
				}
				return;
			}

			try
			{
				Opacity = 0;

				if (!Visible)
					base.Show();

				ApplyRound();

				for (double i = 0; i <= 1; i += 0.1)
				{
					await Task.Delay(15);
					if (IsDisposed) return;
					Opacity = i;
				}

				if (duration > 0)
					await Task.Delay(duration);

				if (duration == -1)
					return;

				for (double i = 1; i >= 0; i -= 0.1)
				{
					await Task.Delay(15);
					if (IsDisposed) return;
					Opacity = i;
				}

				if (!IsDisposed)
					Close();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex);
			}
		}

		private void ApplyRound()
		{
			Rectangle rect = ClientRectangle;
			using (GraphicsPath path = new GraphicsPath())
			{
				float r = radius;
				float d = 2 * r;

				path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
				path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
				path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
				path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
				path.CloseFigure();

				Region?.Dispose();
				Region = new Region(path);
			}
		}

		#endregion

		#region 关闭重排

		private void MyTips_FormClosed(object sender, FormClosedEventArgs e)
		{
			var ctx = GetContextByThreadId(_ownerThreadId);
			if (ctx == null) return;

			lock (ctx)
			{
				ctx.ActiveTips.Remove(this);

				Form host = ctx.HostForm;
				Rectangle area = host != null && !host.IsDisposed
					? Screen.FromControl(host).WorkingArea
					: Screen.PrimaryScreen.WorkingArea;

				int offsetY = 0;

				foreach (var tip in ctx.ActiveTips.Where(t => t != null && !t.IsDisposed))
				{
					int centerX = area.Left + (area.Width - tip.Width) / 2;
					int centerY = area.Top + (area.Height - tip.Height) / 2;

					tip.Location = new Point(centerX, centerY + offsetY);
					offsetY += tip.Height + Margin;
				}
			}
		}

		#endregion

		#region 文本行数

		public int GetTextLineCount(Label label)
		{
			using (Graphics g = label.CreateGraphics())
			{
				SizeF size = g.MeasureString(label.Text, label.Font, label.Width);
				return Math.Max(1, (int)Math.Ceiling(size.Height / label.Font.Height));
			}
		}

		#endregion
	}
}
