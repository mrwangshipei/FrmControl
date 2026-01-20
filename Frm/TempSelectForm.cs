using System;
using System.Drawing;
using System.Windows.Forms;

public class TempSelectForm : Form
{
	private readonly Action<string> _onSelected;

	private const int MaxFormHeight = 240;
	private const int ButtonHeight = 32;
	private const int DragThreshold = 4;

	private FlowLayoutPanel _panel;

	private bool _dragging;
	private Point _dragStart;
	private int _startScrollY;

	public TempSelectForm(string[] options, Action<string> onSelected)
	{
		_onSelected = onSelected;

		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		TopMost = true;
		StartPosition = FormStartPosition.Manual;
		BackColor = Color.White;

		_panel = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true,
			Dock = DockStyle.Fill
		};

		BindDragEvents(_panel);

		foreach (var option in options)
		{
			var btn = CreateFlatButton(option);
			_panel.Controls.Add(btn);
			BindDragEvents(btn);
		}

		Controls.Add(_panel);
		_panel.SizeChanged += (s, e) =>
		{
			foreach (Control c in _panel.Controls)
				c.Width = _panel.ClientSize.Width;
		};

		int totalHeight = options.Length * ButtonHeight;
		Height = Math.Min(totalHeight, MaxFormHeight);
		Width = 180;

		Deactivate += (s, e) => Close();
	}

	private Button CreateFlatButton(string text)
	{
		var btn = new Button
		{
			Text = text,
			Height = ButtonHeight,
			Width = _panel.ClientSize.Width,
			Margin = new Padding(0),
			FlatStyle = FlatStyle.Flat,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(10, 0, 0, 0)
		};


		btn.FlatAppearance.BorderSize = 0;
		btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
		btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 220, 220);

		btn.Click += (s, e) =>
		{
			if (_dragging) return; // 拖动时不触发点击
			_onSelected?.Invoke(text);
			Close();
		};

		return btn;
	}

	private void BindDragEvents(Control control)
	{
		control.MouseDown += OnMouseDown;
		control.MouseMove += OnMouseMove;
		control.MouseUp += OnMouseUp;
	}

	private void OnMouseDown(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Left) return;

		_dragging = false;
		_dragStart = Cursor.Position;
		_startScrollY = _panel.VerticalScroll.Value;
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if ((e.Button & MouseButtons.Left) == 0) return;

		int delta = Cursor.Position.Y - _dragStart.Y;

		if (!_dragging && Math.Abs(delta) > DragThreshold)
			_dragging = true;

		if (_dragging)
		{
			int newValue = _startScrollY - delta;
			newValue = Math.Max(0, Math.Min(
				newValue,
				_panel.VerticalScroll.Maximum));

			_panel.VerticalScroll.Value = newValue;
			_panel.PerformLayout();
		}
	}

	private void OnMouseUp(object sender, MouseEventArgs e)
	{
		_dragging = false;
	}
}
