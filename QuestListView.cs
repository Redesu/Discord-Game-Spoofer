using System.Runtime.InteropServices;

namespace DiscordGameSpoofer
{
    /// <summary>
    /// Details view list that repaints the strip below the last row.
    /// The native control draws its column separator lines all the way to the
    /// bottom of the control, which looks broken on the dark theme.
    /// </summary>
    public sealed class QuestListView : ListView
    {
        private const int WmPaint = 0x000F;
        private const int LvmGetHeader = 0x101F;

        private Color _emptyAreaColor = Color.Empty;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public void SetEmptyAreaColor(Color color)
        {
            _emptyAreaColor = color;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WmPaint)
                PaintEmptyArea();
        }

        private void PaintEmptyArea()
        {
            if (_emptyAreaColor.IsEmpty || !IsHandleCreated)
                return;

            Rectangle client = ClientRectangle;
            int top = GetContentTop();

            if (top >= client.Bottom)
                return;

            using Graphics graphics = Graphics.FromHwnd(Handle);
            using SolidBrush brush = new SolidBrush(_emptyAreaColor);

            graphics.FillRectangle(brush, client.Left, top, client.Width, client.Bottom - top);
        }

        /// <summary>
        /// Bottom edge of the painted content: the last row, or the header when
        /// the list has no rows yet.
        /// </summary>
        private int GetContentTop()
        {
            if (Items.Count > 0)
                return GetItemRect(Items.Count - 1).Bottom;

            IntPtr header = SendMessage(Handle, LvmGetHeader, IntPtr.Zero, IntPtr.Zero);

            if (header == IntPtr.Zero)
                return 0;

            if (!GetWindowRect(header, out NativeRect headerRect) || !GetWindowRect(Handle, out NativeRect listRect))
                return 0;

            return headerRect.Bottom - listRect.Top;
        }
    }
}
