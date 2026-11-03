using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace DiscordGameSpoofer
{
    /// <summary>
    /// Check box drawn by hand so the glyph stays visible on the dark theme.
    /// The native flat check box paints a black glyph on some Windows themes.
    /// </summary>
    public sealed class DarkCheckBox : CheckBox
    {
        private static readonly Color BoxColor = Color.FromArgb(24, 32, 50);
        private static readonly Color BorderColor = Color.FromArgb(71, 85, 105);
        private static readonly Color HoverBorderColor = Color.FromArgb(56, 189, 248);
        private static readonly Color CheckedColor = Color.FromArgb(14, 165, 233);
        private static readonly Color CheckMarkColor = Color.White;
        private static readonly Color DisabledFillColor = Color.FromArgb(30, 41, 59);
        private static readonly Color DisabledBorderColor = Color.FromArgb(51, 65, 85);
        private static readonly Color DisabledTextColor = Color.FromArgb(100, 116, 139);

        private bool _hovered;
        private bool _pressed;

        public DarkCheckBox()
        {
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.FromArgb(10, 14, 23);
            Cursor = Cursors.Hand;
            ForeColor = Color.FromArgb(226, 232, 240);
        }

        [DefaultValue(18)]
        public int BoxSize { get; set; } = 18;

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int boxSize = Math.Min(BoxSize, Math.Max(10, Height - 6));
            int boxTop = (Height - boxSize) / 2;
            Rectangle box = new Rectangle(1, boxTop, boxSize, boxSize);

            bool enabled = Enabled;
            bool isChecked = CheckState == CheckState.Checked;
            bool isIndeterminate = CheckState == CheckState.Indeterminate;

            Color borderColor = !enabled
                ? DisabledBorderColor
                : isChecked || isIndeterminate
                    ? CheckedColor
                    : _hovered
                        ? HoverBorderColor
                        : BorderColor;

            Color fillColor = !enabled
                ? DisabledFillColor
                : isChecked || isIndeterminate
                    ? CheckedColor
                    : _pressed
                        ? Color.FromArgb(30, 41, 59)
                        : BoxColor;

            using (GraphicsPath path = CreateRoundedRectangle(box, 4))
            using (SolidBrush fillBrush = new SolidBrush(fillColor))
            using (Pen borderPen = new Pen(borderColor, 1.4f))
            {
                e.Graphics.FillPath(fillBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            if (isChecked)
            {
                using Pen checkPen = new Pen(CheckMarkColor, 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round,
                };

                float left = box.Left + box.Width * 0.24f;
                float middleX = box.Left + box.Width * 0.44f;
                float right = box.Right - box.Width * 0.22f;
                float top = box.Top + box.Height * 0.22f;
                float middleY = box.Top + box.Height * 0.54f;
                float bottom = box.Bottom - box.Height * 0.22f;

                e.Graphics.DrawLines(
                    checkPen,
                    new[]
                    {
                        new PointF(left, middleY),
                        new PointF(middleX, bottom),
                        new PointF(right, top),
                    });
            }
            else if (isIndeterminate)
            {
                using Pen dashPen = new Pen(CheckMarkColor, 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };

                float left = box.Left + box.Width * 0.24f;
                float right = box.Right - box.Width * 0.24f;
                float middleY = box.Top + box.Height * 0.5f;

                e.Graphics.DrawLine(dashPen, left, middleY, right, middleY);
            }

            int textLeft = box.Right + 8;
            Rectangle textBounds = new Rectangle(
                textLeft,
                0,
                Math.Max(0, Width - textLeft - 1),
                Height);

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textBounds,
                enabled ? ForeColor : DisabledTextColor,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);

            if (enabled && Focused && ShowFocusCues)
            {
                Rectangle focusBounds = Rectangle.Inflate(textBounds, -2, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, ForeColor, BackColor);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            Invalidate();
            base.OnCheckedChanged(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
                Invalidate();

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
                Invalidate();

            base.OnKeyUp(e);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();

            if (diameter <= 1)
            {
                path.AddRectangle(bounds);
                return path;
            }

            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
