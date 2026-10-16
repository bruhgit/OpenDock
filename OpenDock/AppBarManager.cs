using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern UIntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        private const int SPI_GETWORKAREA = 0x0030;
        private const int SPI_SETWORKAREA = 0x002F;

        private const uint ABM_GETSTATE = 0x00000004;
        private const int ABS_AUTOHIDE = 0x00000001;

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_BLOCK = 0x0001;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        public const int ABN_STATECHANGE = 0;
        public const int ABN_POSCHANGED = 1;
        public const int ABN_FULLSCREENAPP = 2;
        public const int ABN_WINDOWARRANGE = 3;

        private bool _isRegistered;
        private Rectangle _reservedBounds = Rectangle.Empty;
        private Rectangle? _originalBaseWorkArea = null;
        private volatile bool _isUpdating = false;
        private long _lastUpdateTick = 0;

        public bool IsRegistered => _isRegistered;
        public int CallbackMessage => 0;
        public Rectangle ReservedBounds => _reservedBounds;
        public bool IsUpdating => _isUpdating || (Environment.TickCount64 - Interlocked.Read(ref _lastUpdateTick) < 1200);

        public AppBarManager()
        {
        }

        public void Register(IntPtr hWnd)
        {
        }

        private static bool IsWindowsTaskbarAutoHide()
        {
            try
            {
                APPBARDATA abd = new APPBARDATA
                {
                    cbSize = Marshal.SizeOf<APPBARDATA>()
                };
                uint state = (uint)SHAppBarMessage(ABM_GETSTATE, ref abd);
                return (state & ABS_AUTOHIDE) != 0;
            }
            catch
            {
                return false;
            }
        }

        public Rectangle GetBaseWorkArea(Screen screen)
        {
            Rectangle screenBounds = screen.Bounds;
            try
            {
                // If Windows taskbar is in auto-hide mode, it does not permanently reserve screen work area.
                if (IsWindowsTaskbarAutoHide())
                {
                    return screenBounds;
                }

                IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
                if (taskbarHwnd != IntPtr.Zero && IsWindowVisible(taskbarHwnd))
                {
                    if (GetWindowRect(taskbarHwnd, out RECT tbRect))
                    {
                        Rectangle tb = Rectangle.FromLTRB(tbRect.left, tbRect.top, tbRect.right, tbRect.bottom);
                        if (screenBounds.IntersectsWith(tb))
                        {
                            if (tb.Bottom >= screenBounds.Bottom && tb.Top > screenBounds.Top && tb.Height < screenBounds.Height / 3)
                            {
                                int usableHeight = Math.Max(0, tb.Top - screenBounds.Top);
                                return new Rectangle(screenBounds.Left, screenBounds.Top, screenBounds.Width, usableHeight);
                            }
                            if (tb.Top <= screenBounds.Top && tb.Bottom < screenBounds.Bottom && tb.Height < screenBounds.Height / 3)
                            {
                                int topOffset = tb.Bottom - screenBounds.Top;
                                return new Rectangle(screenBounds.Left, screenBounds.Top + topOffset, screenBounds.Width, screenBounds.Height - topOffset);
                            }
                            if (tb.Left <= screenBounds.Left && tb.Right < screenBounds.Right && tb.Width < screenBounds.Width / 3)
                            {
                                int leftOffset = tb.Right - screenBounds.Left;
                                return new Rectangle(screenBounds.Left + leftOffset, screenBounds.Top, screenBounds.Width - leftOffset, screenBounds.Height);
                            }
                            if (tb.Right >= screenBounds.Right && tb.Left > screenBounds.Left && tb.Width < screenBounds.Width / 3)
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

        private void ApplyWorkAreaAndNotifyAsync(Rectangle targetArea)
        {
            RECT newRect = new RECT
            {
                left = targetArea.Left,
                top = targetArea.Top,
                right = targetArea.Right,
                bottom = targetArea.Bottom
            };

            _isUpdating = true;
            Interlocked.Exchange(ref _lastUpdateTick, Environment.TickCount64);
            try
            {
                // Apply work area in kernel immediately without synchronous HWND_BROADCAST hang
                SystemParametersInfo(SPI_SETWORKAREA, 0, ref newRect, 0);
            }
            finally
            {
                _isUpdating = false;
            }

            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref _lastUpdateTick, Environment.TickCount64);
                    SendMessageTimeout(
                        HWND_BROADCAST,
                        WM_SETTINGCHANGE,
                        new IntPtr(SPI_SETWORKAREA),
                        IntPtr.Zero,
                        SMTO_BLOCK | SMTO_ABORTIFHUNG,
                        60,
                        out _);
                }
                catch { }
                finally
                {
                    Interlocked.Exchange(ref _lastUpdateTick, Environment.TickCount64);
                }
            });
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
                ApplyWorkAreaAndNotifyAsync(targetWorkArea);
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
                Rectangle restoreArea = GetBaseWorkArea(primary);

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
                    Interlocked.Exchange(ref _lastUpdateTick, Environment.TickCount64);
                    try
                    {
                        SystemParametersInfo(SPI_SETWORKAREA, 0, ref newRect, 0);
                        SendMessageTimeout(
                            HWND_BROADCAST,
                            WM_SETTINGCHANGE,
                            new IntPtr(SPI_SETWORKAREA),
                            IntPtr.Zero,
                            SMTO_BLOCK | SMTO_ABORTIFHUNG,
                            60,
                            out _);
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
