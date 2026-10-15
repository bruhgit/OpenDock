using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OpenDock
{
    internal sealed class AppBarManager
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int SPI_GETWORKAREA = 0x0030;
        private const int SPI_SETWORKAREA = 0x002F;
        private const int SPIF_SENDCHANGE = 0x0002;

        public const int ABN_STATECHANGE = 0;
        public const int ABN_POSCHANGED = 1;
        public const int ABN_FULLSCREENAPP = 2;
        public const int ABN_WINDOWARRANGE = 3;

        private bool _isRegistered;
        private Rectangle _reservedBounds = Rectangle.Empty;
        private Rectangle? _originalBaseWorkArea = null;
        private bool _isUpdating = false;

        public bool IsRegistered => _isRegistered;
        public int CallbackMessage => 0;
        public Rectangle ReservedBounds => _reservedBounds;
        public bool IsUpdating => _isUpdating;

        public AppBarManager()
        {
        }

        public void Register(IntPtr hWnd)
        {
        }

        public Rectangle GetBaseWorkArea(Screen screen)
        {
            Rectangle screenBounds = screen.Bounds;
            try
            {
                IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
                if (taskbarHwnd != IntPtr.Zero && IsWindowVisible(taskbarHwnd))
                {
                    if (GetWindowRect(taskbarHwnd, out RECT tbRect))
                    {
                        Rectangle tb = Rectangle.FromLTRB(tbRect.left, tbRect.top, tbRect.right, tbRect.bottom);
                        if (screenBounds.IntersectsWith(tb))
                        {
                            if (tb.Bottom >= screenBounds.Bottom && tb.Top > screenBounds.Top)
                            {
                                int usableHeight = Math.Max(0, tb.Top - screenBounds.Top);
                                return new Rectangle(screenBounds.Left, screenBounds.Top, screenBounds.Width, usableHeight);
                            }
                            if (tb.Top <= screenBounds.Top && tb.Bottom < screenBounds.Bottom)
                            {
                                int topOffset = tb.Bottom - screenBounds.Top;
                                return new Rectangle(screenBounds.Left, screenBounds.Top + topOffset, screenBounds.Width, screenBounds.Height - topOffset);
                            }
                            if (tb.Left <= screenBounds.Left && tb.Right < screenBounds.Right)
                            {
                                int leftOffset = tb.Right - screenBounds.Left;
                                return new Rectangle(screenBounds.Left + leftOffset, screenBounds.Top, screenBounds.Width - leftOffset, screenBounds.Height);
                            }
                            if (tb.Right >= screenBounds.Right && tb.Left > screenBounds.Left)
                            {
                                int usableWidth = Math.Max(0, tb.Left - screenBounds.Left);
                                return new Rectangle(screenBounds.Left, screenBounds.Top, usableWidth, screenBounds.Height);
                            }
                        }
                    }
                }
            }
            catch { }

            if (_originalBaseWorkArea.HasValue && !_isRegistered)
            {
                return _originalBaseWorkArea.Value;
            }
            return screenBounds;
        }

        public Rectangle UpdateReservation(string position, int thickness, Screen screen)
        {
            Rectangle baseArea = GetBaseWorkArea(screen);
            if (!_originalBaseWorkArea.HasValue)
            {
                _originalBaseWorkArea = baseArea;
            }

            Rectangle targetWorkArea = baseArea;
            string pos = position ?? "Bottom";

            if (pos.Equals("Top", StringComparison.OrdinalIgnoreCase))
            {
                targetWorkArea = new Rectangle(baseArea.Left, baseArea.Top + thickness, baseArea.Width, Math.Max(100, baseArea.Height - thickness));
            }
            else if (pos.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                targetWorkArea = new Rectangle(baseArea.Left + thickness, baseArea.Top, Math.Max(100, baseArea.Width - thickness), baseArea.Height);
            }
            else if (pos.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                targetWorkArea = new Rectangle(baseArea.Left, baseArea.Top, Math.Max(100, baseArea.Width - thickness), baseArea.Height);
            }
            else // Bottom
            {
                targetWorkArea = new Rectangle(baseArea.Left, baseArea.Top, baseArea.Width, Math.Max(100, baseArea.Height - thickness));
            }

            RECT currentRect = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref currentRect, 0);
            Rectangle currentWorkArea = Rectangle.FromLTRB(currentRect.left, currentRect.top, currentRect.right, currentRect.bottom);

            if (currentWorkArea != targetWorkArea)
            {
                RECT newRect = new RECT
                {
                    left = targetWorkArea.Left,
                    top = targetWorkArea.Top,
                    right = targetWorkArea.Right,
                    bottom = targetWorkArea.Bottom
                };

                _isUpdating = true;
                try
                {
                    SystemParametersInfo(SPI_SETWORKAREA, 0, ref newRect, SPIF_SENDCHANGE);
                }
                finally
                {
                    _isUpdating = false;
                }
            }

            _isRegistered = true;
            _reservedBounds = targetWorkArea;
            return targetWorkArea;
        }

        public void Unregister()
        {
            if (!_isRegistered && !_originalBaseWorkArea.HasValue) return;

            try
            {
                Screen primary = Screen.PrimaryScreen ?? Screen.AllScreens[0];
                Rectangle restoreArea = _originalBaseWorkArea ?? GetBaseWorkArea(primary);

                RECT currentRect = new RECT();
                SystemParametersInfo(SPI_GETWORKAREA, 0, ref currentRect, 0);
                Rectangle currentWorkArea = Rectangle.FromLTRB(currentRect.left, currentRect.top, currentRect.right, currentRect.bottom);

                if (currentWorkArea != restoreArea)
                {
                    RECT newRect = new RECT
                    {
                        left = restoreArea.Left,
                        top = restoreArea.Top,
                        right = restoreArea.Right,
                        bottom = restoreArea.Bottom
                    };

                    _isUpdating = true;
                    try
                    {
                        SystemParametersInfo(SPI_SETWORKAREA, 0, ref newRect, SPIF_SENDCHANGE);
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                }
            }
            catch { }
            finally
            {
                _isRegistered = false;
                _reservedBounds = Rectangle.Empty;
                _originalBaseWorkArea = null;
            }
        }
    }
}
