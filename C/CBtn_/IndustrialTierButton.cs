using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FrmControl.IndustrialUI
{
    public enum ButtonTier
    {
        TierA_Process = 0,   // Start / Stop
        TierB_Function = 1,  // Settings / Preferences
        TierC_System = 2     // Back / Exit / SaveAs
    }

    public enum ButtonRole
    {
        Normal = 0,
        Start = 1,
        Stop = 2
    }

    public enum RunState
    {
        Idle = 0,
        Running = 1,
        Fault = 2
    }

    /// <summary>
    /// WinForms industrial button drawn with GDI+ (no animations).
    /// </summary>
    [DefaultEvent(nameof(Click))]
    public class IndustrialTierButton : Control
    {
        private bool _hover;
        private bool _pressed;

        private ButtonTier _tier = ButtonTier.TierB_Function;
        private ButtonRole _role = ButtonRole.Normal;
        private RunState _runState = RunState.Idle;

        private bool _lockWhenRunning;
        private int _cornerRadius = 4;

        private int _iconSize = 18;
        private int _iconTextGap = 10;
        private Image? _icon;

        // Palette (industrial safe)
        private Color _back = Color.FromArgb(0x3C, 0x3F, 0x41);
        private Color _backHover = Color.FromArgb(0x4A, 0x4E, 0x50);
        private Color _backPressed = Color.FromArgb(0x2B, 0x2E, 0x30);
        private Color _backDisabled = Color.FromArgb(0x55, 0x55, 0x55);

        private Color _border = Color.FromArgb(0x5A, 0x5E, 0x60);
        private Color _text = Color.FromArgb(0xE6, 0xE6, 0xE6);
        private Color _textDisabled = Color.FromArgb(0xA0, 0xA0, 0xA0);

        private Color _startAccent = Color.FromArgb(0x2E, 0xCC, 0x71);
        private Color _stopAccent = Color.FromArgb(0xE7, 0x4C, 0x3C);
        private Color _faultAccent = Color.FromArgb(0xF3, 0x9C, 0x12);

        private Padding _contentPadding = new Padding(12, 0, 12, 0);

        public IndustrialTierButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.SupportsTransparentBackColor|
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);

            TabStop = true;
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(120, 42);

            ApplyTierDefaults();
            UpdateEnabledByRunState();
        }
		private ContentAlignment _textAlign = ContentAlignment.MiddleLeft;

		[Category("IndustrialUI")]
		[DefaultValue(typeof(ContentAlignment), "MiddleLeft")]
		public ContentAlignment FrmTextAlign
		{
			get => _textAlign;
			set { _textAlign = value; Invalidate(); }
		}

		// -------------------- Public properties --------------------

		[Category("IndustrialUI")]
        public ButtonTier Tier
        {
            get => _tier;
            set { _tier = value; ApplyTierDefaults(); Invalidate(); }
        }

        [Category("IndustrialUI")]
        public ButtonRole Role
        {
            get => _role;
            set { _role = value; ApplyTierDefaults(); Invalidate(); }
        }

        [Category("IndustrialUI")]
        public RunState RunState
        {
            get => _runState;
            set { _runState = value; UpdateEnabledByRunState(); Invalidate(); }
        }

        /// <summary>
        /// If true, button is disabled when RunState == Running.
        /// Use for Settings/SaveAs/etc.
        /// </summary>
        [Category("IndustrialUI")]
        public bool LockWhenRunning
        {
            get => _lockWhenRunning;
            set { _lockWhenRunning = value; UpdateEnabledByRunState(); Invalidate(); }
        }

        [Category("IndustrialUI")]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("IndustrialUI")]
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = Math.Max(8, value); Invalidate(); }
        }

        [Category("IndustrialUI")]
        public int IconTextGap
        {
            get => _iconTextGap;
            set { _iconTextGap = Math.Max(0, value); Invalidate(); }
        }

        [Category("IndustrialUI")]
        public Image? IconImage
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        [Category("IndustrialUI")]
        public Padding ContentPadding
        {
            get => _contentPadding;
            set { _contentPadding = value; Invalidate(); }
        }

        // Optional: expose palette if you want to tune globally
        [Category("IndustrialUI")] public Color BaseBack { get => _back; set { _back = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color BaseBackHover { get => _backHover; set { _backHover = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color BaseBackPressed { get => _backPressed; set { _backPressed = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color BaseBackDisabled { get => _backDisabled; set { _backDisabled = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color BaseBorder { get => _border; set { _border = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color TextColor { get => _text; set { _text = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color TextDisabledColor { get => _textDisabled; set { _textDisabled = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color StartAccent { get => _startAccent; set { _startAccent = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color StopAccent { get => _stopAccent; set { _stopAccent = value; Invalidate(); } }
        [Category("IndustrialUI")] public Color FaultAccent { get => _faultAccent; set { _faultAccent = value; Invalidate(); } }

        // -------------------- Tier defaults --------------------

        private void ApplyTierDefaults()
        {
            switch (_tier)
            {
                case ButtonTier.TierA_Process:
                  //  Size = new Size(140, 48);
                //    Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold, GraphicsUnit.Point);
                    _cornerRadius = Math.Max(_cornerRadius, 4);
                    break;

                case ButtonTier.TierB_Function:
                 //   Size = new Size(120, 42);
                 //   Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
                    _cornerRadius = Math.Max(3, _cornerRadius);
                    break;

                case ButtonTier.TierC_System:
                 //   Size = new Size(120, 40);
                 //   Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
                    _cornerRadius = Math.Max(3, _cornerRadius);
                    // Lower emphasis by default (slightly darker)
                    _back = Color.FromArgb(0x36, 0x39, 0x3B);
                    _backHover = Color.FromArgb(0x42, 0x46, 0x49);
                    _backPressed = Color.FromArgb(0x2A, 0x2D, 0x2F);
                    _border = Color.FromArgb(0x50, 0x54, 0x56);
                    break;
            }

            // Restore default B/A palette if not TierC
            if (_tier != ButtonTier.TierC_System)
            {
                _back = Color.FromArgb(0x3C, 0x3F, 0x41);
                _backHover = Color.FromArgb(0x4A, 0x4E, 0x50);
                _backPressed = Color.FromArgb(0x2B, 0x2E, 0x30);
                _border = Color.FromArgb(0x5A, 0x5E, 0x60);
            }

            UpdateEnabledByRunState();
        }

        // -------------------- RunState gating (no animation) --------------------

        private void UpdateEnabledByRunState()
        {
            bool enabled;

            // Typical ATE logic:
            // Idle: Start enabled, Stop disabled
            // Running/Fault: Stop enabled, Start disabled
            if (_role == ButtonRole.Start)
                enabled = (_runState == RunState.Idle);
            else if (_role == ButtonRole.Stop)
                enabled = (_runState != RunState.Idle);
            else
                enabled = !(_lockWhenRunning && _runState == RunState.Running);

            base.Enabled = enabled;
        }

        // -------------------- Input (no animations) --------------------

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Enabled && e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Focus();
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (Enabled && e.Button == MouseButtons.Left)
            {
                bool wasPressed = _pressed;
                _pressed = false;
                Invalidate();

                // Click if mouse is still inside
                if (wasPressed && ClientRectangle.Contains(e.Location))
                    OnClick(EventArgs.Empty);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled) return;

            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (!Enabled) return;

            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && _pressed)
            {
                _pressed = false;
                Invalidate();
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        // -------------------- Paint --------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color back = _back;
            if (!Enabled) back = _backDisabled;
            else if (_pressed) back = _backPressed;
            else if (_hover) back = _backHover;

            // Accent selection
            Color accent = Color.Empty;
            if (_role == ButtonRole.Start) accent = _startAccent;
            else if (_role == ButtonRole.Stop) accent = _stopAccent;

            // Optional: fault tint for Stop
            if (_runState == RunState.Fault && _role == ButtonRole.Stop)
                accent = _faultAccent;

            // Border thickness
            int borderThickness = (_tier == ButtonTier.TierA_Process) ? 2 : 1;
            Color borderColor = (accent != Color.Empty) ? accent : _border;

            // Dim border when disabled
            if (!Enabled)
            {
                borderColor = Color.FromArgb(140, borderColor);
            }

            using (GraphicsPath path = RoundedRect(rect, _cornerRadius))
            using (SolidBrush b = new SolidBrush(back))
            using (Pen p = new Pen(borderColor, borderThickness))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }

            DrawContent(g, rect, accent);
            DrawFocus(g, rect);
        }

        private void DrawContent(Graphics g, Rectangle rect, Color accent)
        {
            Color textColor = Enabled ? _text : _textDisabled;
            Color iconColor = (accent != Color.Empty) ? accent : textColor;
            if (!Enabled && accent == Color.Empty) iconColor = _textDisabled;

            int left = _contentPadding.Left;
            int right = rect.Width - _contentPadding.Right;

            int iconSize = _iconSize;
            Rectangle iconRect = new Rectangle(
                left,
                rect.Top + (rect.Height - iconSize) / 2,
                iconSize,
                iconSize);

            int textX = iconRect.Right + _iconTextGap;
            Rectangle textRect = new Rectangle(
                textX,
                rect.Top,
                Math.Max(0, right - textX),
                rect.Height);

            // Icon
            if (_icon != null)
            {
                g.DrawImage(_icon, iconRect);
            }
            else
            {
                DrawDefaultSymbol(g, iconRect, iconColor);
            }

			TextFormatFlags flags = GetTextFormatFlags(_textAlign);
			flags |= TextFormatFlags.EndEllipsis;
			flags |= TextFormatFlags.NoPrefix;   // 防止 & 被当成快捷键
			flags |= TextFormatFlags.SingleLine; // 工业按钮通常单行

			TextRenderer.DrawText(g, Text ?? string.Empty, Font, textRect, textColor, flags);


		}
		private static TextFormatFlags GetTextFormatFlags(ContentAlignment align)
		{
			TextFormatFlags flags = TextFormatFlags.Default;

			// Horizontal
			switch (align)
			{
				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
					flags |= TextFormatFlags.Left;
					break;

				case ContentAlignment.TopCenter:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.BottomCenter:
					flags |= TextFormatFlags.HorizontalCenter;
					break;

				case ContentAlignment.TopRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.BottomRight:
					flags |= TextFormatFlags.Right;
					break;
			}

			// Vertical
			switch (align)
			{
				case ContentAlignment.TopLeft:
				case ContentAlignment.TopCenter:
				case ContentAlignment.TopRight:
					flags |= TextFormatFlags.Top;
					break;

				case ContentAlignment.MiddleLeft:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.MiddleRight:
					flags |= TextFormatFlags.VerticalCenter;
					break;

				case ContentAlignment.BottomLeft:
				case ContentAlignment.BottomCenter:
				case ContentAlignment.BottomRight:
					flags |= TextFormatFlags.Bottom;
					break;
			}

			return flags;
		}

		private void DrawDefaultSymbol(Graphics g, Rectangle r, Color c)
        {
            using (SolidBrush b = new SolidBrush(c))
            {
                if (_role == ButtonRole.Start)
                {
                    // ▶ triangle
                    PointF p1 = new PointF(r.Left + 4, r.Top + 3);
                    PointF p2 = new PointF(r.Left + 4, r.Bottom - 3);
                    PointF p3 = new PointF(r.Right - 3, r.Top + r.Height / 2f);
                    g.FillPolygon(b, new[] { p1, p2, p3 });
                }
                else if (_role == ButtonRole.Stop)
                {
                    // ■ square
                    Rectangle sq = Rectangle.Inflate(r, -4, -4);
                    g.FillRectangle(b, sq);
                }
                else
                {
                    // ● subtle dot
                    Rectangle dot = new Rectangle(r.Left + r.Width / 2 - 3, r.Top + r.Height / 2 - 3, 6, 6);
                    g.FillEllipse(b, dot);
                }
            }
        }

        private void DrawFocus(Graphics g, Rectangle rect)
        {
            if (!Focused || !ShowFocusCues) return;

            Rectangle focus = Rectangle.Inflate(rect, -4, -4);
            using (Pen p = new Pen(Color.FromArgb(140, 255, 255, 255), 1))
            {
                p.DashStyle = DashStyle.Dot;
                g.DrawRectangle(p, focus);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int r = Math.Max(0, radius);
            GraphicsPath path = new GraphicsPath();

            if (r == 0)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            int d = r * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(d, d));

            path.AddArc(arc, 180, 90);                // TL
            arc.X = bounds.Right - d; path.AddArc(arc, 270, 90); // TR
            arc.Y = bounds.Bottom - d; path.AddArc(arc, 0, 90);  // BR
            arc.X = bounds.Left; path.AddArc(arc, 90, 90);       // BL
            path.CloseFigure();

            return path;
        }

        // Helpful quick config methods
        public void ConfigureAsStart(string text = "启 动")
        {
            Tier = ButtonTier.TierA_Process;
            Role = ButtonRole.Start;
            Text = text;
            LockWhenRunning = false;
        }

        public void ConfigureAsStop(string text = "停 止")
        {
            Tier = ButtonTier.TierA_Process;
            Role = ButtonRole.Stop;
            Text = text;
            LockWhenRunning = false;
        }
    }
}
