using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public static class FlowLayoutPanelDragScrollExtensionsNet48
{
	/// <summary>
	/// 让 FlowLayoutPanel 支持：按住内部控件拖拽 -> 滚动（且轻点仍可触发 Click/MouseClick）
	/// 适用于 .NET Framework 4.8（net48）
	/// </summary>
	public static void EnableDragScrollNet48(
		this FlowLayoutPanel panel,
		bool enableHorizontal = false,
		bool enableVertical = true,
		int activateThresholdPx = 6)
	{
		if (panel == null) throw new ArgumentNullException(nameof(panel));

		panel.AutoScroll = true;

		var state = new DragScrollState(panel, enableHorizontal, enableVertical, activateThresholdPx);

		// 绑定 panel 自己（空白处也能拖）
		state.AttachTo(panel);

		// 绑定现有子控件（递归）
		AttachRecursive(panel, state);

		// 新增控件自动绑定
		panel.ControlAdded += (_, e) => AttachRecursive(e.Control, state);

		// 移除控件解绑（避免动态增删导致事件残留）
		panel.ControlRemoved += (_, e) => DetachRecursive(e.Control, state);
	}

	private static void AttachRecursive(Control root, DragScrollState state)
	{
		if (root == null) return;
		state.AttachTo(root);
		foreach (Control c in root.Controls)
			AttachRecursive(c, state);
	}

	private static void DetachRecursive(Control root, DragScrollState state)
	{
		if (root == null) return;
		state.DetachFrom(root);
		foreach (Control c in root.Controls)
			DetachRecursive(c, state);
	}

	private sealed class DragScrollState
	{
		private readonly FlowLayoutPanel _panel;
		private readonly bool _enableH;
		private readonly bool _enableV;
		private readonly int _threshold;

		private bool _mouseDown;
		private bool _dragActivated;

		private Point _startScreen;
		private int _startV;
		private int _startH;

		// 记录“按下时在哪个控件上”，用于结束时释放 Capture
		private Control _downControl;

		// 防止重复挂事件
		private readonly HashSet<IntPtr> _attached = new HashSet<IntPtr>();

		public DragScrollState(FlowLayoutPanel panel, bool enableH, bool enableV, int threshold)
		{
			_panel = panel;
			_enableH = enableH;
			_enableV = enableV;
			_threshold = Math.Max(0, threshold);
		}

		public void AttachTo(Control c)
		{
			if (c == null) return;
			if (!_attached.Add(c.Handle)) return;

			c.MouseDown += OnMouseDown;
			c.MouseMove += OnMouseMove;
			c.MouseUp += OnMouseUp;
		}

		public void DetachFrom(Control c)
		{
			if (c == null) return;
			if (!_attached.Remove(c.Handle)) return;

			c.MouseDown -= OnMouseDown;
			c.MouseMove -= OnMouseMove;
			c.MouseUp -= OnMouseUp;
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;

			_mouseDown = true;
			_dragActivated = false;

			_downControl = sender as Control;
			_startScreen = Control.MousePosition;

			_startV = _panel.VerticalScroll.Value;
			_startH = _panel.HorizontalScroll.Value;

			// 先让“按下的控件”捕获鼠标：轻点时 Click/MouseClick 才能正常走完
			if (_downControl != null)
				_downControl.Capture = true;
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!_mouseDown) return;

			var now = Control.MousePosition;
			int dx = now.X - _startScreen.X;
			int dy = now.Y - _startScreen.Y;

			// 未激活拖拽：先判断是否超过阈值
			if (!_dragActivated)
			{
				if (Math.Abs(dx) < _threshold && Math.Abs(dy) < _threshold)
					return;

				_dragActivated = true;

				// 🔥关键：一旦进入拖拽滚动，把 Capture 切到 panel
				// 这样 MouseUp 不会回到子控件，子控件就不会触发 Click/MouseClick
				if (_downControl != null) _downControl.Capture = false;
				_panel.Capture = true;
			}

			// 拖拽滚动：手指向下拖 => Scroll.Value 减小（内容随手指方向移动的“跟手感”）
			if (_enableV)
				SetVerticalScroll(_startV - dy);

			if (_enableH)
				SetHorizontalScroll(_startH - dx);
		}

		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (!_mouseDown) return;
			_mouseDown = false;

			// 释放 Capture
			if (_panel.Capture) _panel.Capture = false;
			if (_downControl != null && _downControl.Capture) _downControl.Capture = false;

			_downControl = null;
		}

		private void SetVerticalScroll(int value)
		{
			var s = _panel.VerticalScroll;
			int min = s.Minimum;
			int max = Math.Max(min, s.Maximum - s.LargeChange + 1);

			int v = Math.Max(min, Math.Min(max, value));
			if (v == s.Value) return;

			s.Value = v;
			_panel.PerformLayout();
			_panel.Invalidate();
		}

		private void SetHorizontalScroll(int value)
		{
			var s = _panel.HorizontalScroll;
			int min = s.Minimum;
			int max = Math.Max(min, s.Maximum - s.LargeChange + 1);

			int h = Math.Max(min, Math.Min(max, value));
			if (h == s.Value) return;

			s.Value = h;
			_panel.PerformLayout();
			_panel.Invalidate();
		}
	}
}
