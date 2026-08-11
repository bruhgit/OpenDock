using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenDock
{
    internal sealed class OpenDockSettings
    {
        public int DockColorArgb { get; set; } = Color.FromArgb(175, 24, 24, 24).ToArgb();
        public int MenuColorArgb { get; set; } = Color.FromArgb(175, 24, 24, 24).ToArgb();
        public int SearchColorArgb { get; set; } = Color.FromArgb(45, 45, 45).ToArgb();
        public string MenuLogoPath { get; set; } = "";
        public string DockPosition { get; set; } = "Bottom";
        public bool GameModeEnabled { get; set; } = false;
    }

    public partial class Form1 : Form
    {
        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS { public int Left, Right, Top, Bottom; }

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, System.Text.StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_NOACTIVATE = 0x08000000; // Prevents the window from being activated on click

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        private static string GetWindowClassName(IntPtr hWnd)
        {
            var className = new System.Text.StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            var title = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            return title.ToString();
        }

        private static string GetProcessNameFromPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return "";
                return System.IO.Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
                return "";
            }
        }

        private static Icon? _genericAppIcon;
        public static Icon GetGenericApplicationIcon()
        {
            if (_genericAppIcon != null) return _genericAppIcon;

            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
                const uint SHGFI_ICON = 0x000000100;
                const uint FILE_ATTRIBUTE_NORMAL = 0x80;

                IntPtr hImg = SHGetFileInfo(".exe", FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_USEFILEATTRIBUTES);

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    _genericAppIcon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                    DestroyIcon(shinfo.hIcon);
                    return _genericAppIcon;
                }
            }
            catch { }

            return _genericAppIcon = SystemIcons.WinLogo;
        }

        private static string GetProcessFilePath(Process proc)
        {
            string procName = proc.ProcessName;
            if (procName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
            {
                string windir = Environment.GetEnvironmentVariable("windir") ?? "C:\\Windows";
                return System.IO.Path.Combine(windir, "explorer.exe");
            }

            try
            {
                return proc.MainModule?.FileName ?? "";
            }
            catch
            {
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, proc.Id);
                if (hProcess != IntPtr.Zero)
                {
                    try
                    {
                        int size = 1024;
                        var buffer = new System.Text.StringBuilder(size);
                        if (QueryFullProcessImageName(hProcess, 0, buffer, ref size))
                        {
                            return buffer.ToString();
                        }
                    }
                    finally
                    {
                        CloseHandle(hProcess);
                    }
                }
            }

            string fallbackPath = GetFallbackExecutablePath(procName);
            return System.IO.File.Exists(fallbackPath) ? fallbackPath : "";
        }

        private static bool IsValidExecutablePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? uri) && !uri.IsFile)
            {
                return true;
            }

            return System.IO.File.Exists(path);
        }

        private static string GetFallbackExecutablePath(string processName)
        {
            string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            if (processName.Equals("notepad", StringComparison.OrdinalIgnoreCase))
            {
                return System.IO.Path.Combine(windowsPath, "System32", "notepad.exe");
            }

            if (processName.Equals("notepad++", StringComparison.OrdinalIgnoreCase))
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                return System.IO.Path.Combine(programFiles, "Notepad++", "notepad++.exe");
            }

            return "";
        }

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private const int DWMWA_CLOAKED = 14;

        private static bool ShouldShowProcessWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || hWnd == GetShellWindow()) return false;
            if (!IsWindowVisible(hWnd)) return false;

            long exStyle = GetWindowLong(hWnd, GWL_EXSTYLE).ToInt64();

            // WS_EX_TOOLWINDOW without WS_EX_APPWINDOW = hidden utility/helper window
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
            {
                return false;
            }

            // Owned windows are usually child dialogs — skip them,
            // UNLESS they explicitly have WS_EX_APPWINDOW (some apps like Notepad++ do this)
            IntPtr owner = GetWindow(hWnd, GW_OWNER);
            if (owner != IntPtr.Zero && (exStyle & WS_EX_APPWINDOW) == 0)
            {
                return false;
            }

            // Check if window is empty or off-screen
            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0) return false;
            }

            // Check if window is cloaked by DWM (suspended UWP apps, off-screen hosts, virtual desktop hidden windows)
            if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0)
            {
                if (cloaked != 0) return false;
            }

            return true;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        internal const uint SHGFI_ICON = 0x000000100;
        internal const uint SHGFI_LARGEICON = 0x000000000;

        // Her dock ikonu iin ayr, bamsz (top-level) bir pencere.
        // Parent Form'un Region/clip snrlarna taklmadan taabilmesi iin
        // PictureBox' dorudan Form1'e eklemek yerine kendi penceresine koyuyoruz.
        private class IconWindow : Form
        {
            private Bitmap? _originalBitmap; // asla kltlmemi, her zaman kaliteli kaynak
            private Bitmap? _bitmap;         // o an ekranda gsterilen, leklenmi versiyon
            private string _appName = "";
            private float _scrollOffset = 0f;

            [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

            [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern bool DeleteObject(IntPtr hObject);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern bool DeleteDC(IntPtr hdc);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFOHEADER
            {
                public uint biSize;
                public int biWidth;
                public int biHeight;
                public ushort biPlanes;
                public ushort biBitCount;
                public uint biCompression;
                public uint biSizeImage;
                public int biXPelsPerMeter;
                public int biYPelsPerMeter;
                public uint biClrUsed;
                public uint biClrImportant;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFO
            {
                public BITMAPINFOHEADER bmiHeader;
                public uint bmiColors;
            }

            // Z-order'� ger�ekten �ne getirmek i�in (TopMost toggle'� yeterli olmuyordu,
            // ��nk� t�m ikon pencereleri zaten TopMost = true idi).
            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            private const uint SWP_NOMOVE = 0x0002;
            private const uint SWP_NOSIZE = 0x0001;
            private const uint SWP_NOACTIVATE = 0x0010;

            public void BringToTopNoActivate()
            {
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct POINT { public int x; public int y; public POINT(int x, int y) { this.x = x; this.y = y; } }

            [StructLayout(LayoutKind.Sequential)]
            private struct SIZE { public int cx; public int cy; public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; } }

            [StructLayout(LayoutKind.Sequential)]
            private struct BLENDFUNCTION
            {
                public byte BlendOp;
                public byte BlendFlags;
                public byte SourceConstantAlpha;
                public byte AlphaFormat;
            }
            private const int WS_EX_LAYERED = 0x80000;
            private const int ULW_ALPHA = 0x02;
            private const byte AC_SRC_OVER = 0x00;
            private const byte AC_SRC_ALPHA = 0x01;

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_LAYERED;
                    return cp;
                }
            }

            private readonly int _canvasSize;
            private bool _showIndicator = false;

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool ShowIndicator
            {
                get => _showIndicator;
                set { _showIndicator = value; }
            }

            public IconWindow(int canvasSize)
            {
                _canvasSize = canvasSize;
                AutoScaleMode = AutoScaleMode.None;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                Size = new Size(canvasSize, canvasSize); // SABT  bundan sonra bir daha ASLA resize edilmiyor
            }
            public void UpdateBounds(int x, int y, int size)
            {
                if (_originalBitmap == null || size <= 0) return;

                int indicatorSpace = _showIndicator ? 7 : 0; // Extra height for the dot below the icon
                int windowWidth = size;
                int windowHeight = size + indicatorSpace;
                int drawX = 0;
                int drawY = 0;
                int windowX = x;
                int windowY = y;

                bool showText = (size >= 48 && !string.IsNullOrEmpty(_appName));

                if (showText)
                {
                    windowWidth = 150;
                    windowHeight = size + 20 + indicatorSpace;
                    drawX = (windowWidth - size) / 2;
                    drawY = 20;
                    windowX = x - (windowWidth - size) / 2;
                    windowY = y - 20;
                }
                else
                {
                    _scrollOffset = 0f;
                }

                var scaled = new Bitmap(windowWidth, windowHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.Clear(Color.FromArgb(1, 0, 0, 0));
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Draw the icon
                    g.DrawImage(_originalBitmap, drawX, drawY, size, size);

                    // Draw indicator line below icon for open applications
                    if (_showIndicator)
                    {
                        float dotWidth = Math.Max(8f, size * 0.35f);
                        float dotHeight = 3f;
                        float dotX = drawX + (size - dotWidth) / 2f;
                        float dotY = drawY + size + 2f;
                        using (var dotBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
                        {
                            g.FillEllipse(dotBrush, dotX, dotY, dotWidth, dotHeight);
                        }
                    }

                    // Draw the application name above the icon
                    if (showText)
                    {
                        using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.White))
                        {
                            SizeF textSize = g.MeasureString(_appName, font);
                            float maxVisibleWidth = 120f;

                            if (textSize.Width > maxVisibleWidth)
                            {
                                int maxScroll = (int)(textSize.Width - maxVisibleWidth);
                                _scrollOffset += 0.25f; // Slower, smoother scroll step
                                if (_scrollOffset > maxScroll + 30) // Pause at end
                                {
                                    _scrollOffset = -20f; // Pause at start
                                }

                                int currentScroll = Math.Max(0, Math.Min(maxScroll, (int)_scrollOffset));

                                // Draw background pill centered with fixed size
                                float pillWidth = maxVisibleWidth + 12;
                                float pillX = (windowWidth - pillWidth) / 2;
                                using (var bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                                {
                                    var rect = new RectangleF(pillX, 2, pillWidth, textSize.Height + 2);
                                    using (var path = RoundedRect(Rectangle.Round(rect), 3))
                                    {
                                        g.FillPath(bgBrush, path);
                                    }
                                }

                                // Set clipping region to keep text inside the pill boundaries
                                var oldClip = g.Clip;
                                var clipRect = new RectangleF(pillX + 6, 2, maxVisibleWidth, textSize.Height + 2);
                                g.SetClip(clipRect);

                                // Draw scrolling text
                                float drawTextX = (windowWidth - maxVisibleWidth) / 2 - currentScroll;
                                g.DrawString(_appName, font, brush, drawTextX, 3);

                                g.Clip = oldClip;
                            }
                            else
                            {
                                _scrollOffset = 0;
                                float textXNormal = (windowWidth - textSize.Width) / 2;

                                // Draw a subtle dark background pill for readability
                                using (var bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                                {
                                    var rect = new RectangleF(textXNormal - 6, 2, textSize.Width + 12, textSize.Height + 2);
                                    using (var path = RoundedRect(Rectangle.Round(rect), 3))
                                    {
                                        g.FillPath(bgBrush, path);
                                    }
                                }

                                g.DrawString(_appName, font, brush, textXNormal, 3);
                            }
                        }
                    }
                }

                _bitmap?.Dispose();
                _bitmap = scaled;

                SetBounds(windowX, windowY, windowWidth, windowHeight);
                Redraw(windowX, windowY);
            }

            // PNG'nin gerçek alpha kanalını kullanarak pencereyi çiziyoruz.
            // Magenta yok, renk-anahtar yok - kenarlar pürüzsüz.
            public void SetIconImage(Bitmap bmp, int baseSize, string appName)
            {
                _originalBitmap?.Dispose();
                _originalBitmap = new Bitmap(bmp); // pristine kopya, bir daha dokunulmayacak
                _appName = appName;
            }

            // TEK render giriş noktası - hem ilk çizim hem hover animasyonu bunu kullanır.
            // Pencere SIZE'ı hiç değişmiyor (canvasSize sabit); sadece konum kayar (SWP_NOSIZE)
            // ve o sabit tuval içinde ikon büyüyüp küçülüyor. Resize olmadığından DWM'in
            // eski surface'i yanlış gerdirmesi (hayalet ikon) artık mümkün değil.
            public void RenderIcon(int windowX, int windowY, int iconSize)
            {
                if (_originalBitmap == null || !IsHandleCreated) return;

                SetWindowPos(Handle, HWND_TOPMOST, windowX, windowY, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);

                var canvas = new Bitmap(_canvasSize, _canvasSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(canvas))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    int offset = (_canvasSize - iconSize) / 2; // tuvalin ortas�na hizala
                    g.DrawImage(_originalBitmap, offset, offset, iconSize, iconSize);
                }

                _bitmap?.Dispose();
                _bitmap = canvas;
                Redraw(windowX, windowY);
            }

            private void RescaleFromOriginal(int targetSize)
            {
                if (_originalBitmap == null || targetSize <= 0) return;

                var scaled = new Bitmap(targetSize, targetSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_originalBitmap, 0, 0, targetSize, targetSize);
                }

                _bitmap?.Dispose();
                _bitmap = scaled;
                Size = _bitmap.Size; // sadece ilk y�kleme (SetIconImage) i�in, o an konum zaten sabit ve do�ru
                Redraw(Left, Top);
            }

            // x, y art�k PARAMETRE olarak geliyor � Left/Top property'lerinin event
            // s�ras�na ba�l� olarak hen�z g�ncellenmemi� (stale) olma riski tamamen ortadan kalk�yor.
            private void Redraw(int x, int y)
            {
                if (_bitmap == null || !IsHandleCreated) return;

                int w = _bitmap.Width;
                int h = _bitmap.Height;

                IntPtr screenDc = GetDC(IntPtr.Zero);
                IntPtr memDc = CreateCompatibleDC(screenDc);

                var bmi = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                        biWidth = w,
                        biHeight = -h, // negatif = top-down DIB, GDI+ ile e�le�en sat�r s�ras�
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0 // BI_RGB
                    }
                };

                IntPtr hBitmap = CreateDIBSection(screenDc, ref bmi, 0, out IntPtr bits, IntPtr.Zero, 0);
                if (hBitmap == IntPtr.Zero || bits == IntPtr.Zero)
                {
                    DeleteDC(memDc);
                    ReleaseDC(IntPtr.Zero, screenDc);
                    return;
                }

                // Kaynak bitmap'ten ham piksel verisini oku ve PREMULTIPLIED alpha
                // olarak DIB section'a yaz. GetHbitmap'in eksik b�rakt��� as�l ad�m bu �
                // hayalet/ghosting ve kenar fringe sorunlar�n�n k�k� buydu.
                var rect = new Rectangle(0, 0, w, h);
                var srcData = _bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    byte[] row = new byte[w * 4];
                    for (int y2 = 0; y2 < h; y2++)
                    {
                        Marshal.Copy(srcData.Scan0 + y2 * srcData.Stride, row, 0, row.Length);
                        for (int x2 = 0; x2 < w; x2++)
                        {
                            int i = x2 * 4;
                            byte b = row[i];
                            byte g = row[i + 1];
                            byte r = row[i + 2];
                            byte a = row[i + 3];
                            // Premultiply: her renk kanal� alpha oran�nda �l�ekleniyor
                            row[i] = (byte)(b * a / 255);
                            row[i + 1] = (byte)(g * a / 255);
                            row[i + 2] = (byte)(r * a / 255);
                            row[i + 3] = a;
                        }
                        Marshal.Copy(row, 0, bits + y2 * w * 4, row.Length);
                    }
                }
                finally
                {
                    _bitmap.UnlockBits(srcData);
                }

                IntPtr oldBitmap = SelectObject(memDc, hBitmap);

                var size = new SIZE(w, h);
                var pointSource = new POINT(0, 0);
                var topPos = new POINT(x, y);
                var blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, ULW_ALPHA);

                SelectObject(memDc, oldBitmap);
                DeleteObject(hBitmap);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }

            // Boyut de�i�ti�inde (hover animasyonunda) bitmap'i yeniden �l�ekleyip �iz


            protected override bool ShowWithoutActivation => true;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _bitmap?.Dispose();
                    _originalBitmap?.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        // Animasyon durumlarını ve hedef değerleri saklayan güncel veri sınıfı
        private class DockItemData
        {
            public IntPtr WindowHandle { get; set; }
            public Point OriginalLocation { get; set; } // Ekran koordinatı
            public Size OriginalSize { get; set; }
            public string AppKey { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string ExePath { get; set; } = "";
            public bool IsPinned { get; set; }
            public bool IsOpen { get; set; }

            public double CurrentProgress { get; set; } = 0; // 0.0 (en küçük) ile 1.0 (en büyük) arası
            public bool IsHovered { get; set; } = false;
            public System.Windows.Forms.Timer AnimTimer { get; set; } = null!;
            public IconWindow Owner { get; set; } = null!;

            // Bouncing and scale-in animation fields
            public bool IsBouncing { get; set; } = false;
            public float BounceY { get; set; } = 0f;
            public float BounceVelocity { get; set; } = 0f;
            public int BounceTicks { get; set; } = 0;
            public double EnterProgress { get; set; } = 1.0;
            public bool IsTaskManagerPlaceholder { get; set; } = false;
        }

        private class PinnedDockApp
        {
            public string DisplayName { get; set; } = "";
            public string ExePath { get; set; } = "";
        }

        private class DockEntryInfo
        {
            public string AppKey { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string ExePath { get; set; } = "";
            public IntPtr WindowHandle { get; set; } = IntPtr.Zero;
            public bool IsPinned { get; set; }
            public bool IsOpen => WindowHandle != IntPtr.Zero;
            public bool IsTaskManagerPlaceholder { get; set; } = false;
        }

        private readonly List<DockItemData> _dockItems = new();
        private const int MaxIconSize = 48; // pencerelerin SABİT, hiç değişmeyen tuval boyutu
        private System.Windows.Forms.Timer _hoverCheckTimer = null!;
        private System.Windows.Forms.Timer _autoRefreshTimer = null!;
        private NotifyIcon? _trayIcon;
        private ToolStripMenuItem? _startupMenuItem;
        private bool _isUpdatingStartupMenuState;
        private int _separatorX = -1;
        private int _separatorY = -1;
        private bool _dockHiddenByGameMode = false;
        private static readonly string OrderFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dock_order.txt");
        private static readonly string PinnedAppsFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dock_pins.json");
        private static readonly string SettingsFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "opendock_settings.json");
        internal static OpenDockSettings CurrentSettings { get; private set; } = new();
        private const int DockCornerRadius = 10;
        private List<string> _savedOrder = new();
        private List<PinnedDockApp> _pinnedApps = new();
        private ClockForm? _clockForm;
        private const float Gravity = 0.8f;
        private const float BounceSpring = -0.7f;
        private ContextMenuStrip _dockBgContextMenu = null!;

        // Fareyi her ikonun SABT orijinal (bymeden nceki) alanna gre kontrol eder.
        // Bylece byyen/kayan pencere snrlar hover durumunu etkilemez, titreme biter.
        private void CheckHoverStates()
        {
            CheckGameModeVisibility();
            if (_dockHiddenByGameMode) return;

            Point cursor = Cursor.Position;
            int padding = 10; // biraz tolerans, tam kenarda titremeyi de nler

            DockItemData? closestItem = null;
            double minDistance = double.MaxValue;

            foreach (var item in _dockItems)
            {
                var rect = new Rectangle(
                    item.OriginalLocation.X - padding,
                    item.OriginalLocation.Y - padding,
                    item.OriginalSize.Width + padding * 2,
                    item.OriginalSize.Height + padding * 2);

                if (rect.Contains(cursor))
                {
                    double centerX = item.OriginalLocation.X + item.OriginalSize.Width / 2.0;
                    double distance = Math.Abs(cursor.X - centerX);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestItem = item;
                    }
                }
            }

            foreach (var item in _dockItems)
            {
                bool shouldHover = (item == closestItem);
                if (shouldHover && !item.IsHovered)
                    PicBox_MouseEnter(item);
                else if (!shouldHover && item.IsHovered)
                    PicBox_MouseLeave(item);
            }
        }

        public Form1()
        {
            InitializeComponent();

            // Load custom application icon (icon.ico) from executing assembly or base directory
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                this.Icon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch
            {
                try
                {
                    string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        this.Icon = new Icon(iconPath);
                    }
                }
                catch { }
            }

            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(980, 50);
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(0, 0, 0);
            this.TopMost = true;
            this.ShowInTaskbar = false;

            UpdateDockPositionAndSize();
        }

        private void UpdateDockPositionAndSize()
        {
            if (Screen.PrimaryScreen == null) return;
            Rectangle workspace = Screen.PrimaryScreen.WorkingArea;

            string pos = CurrentSettings.DockPosition ?? "Bottom";
            if (pos.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                this.Size = new Size(50, 980);
                int x = workspace.Left + 10;
                int y = (workspace.Height - this.Height) / 2 + workspace.Top;
                this.Location = new Point(x, y);
            }
            else if (pos.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                this.Size = new Size(50, 980);
                int x = workspace.Right - this.Width - 10;
                int y = (workspace.Height - this.Height) / 2 + workspace.Top;
                this.Location = new Point(x, y);
            }
            else if (pos.Equals("Top", StringComparison.OrdinalIgnoreCase))
            {
                this.Size = new Size(980, 50);
                int x = (workspace.Width - this.Width) / 2 + workspace.Left;
                int y = workspace.Top + 10;
                this.Location = new Point(x, y);
            }
            else // Bottom
            {
                this.Size = new Size(980, 50);
                int x = (workspace.Width - this.Width) / 2 + workspace.Left;
                int y = workspace.Height - this.Height - 10;
                this.Location = new Point(x, y);
            }

            ApplyDockRegion();
        }

        private void ChangeDockPosition(string position)
        {
            CurrentSettings.DockPosition = position;
            SaveSettings();
            UpdateDockPositionAndSize();
            RefreshDockIcons();
        }       
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyDockRegion();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Right)
            {
                _dockBgContextMenu?.Show(this, e.Location);
            }
        }

        private void ApplyDockRegion()
        {
            if (Width <= 0 || Height <= 0)
                return;

            Region?.Dispose();
            Region = new Region(RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), DockCornerRadius));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.SuspendLayout();
            LoadSettings();
            UpdateDockPositionAndSize();
            LoadPinnedApps();
            LoadDockOrder();
            RefreshDockIcons();
            EnableBlur();
            _hoverCheckTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _hoverCheckTimer.Tick += (s, e) => CheckHoverStates();
            _hoverCheckTimer.Start();

            _autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _autoRefreshTimer.Tick += (s, e) => CheckForWindowChanges();
            _autoRefreshTimer.Start();

            // Initialize and show clock
            _clockForm = new ClockForm();
            _clockForm.Show();

            // Initialize Right-Click context menu for the dock background
            _dockBgContextMenu = new ContextMenuStrip
            {
                Renderer = new ModernTrayMenuRenderer(),
                ShowImageMargin = false,
                BackColor = Color.FromArgb(32, 32, 34),
                ForeColor = Color.FromArgb(246, 247, 249),
                Font = new Font("Segoe UI", 9.5f)
            };

            var taskmgrItem = new ToolStripMenuItem("Görev Yöneticisi");
            taskmgrItem.Click += (s, e) =>
            {
                try
                {
                    Process.Start("taskmgr.exe");
                }
                catch { }
            };
            _dockBgContextMenu.Items.Add(taskmgrItem);

            _dockBgContextMenu.Items.Add("-");

            var refreshItem = new ToolStripMenuItem("Yenile");
            refreshItem.Click += (s, e) => RefreshDockIcons();
            _dockBgContextMenu.Items.Add(refreshItem);

            this.Activated += (s, e) => EnableBlur();
            this.Deactivate += (s, e) => EnableBlur();
            InitializeTrayIcon();
            StartMenuForm.WarmInstalledAppsCacheAsync();
            this.ResumeLayout();
        }

        private void RefreshDockIcons()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshDockIcons));
                return;
            }

            // Önce eski ikon pencerelerini ve timer'larını temizle
            foreach (var item in _dockItems)
            {
                item.AnimTimer.Stop();
                item.AnimTimer.Dispose();
                item.Owner.Hide(); // İkonu ekrandan ANINDA gizle (iç içe geçmeyi ve hayalet ikonları önler)
                item.Owner.Close();
                item.Owner.Dispose();
            }
            _dockItems.Clear();

            string pos = CurrentSettings.DockPosition ?? "Bottom";
            bool isVertical = pos.Equals("Left", StringComparison.OrdinalIgnoreCase) || 
                              pos.Equals("Right", StringComparison.OrdinalIgnoreCase);

            int baseSize = 32;
            int defaultOffset = isVertical ? (this.Width - baseSize) / 2 : (this.Height - baseSize) / 2;

            var seenProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tempWindows = new List<(IntPtr hwnd, string procName, Process proc)>();

            // Collect all matching open windows first
            EnumWindows((hwnd, lParam) =>
            {
                if (ShouldShowProcessWindow(hwnd))
                {
                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid != 0)
                    {
                        try
                        {
                            var proc = Process.GetProcessById((int)pid);
                            string procName = proc.ProcessName;

                            if (!procName.Equals("OpenDock", StringComparison.OrdinalIgnoreCase) &&
                                !procName.Equals("idle", StringComparison.OrdinalIgnoreCase))
                            {
                                if (procName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                                {
                                    string className = GetWindowClassName(hwnd);
                                    if (className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) ||
                                        className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (seenProcessNames.Add(procName))
                                        {
                                            tempWindows.Add((hwnd, procName, proc));
                                        }
                                        else
                                        {
                                            proc.Dispose();
                                        }
                                    }
                                    else
                                    {
                                        proc.Dispose();
                                    }
                                }
                                else
                                {
                                    if (seenProcessNames.Add(procName))
                                    {
                                        tempWindows.Add((hwnd, procName, proc));
                                    }
                                    else
                                    {
                                        proc.Dispose();
                                    }
                                }
                            }
                            else
                            {
                                proc.Dispose();
                            }
                        }
                        catch { }
                    }
                }
                return true;
            }, IntPtr.Zero);

            // Add newly discovered applications to the end of our saved order list
            bool orderUpdated = false;
            foreach (var item in tempWindows)
            {
                if (!_savedOrder.Contains(item.procName, StringComparer.OrdinalIgnoreCase))
                {
                    _savedOrder.Add(item.procName);
                    orderUpdated = true;
                }
            }
            if (orderUpdated)
            {
                SaveDockOrder();
            }

            var dockEntries = new List<DockEntryInfo>();
            var entryMap = new Dictionary<string, DockEntryInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var winItem in tempWindows)
            {
                IntPtr hwnd = winItem.hwnd;
                string procName = winItem.procName;
                Process proc = winItem.proc;

                try
                {
                    string path = GetProcessFilePath(proc);
                    if (string.IsNullOrWhiteSpace(path) || !IsValidExecutablePath(path))
                        continue;

                    string appKey = NormalizeAppKey(path);
                    if (string.IsNullOrWhiteSpace(appKey))
                        continue;

                    string displayName = "";
                    try
                    {
                        var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                        displayName = vi.FileDescription ?? "";
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(procName.ToLower());
                    }

                    // Clean up common application names for clean Dock presentation
                    if (displayName.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase))
                    {
                        if (displayName.Contains("Code", StringComparison.OrdinalIgnoreCase))
                            displayName = "VS Code";
                        else
                            displayName = "Visual Studio 2022";
                    }
                    else if (displayName.Contains("Windows Terminal", StringComparison.OrdinalIgnoreCase) || 
                             procName.Equals("WindowsTerminal", StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = "Terminal";
                    }
                    else if (displayName.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = displayName.Substring(10);
                    }

                    if (!entryMap.TryGetValue(appKey, out var entry))
                    {
                        entry = new DockEntryInfo
                        {
                            AppKey = appKey,
                            DisplayName = displayName,
                            ExePath = path,
                            WindowHandle = hwnd,
                            IsPinned = IsPinnedApp(path)
                        };
                        entryMap[appKey] = entry;
                        dockEntries.Add(entry);
                    }
                    else if (entry.WindowHandle == IntPtr.Zero)
                    {
                        entry.WindowHandle = hwnd;
                        if (!string.IsNullOrWhiteSpace(displayName))
                            entry.DisplayName = displayName;
                        entry.ExePath = path;
                    }

                    if (!entry.IsPinned && IsPinnedApp(path))
                    {
                        entry.IsPinned = true;
                    }
                }
                catch
                {
                    continue;
                }
                finally
                {
                    proc?.Dispose();
                }
            }

            foreach (var pinnedApp in _pinnedApps)
            {
                string path = pinnedApp.ExePath.Trim();
                if (!IsValidExecutablePath(path))
                    continue;

                string appKey = NormalizeAppKey(path);
                if (string.IsNullOrWhiteSpace(appKey))
                    continue;

                if (entryMap.ContainsKey(appKey))
                {
                    entryMap[appKey].IsPinned = true;
                    if (string.IsNullOrWhiteSpace(entryMap[appKey].DisplayName))
                    {
                        entryMap[appKey].DisplayName = string.IsNullOrWhiteSpace(pinnedApp.DisplayName)
                            ? GetDisplayNameFromPath(path)
                            : pinnedApp.DisplayName;
                    }
                    continue;
                }

                var pinnedEntry = new DockEntryInfo
                {
                    AppKey = appKey,
                    DisplayName = string.IsNullOrWhiteSpace(pinnedApp.DisplayName)
                        ? GetDisplayNameFromPath(path)
                        : pinnedApp.DisplayName,
                    ExePath = path,
                    WindowHandle = IntPtr.Zero,
                    IsPinned = true
                };
                entryMap[appKey] = pinnedEntry;
                dockEntries.Add(pinnedEntry);
            }

            // Ensure all pinned apps are represented in _savedOrder
            bool orderUpdatedPinned = false;
            foreach (var pinnedApp in _pinnedApps)
            {
                string path = pinnedApp.ExePath.Trim();
                if (!IsValidExecutablePath(path))
                    continue;

                string procName = GetProcessNameFromPath(path);
                if (!string.IsNullOrEmpty(procName) && !_savedOrder.Contains(procName, StringComparer.OrdinalIgnoreCase))
                {
                    _savedOrder.Add(procName);
                    orderUpdatedPinned = true;
                }
            }
            if (orderUpdatedPinned)
            {
                SaveDockOrder();
            }

            // Sort all dockEntries according to their index in the saved order list (prevents icons from shifting around when opened/closed)
            dockEntries.Sort((x, y) =>
            {
                string procX = GetProcessNameFromPath(x.ExePath);
                string procY = GetProcessNameFromPath(y.ExePath);

                int idxX = _savedOrder.FindIndex(name => name.Equals(procX, StringComparison.OrdinalIgnoreCase));
                int idxY = _savedOrder.FindIndex(name => name.Equals(procY, StringComparison.OrdinalIgnoreCase));

                if (idxX == -1) idxX = int.MaxValue;
                if (idxY == -1) idxY = int.MaxValue;

                return idxX.CompareTo(idxY);
            });

            // Create dock items in the sorted order
            int startOffset = 25;
            foreach (var entry in dockEntries)
            {
                try
                {
                    Icon? icon = GetHighQualityIcon(entry.ExePath);
                    if (icon == null)
                        icon = GetGenericApplicationIcon();

                    Point screenLocation;
                    if (isVertical)
                        screenLocation = new Point(this.Left + defaultOffset, this.Top + startOffset);
                    else
                        screenLocation = new Point(this.Left + startOffset, this.Top + defaultOffset);

                    var iconWindow = new IconWindow(MaxIconSize);
                    iconWindow.SetIconImage(icon.ToBitmap(), baseSize, entry.DisplayName);
                    icon.Dispose(); // ToBitmap kopyaladıktan sonra artık lazım değil

                    var animData = new DockItemData
                    {
                        WindowHandle = entry.WindowHandle,
                        OriginalLocation = screenLocation,
                        OriginalSize = new Size(baseSize, baseSize),
                        AppKey = entry.AppKey,
                        DisplayName = entry.DisplayName,
                        ExePath = entry.ExePath,
                        IsPinned = entry.IsPinned,
                        IsOpen = entry.IsOpen,
                        IsTaskManagerPlaceholder = entry.IsTaskManagerPlaceholder,
                        EnterProgress = entry.IsTaskManagerPlaceholder ? 0.0 : 1.0,
                        Owner = iconWindow,
                        AnimTimer = new System.Windows.Forms.Timer { Interval = 15 }
                    };

                    animData.AnimTimer.Tick += (s, e) => UpdateAnimation(animData);

                    if (animData.IsTaskManagerPlaceholder)
                    {
                        animData.AnimTimer.Start();
                    }

                    iconWindow.Cursor = Cursors.Hand;
                    iconWindow.ShowIndicator = animData.IsOpen;
                    iconWindow.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Right)
                        {
                            BuildDockItemContextMenu(animData).Show(iconWindow, e.Location);
                            return;
                        }

                        if (e.Button != MouseButtons.Left)
                            return;

                        if (animData.IsOpen && animData.WindowHandle != IntPtr.Zero)
                        {
                            FocusWindow(animData.WindowHandle);
                            return;
                        }

                        try
                        {
                            // Start bouncing trampoline animation when launching
                            animData.IsBouncing = true;
                            animData.BounceVelocity = -12f;
                            animData.BounceTicks = 0;
                            animData.AnimTimer.Start();

                            Process.Start(new ProcessStartInfo(animData.ExePath) { UseShellExecute = true });
                        }
                        catch { }
                    };

                    iconWindow.Show(this); // this = sahibi (owner), her zaman dock'un üzerinde durur
                    iconWindow.UpdateBounds(screenLocation.X, screenLocation.Y, baseSize); // handle artık var - ilk çizimi garanti altına al
                    _dockItems.Add(animData);
                    startOffset += baseSize + 15;
                }
                catch
                {
                    continue;
                }
            }

            // Draw a separator line and place the Windows button at the very bottom/right of the dock
            int winButtonOffset = (isVertical ? this.Height : this.Width) - baseSize - 25;
            int separatorOffset = winButtonOffset - 15;
            if (isVertical)
            {
                _separatorY = separatorOffset;
                _separatorX = -1;
            }
            else
            {
                _separatorX = separatorOffset;
                _separatorY = -1;
            }

            try
            {
                Point screenLocation;
                if (isVertical)
                    screenLocation = new Point(this.Left + defaultOffset, this.Top + winButtonOffset);
                else
                    screenLocation = new Point(this.Left + winButtonOffset, this.Top + defaultOffset);

                var winIconWindow = new IconWindow(MaxIconSize);
                winIconWindow.SetIconImage(GetMenuLogoBitmap(baseSize), baseSize, "Menü");

                var winAnimData = new DockItemData
                {
                    WindowHandle = IntPtr.Zero,
                    OriginalLocation = screenLocation,
                    OriginalSize = new Size(baseSize, baseSize),
                    Owner = winIconWindow,
                    AnimTimer = new System.Windows.Forms.Timer { Interval = 15 }
                };

                winAnimData.AnimTimer.Tick += (s, e) => UpdateAnimation(winAnimData);
                winIconWindow.Cursor = Cursors.Hand;
                winIconWindow.Click += (s, e) =>
                {
                    ToggleCustomStartMenu(winIconWindow.PointToScreen(new Point(0, 0)));
                };

                winIconWindow.Show(this);
                winIconWindow.UpdateBounds(screenLocation.X, screenLocation.Y, baseSize);
                _dockItems.Add(winAnimData);
            }
            catch { }

            this.Invalidate();
        }

        private Bitmap GetMenuLogoBitmap(int size)
        {
            string logoPath = CurrentSettings.MenuLogoPath;
            if (!string.IsNullOrWhiteSpace(logoPath) && System.IO.File.Exists(logoPath))
            {
                try
                {
                    using var source = new Bitmap(logoPath);
                    var customLogo = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(customLogo))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.DrawImage(source, new Rectangle(0, 0, size, size));
                    }
                    return customLogo;
                }
                catch { }
            }

            return GetWindowsLogoBitmap(size);
        }

        private Bitmap GetWindowsLogoBitmap(int size)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Add a small margin to shrink the logo slightly (15% padding)
                float margin = size * 0.15f;
                float drawSize = size - (margin * 2);

                // Draw modern Windows 11 logo (4 white/semi-transparent squares)
                using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                {
                    float gap = drawSize * 0.08f;
                    float half = (drawSize - gap) / 2f;

                    // Top Left
                    g.FillRectangle(brush, margin, margin, half, half);
                    // Top Right
                    g.FillRectangle(brush, margin + half + gap, margin, half, half);
                    // Bottom Left
                    g.FillRectangle(brush, margin, margin + half + gap, half, half);
                    // Bottom Right
                    g.FillRectangle(brush, margin + half + gap, margin + half + gap, half, half);
                }
            }
            return bmp;
        }

        private void LoadDockOrder()
        {
            try
            {
                if (System.IO.File.Exists(OrderFilePath))
                {
                    _savedOrder = System.IO.File.ReadAllLines(OrderFilePath)
                                      .Select(line => line.Trim())
                                      .Where(line => !string.IsNullOrEmpty(line))
                                      .ToList();
                }
            }
            catch { }
        }

        private void SaveDockOrder()
        {
            try
            {
                System.IO.File.WriteAllLines(OrderFilePath, _savedOrder);
            }
            catch { }
        }

        private static void LoadSettings()
        {
            try
            {
                if (!System.IO.File.Exists(SettingsFilePath))
                {
                    CurrentSettings = new OpenDockSettings();
                    return;
                }

                string json = System.IO.File.ReadAllText(SettingsFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                CurrentSettings = JsonSerializer.Deserialize<OpenDockSettings>(json, options) ?? new OpenDockSettings();
                CurrentSettings.DockColorArgb = WithAlpha(CurrentSettings.DockColorArgb, 175);
                CurrentSettings.MenuColorArgb = WithAlpha(CurrentSettings.MenuColorArgb, 175);
                CurrentSettings.SearchColorArgb = WithAlpha(CurrentSettings.SearchColorArgb, 255);
            }
            catch
            {
                CurrentSettings = new OpenDockSettings();
            }
        }

        private static int WithAlpha(int colorArgb, int alpha)
        {
            var color = Color.FromArgb(colorArgb);
            return Color.FromArgb(alpha, color.R, color.G, color.B).ToArgb();
        }

        private static void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(CurrentSettings, options);
                System.IO.File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        private void ApplyDockAppearance()
        {
            CurrentSettings.DockColorArgb = WithAlpha(CurrentSettings.DockColorArgb, 175);
            ApplyDockRegion();
            EnableBlur();
            Invalidate(true);
            Update();
        }

        private void SelectMenuLogo()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "32x32 logo sec",
                Filter = "Resim dosyalari|*.png;*.jpg;*.jpeg;*.bmp;*.ico|Tum dosyalar|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            CurrentSettings.MenuLogoPath = dialog.FileName;
            SaveSettings();
            RefreshDockIcons();
        }

        private void SelectColor(string title, Func<OpenDockSettings, int> getColor, Action<OpenDockSettings, int> setColor, Action afterApply, int? alphaOverride = null)
        {
            using var dialog = new ColorDialog
            {
                FullOpen = true,
                Color = Color.FromArgb(getColor(CurrentSettings))
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var previousColor = Color.FromArgb(getColor(CurrentSettings));
            int alpha = alphaOverride ?? previousColor.A;
            var selectedColor = Color.FromArgb(alpha, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            setColor(CurrentSettings, selectedColor.ToArgb());
            SaveSettings();
            afterApply();
        }

        private static string NormalizeAppKey(string? appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath))
                return "";

            string normalized = appPath.Trim().Trim('"');
            if (System.IO.Path.IsPathRooted(normalized))
            {
                try
                {
                    normalized = System.IO.Path.GetFullPath(normalized);
                }
                catch
                {
                    // Keep the trimmed value if the path cannot be normalized.
                }
            }

            return normalized.ToLowerInvariant();
        }

        private static string GetDisplayNameFromPath(string? exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                return "Uygulama";

            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(exePath);
                if (!string.IsNullOrWhiteSpace(fileName))
                    return fileName;
            }
            catch { }

            return "Uygulama";
        }

        private void LoadPinnedApps()
        {
            try
            {
                if (!System.IO.File.Exists(PinnedAppsFilePath))
                {
                    _pinnedApps = new List<PinnedDockApp>();
                    return;
                }

                string json = System.IO.File.ReadAllText(PinnedAppsFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var fileData = JsonSerializer.Deserialize<PinnedDockConfig>(json, options);
                _pinnedApps = fileData?.PinnedApps
                    ?.Where(app => !string.IsNullOrWhiteSpace(app.ExePath))
                    .Select(app => new PinnedDockApp
                    {
                        DisplayName = string.IsNullOrWhiteSpace(app.DisplayName) ? GetDisplayNameFromPath(app.ExePath) : app.DisplayName.Trim(),
                        ExePath = app.ExePath.Trim()
                    })
                    .GroupBy(app => NormalizeAppKey(app.ExePath))
                    .Select(group => group.First())
                    .ToList() ?? new List<PinnedDockApp>();
            }
            catch
            {
                _pinnedApps = new List<PinnedDockApp>();
            }
        }

        private void SavePinnedApps()
        {
            try
            {
                var config = new PinnedDockConfig
                {
                    PinnedApps = _pinnedApps
                        .Where(app => !string.IsNullOrWhiteSpace(app.ExePath))
                        .GroupBy(app => NormalizeAppKey(app.ExePath))
                        .Select(group => group.First())
                        .OrderBy(app => app.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(PinnedAppsFilePath, json);
            }
            catch { }
        }

        private bool IsPinnedApp(string exePath)
        {
            string key = NormalizeAppKey(exePath);
            return _pinnedApps.Any(app => NormalizeAppKey(app.ExePath) == key);
        }

        private void PinApp(string displayName, string exePath)
        {
            string key = NormalizeAppKey(exePath);
            if (string.IsNullOrWhiteSpace(key))
                return;

            var existing = _pinnedApps.FirstOrDefault(app => NormalizeAppKey(app.ExePath) == key);
            if (existing != null)
            {
                existing.DisplayName = displayName;
                existing.ExePath = exePath;
            }
            else
            {
                _pinnedApps.Add(new PinnedDockApp
                {
                    DisplayName = displayName,
                    ExePath = exePath
                });
            }

            // Keep _savedOrder in sync when pinning a new app
            string procName = GetProcessNameFromPath(exePath);
            if (!string.IsNullOrEmpty(procName) && !_savedOrder.Contains(procName, StringComparer.OrdinalIgnoreCase))
            {
                _savedOrder.Add(procName);
                SaveDockOrder();
            }

            SavePinnedApps();
        }

        private void UnpinApp(string exePath)
        {
            string key = NormalizeAppKey(exePath);
            if (string.IsNullOrWhiteSpace(key))
                return;

            _pinnedApps = _pinnedApps
                .Where(app => NormalizeAppKey(app.ExePath) != key)
                .ToList();
            SavePinnedApps();
        }

        private sealed class PinnedDockConfig
        {
            public List<PinnedDockApp> PinnedApps { get; set; } = new();
        }

        private ContextMenuStrip BuildDockItemContextMenu(DockItemData data)
        {
            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false
            };

            var pinToggleText = data.IsPinned ? "Dock'tan kaldır" : "Dock'a kilitle";
            var pinToggleItem = new ToolStripMenuItem(pinToggleText);
            pinToggleItem.Enabled = !string.IsNullOrWhiteSpace(data.ExePath);
            pinToggleItem.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(data.ExePath))
                    return;

                if (data.IsPinned)
                {
                    UnpinApp(data.ExePath);
                }
                else
                {
                    PinApp(string.IsNullOrWhiteSpace(data.DisplayName) ? GetDisplayNameFromPath(data.ExePath) : data.DisplayName, data.ExePath);
                }

                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(new Action(RefreshDockIcons));
                }
            };

            menu.Items.Add(pinToggleItem);

            // Add "Görevi sonlandır" under pinToggleItem if the app is currently running (prevent killing explorer.exe)
            if (data.WindowHandle != IntPtr.Zero)
            {
                string procName = GetProcessNameFromPath(data.ExePath);
                if (!procName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                {
                    var killItem = new ToolStripMenuItem("Görevi sonlandır");
                    killItem.Click += (s, e) =>
                    {
                        try
                        {
                            GetWindowThreadProcessId(data.WindowHandle, out uint pid);
                            if (pid != 0)
                            {
                                using (var proc = Process.GetProcessById((int)pid))
                                {
                                    proc.Kill();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Görev sonlandırılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                    menu.Items.Add(killItem);
                }
            }

            return menu;
        }

        private void CheckForWindowChanges()
        {
            var currentHandles = new List<IntPtr>();
            var seenProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            EnumWindows((hwnd, lParam) =>
            {
                if (ShouldShowProcessWindow(hwnd))
                {
                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid != 0)
                    {
                        try
                        {
                            using (var proc = Process.GetProcessById((int)pid))
                            {
                                string procName = proc.ProcessName;
                                if (!procName.Equals("OpenDock", StringComparison.OrdinalIgnoreCase) &&
                                    !procName.Equals("idle", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (procName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string className = GetWindowClassName(hwnd);
                                        if (className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) ||
                                            className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (seenProcessNames.Add(procName))
                                            {
                                                currentHandles.Add(hwnd);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (seenProcessNames.Add(procName))
                                        {
                                            currentHandles.Add(hwnd);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                return true;
            }, IntPtr.Zero);

            var existingHandles = new List<IntPtr>();
            foreach (var item in _dockItems)
            {
                if (item.WindowHandle != IntPtr.Zero)
                {
                    existingHandles.Add(item.WindowHandle);
                }
            }

            // Compare sets of handles (order-independent) to prevent refreshing on window focus/Z-order changes
            var currentSet = new HashSet<IntPtr>(currentHandles);
            var existingSet = new HashSet<IntPtr>(existingHandles);
            bool changed = !currentSet.SetEquals(existingSet);

            if (changed)
            {
                RefreshDockIcons();
            }
        }

        // Animasyon Hesaplama Motoru (Delta Zamanlı Geçiş)
        private void UpdateAnimation(DockItemData data)
        {
            double step = 0.12;

            // Handle scale-in animation for new items (like Task Manager placeholder)
            if (data.EnterProgress < 1.0)
            {
                data.EnterProgress += 0.08;
                if (data.EnterProgress > 1.0) data.EnterProgress = 1.0;
            }

            if (data.IsHovered)
            {
                data.CurrentProgress += step;
                if (data.CurrentProgress > 1.0) data.CurrentProgress = 1.0;
            }
            else
            {
                data.CurrentProgress -= step;
                if (data.CurrentProgress < 0.0)
                {
                    data.CurrentProgress = 0.0;
                    // Stop timer only if not bouncing and not scaling in
                    if (!data.IsBouncing && data.EnterProgress >= 1.0)
                    {
                        data.AnimTimer.Stop();
                    }
                }
            }

            int minSize = data.OriginalSize.Width;
            int maxSize = 48;
            int currentSize = minSize + (int)((maxSize - minSize) * data.CurrentProgress);

            // Apply EnterProgress scale
            currentSize = (int)(currentSize * data.EnterProgress);

            int maxYukseklik = 24;
            string pos = CurrentSettings.DockPosition ?? "Bottom";
            bool isVertical = pos.Equals("Left", StringComparison.OrdinalIgnoreCase) || 
                              pos.Equals("Right", StringComparison.OrdinalIgnoreCase);

            int currentX = data.OriginalLocation.X;
            int currentY = data.OriginalLocation.Y;

            int sizeDiff = currentSize - minSize;

            if (isVertical)
            {
                // Centered vertically when scaling
                currentY = data.OriginalLocation.Y - sizeDiff / 2;

                if (pos.Equals("Left", StringComparison.OrdinalIgnoreCase))
                {
                    // Shifts Right (inwards) when hovered
                    currentX = data.OriginalLocation.X + (int)(maxYukseklik * data.CurrentProgress);
                }
                else // Right
                {
                    // Shifts Left (inwards) when hovered
                    currentX = data.OriginalLocation.X - (int)(maxYukseklik * data.CurrentProgress) - sizeDiff;
                }
            }
            else // Horizontal (Bottom or Top)
            {
                // Centered horizontally when scaling
                currentX = data.OriginalLocation.X - sizeDiff / 2;

                if (pos.Equals("Top", StringComparison.OrdinalIgnoreCase))
                {
                    // Shifts Down (inwards) when hovered
                    currentY = data.OriginalLocation.Y + (int)(maxYukseklik * data.CurrentProgress);
                }
                else // Bottom
                {
                    // Shifts Up (inwards) when hovered
                    currentY = data.OriginalLocation.Y - (int)(maxYukseklik * data.CurrentProgress);
                }
            }

            // Apply Bouncing physics (trampoline spring effect)
            if (data.IsBouncing)
            {
                data.BounceTicks++;
                if (data.BounceTicks > 600) // Timeout of ~10s to prevent infinite bounce
                {
                    data.IsBouncing = false;
                }

                data.BounceVelocity += Gravity;
                data.BounceY += data.BounceVelocity;

                if (data.BounceY >= 0)
                {
                    data.BounceY = 0;
                    data.BounceVelocity = data.BounceVelocity * BounceSpring; // Bounce back up!

                    // Keep jumping if not yet launched
                    if (Math.Abs(data.BounceVelocity) < 3.0f)
                    {
                        data.BounceVelocity = -12.0f; // fresh jump
                    }
                }

                // Bounce direction is inwards/outwards relative to screen bounds
                if (pos.Equals("Left", StringComparison.OrdinalIgnoreCase))
                {
                    currentX += (int)(-data.BounceY);
                }
                else if (pos.Equals("Right", StringComparison.OrdinalIgnoreCase))
                {
                    currentX -= (int)(-data.BounceY);
                }
                else if (pos.Equals("Top", StringComparison.OrdinalIgnoreCase))
                {
                    currentY += (int)(-data.BounceY);
                }
                else // Bottom
                {
                    currentY += (int)data.BounceY;
                }
            }

            data.Owner.UpdateBounds(currentX, currentY, currentSize);
        }

        private void PicBox_MouseEnter(DockItemData data)
        {
            data.IsHovered = true;
            data.Owner.BringToTopNoActivate(); // gerek z-order gncellemesi  focus almadan en ne getirir
            data.AnimTimer.Start();
        }

        private void PicBox_MouseLeave(DockItemData data)
        {
            data.IsHovered = false;
            data.AnimTimer.Start();
        }

        private Icon? GetHighQualityIcon(string fileName)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hImg = SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                    DestroyIcon(shinfo.hIcon);
                    return icon;
                }
            }
            catch { }

            // Fallback: check if there's a custom icon.ico or similar in the executable's directory
            try
            {
                if (!string.IsNullOrWhiteSpace(fileName) && System.IO.File.Exists(fileName))
                {
                    string? dir = System.IO.Path.GetDirectoryName(fileName);
                    if (!string.IsNullOrWhiteSpace(dir) && System.IO.Directory.Exists(dir))
                    {
                        string iconPath = System.IO.Path.Combine(dir, "icon.ico");
                        if (System.IO.File.Exists(iconPath))
                        {
                            return new Icon(iconPath);
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private void FocusWindow(IntPtr hWnd)
        {
            const int SW_RESTORE = 9;
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }

        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_NCACTIVATE = 0x0086;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE; // Never let dock steal focus - blur stays active forever
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1;
                return;
            }

            // Keep Acrylic blur active when losing focus
            if (m.Msg == WM_NCACTIVATE)
            {
                m.WParam = (IntPtr)1;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // DWM arka plan (Mica/Acrylic) efektinin görünmesi için arka planı düz renkle boyamıyoruz.
        }

        private void EnableBlur()
        {
            // Apply WS_EX_NOACTIVATE at runtime too (belt-and-suspenders)
            IntPtr exStyle = GetWindowLongPtr(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, new IntPtr(exStyle.ToInt32() | WS_EX_NOACTIVATE));

            // Dock uses its own region mask; DWM rounding would add a second, mismatched corner shape.
            int cornerPref = 1;
            DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));

            // Set Window Composition Accent Policy for Aero Blur (keeps blur active when deactivated)
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                GradientColor = 0x55181818
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var dockColor = Color.FromArgb(CurrentSettings.DockColorArgb);
            var glassColor = Color.FromArgb(74, dockColor.R, dockColor.G, dockColor.B);

            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.Clear(Color.Transparent);

            using (var path = RoundedRect(rect, DockCornerRadius))
            using (var fillBrush = new SolidBrush(glassColor))
            using (var borderPen = new Pen(Color.FromArgb(82, 255, 255, 255), 1f))
            using (var glossBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect,
                Color.FromArgb(26, 255, 255, 255),
                Color.FromArgb(3, 255, 255, 255),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                g.FillPath(fillBrush, path);
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.FillPath(glossBrush, path);
                g.DrawPath(borderPen, path);
            }

            // Draw vertical/horizontal separator line for the Windows button
            string pos = CurrentSettings.DockPosition ?? "Bottom";
            bool isVertical = pos.Equals("Left", StringComparison.OrdinalIgnoreCase) || 
                              pos.Equals("Right", StringComparison.OrdinalIgnoreCase);

            if (isVertical)
            {
                if (_separatorY > 0)
                {
                    using (var separatorPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
                    {
                        g.DrawLine(separatorPen, 10, _separatorY, Width - 10, _separatorY);
                    }
                }
            }
            else
            {
                if (_separatorX > 0)
                {
                    using (var separatorPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
                    {
                        g.DrawLine(separatorPen, _separatorX, 10, _separatorX, Height - 10);
                    }
                }
            }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "OpenDock";
            _trayIcon.Icon = this.Icon ?? SystemIcons.Application;

            var contextMenu = new ContextMenuStrip
            {
                Renderer = new ModernTrayMenuRenderer(),
                ShowImageMargin = true,
                BackColor = Color.FromArgb(32, 32, 34),
                ForeColor = Color.FromArgb(246, 247, 249),
                Font = new Font("Segoe UI", 9.5f)
            };

            contextMenu.Items.Add("Yenile", null, (s, e) => RefreshDockIcons());
            contextMenu.Items.Add("-");

            var appearanceMenu = new ToolStripMenuItem("Gorunum");
            appearanceMenu.DropDownItems.Add("32x32 logo sec", null, (s, e) => SelectMenuLogo());
            appearanceMenu.DropDownItems.Add("Varsayilan Windows logosu", null, (s, e) =>
            {
                CurrentSettings.MenuLogoPath = "";
                SaveSettings();
                RefreshDockIcons();
            });
            appearanceMenu.DropDownItems.Add("-");
            appearanceMenu.DropDownItems.Add("Dock rengi", null, (s, e) =>
                SelectColor(
                    "Dock rengi",
                    settings => settings.DockColorArgb,
                    (settings, color) => settings.DockColorArgb = color,
                    () =>
                    {
                        ApplyDockAppearance();
                    },
                    175));
            appearanceMenu.DropDownItems.Add("Menu rengi", null, (s, e) =>
                SelectColor(
                    "Menu rengi",
                    settings => settings.MenuColorArgb,
                    (settings, color) => settings.MenuColorArgb = color,
                    () =>
                    {
                        _startMenu?.Close();
                    },
                    175));
            appearanceMenu.DropDownItems.Add("Arama kutusu rengi", null, (s, e) =>
                SelectColor(
                    "Arama kutusu rengi",
                    settings => settings.SearchColorArgb,
                    (settings, color) => settings.SearchColorArgb = color,
                    () =>
                    {
                        _startMenu?.Close();
                    },
                    255));
            var positionMenu = new ToolStripMenuItem("Konum");
            positionMenu.DropDownItems.Add("Alt (Yatay)", null, (s, e) => ChangeDockPosition("Bottom"));
            positionMenu.DropDownItems.Add("Üst (Yatay)", null, (s, e) => ChangeDockPosition("Top"));
            positionMenu.DropDownItems.Add("Sol (Dikey)", null, (s, e) => ChangeDockPosition("Left"));
            positionMenu.DropDownItems.Add("Sağ (Dikey)", null, (s, e) => ChangeDockPosition("Right"));
            appearanceMenu.DropDownItems.Add(positionMenu);

            contextMenu.Items.Add(appearanceMenu);

            _startupMenuItem = new ToolStripMenuItem("Başlangıçta çalıştır")
            {
                CheckOnClick = true,
                Checked = IsStartupEnabled()
            };
            _startupMenuItem.CheckedChanged += StartupMenuItem_CheckedChanged;
            contextMenu.Items.Add(_startupMenuItem);

            var gameModeMenuItem = new ToolStripMenuItem("Oyun Modu")
            {
                CheckOnClick = true,
                Checked = CurrentSettings.GameModeEnabled
            };
            gameModeMenuItem.CheckedChanged += (s, e) =>
            {
                CurrentSettings.GameModeEnabled = gameModeMenuItem.Checked;
                SaveSettings();
                CheckGameModeVisibility();
            };
            contextMenu.Items.Add(gameModeMenuItem);
            contextMenu.Opening += (s, e) => SyncStartupMenuState();
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Çıkış", null, (s, e) =>
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                Application.Exit();
            });

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.Visible = true;
        }

        private sealed class ModernTrayMenuRenderer : ToolStripProfessionalRenderer
        {
            public ModernTrayMenuRenderer()
                : base(new ModernTrayColorTable())
            {
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var rect = new Rectangle(Point.Empty, e.Item.Size);
                if (e.Item.Selected)
                {
                    using var brush = new SolidBrush(Color.FromArgb(58, 58, 62));
                    e.Graphics.FillRectangle(brush, rect);
                    return;
                }

                using var bgBrush = new SolidBrush(Color.FromArgb(32, 32, 34));
                e.Graphics.FillRectangle(bgBrush, rect);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                var y = e.Item.Height / 2;
                using var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1f);
                e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
            }
        }

        private sealed class ModernTrayColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Color.FromArgb(32, 32, 34);
            public override Color ImageMarginGradientBegin => Color.FromArgb(32, 32, 34);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(32, 32, 34);
            public override Color ImageMarginGradientEnd => Color.FromArgb(32, 32, 34);
            public override Color MenuItemSelected => Color.FromArgb(58, 58, 62);
            public override Color MenuItemBorder => Color.FromArgb(82, 82, 88);
            public override Color SeparatorDark => Color.FromArgb(70, 255, 255, 255);
            public override Color SeparatorLight => Color.FromArgb(70, 255, 255, 255);
        }

        private void SyncStartupMenuState()
        {
            if (_startupMenuItem == null)
                return;

            bool enabled = IsStartupEnabled();
            if (_startupMenuItem.Checked == enabled)
                return;

            _isUpdatingStartupMenuState = true;
            _startupMenuItem.Checked = enabled;
            _isUpdatingStartupMenuState = false;
        }

        private void StartupMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            if (_startupMenuItem == null || _isUpdatingStartupMenuState)
                return;

            if (!SetStartupEnabled(_startupMenuItem.Checked))
            {
                _isUpdatingStartupMenuState = true;
                _startupMenuItem.Checked = !_startupMenuItem.Checked;
                _isUpdatingStartupMenuState = false;
                MessageBox.Show(
                    "Başlangıç ayarı güncellenemedi.",
                    "OpenDock",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private bool IsFullscreenAppActive()
        {
            IntPtr foregroundWnd = GetForegroundWindow();
            if (foregroundWnd == IntPtr.Zero) return false;

            // Ignore any window belonging to OpenDock process
            GetWindowThreadProcessId(foregroundWnd, out uint pid);
            if (pid == (uint)Process.GetCurrentProcess().Id) return false;

            var className = new System.Text.StringBuilder(256);
            if (GetClassName(foregroundWnd, className, className.Capacity) > 0)
            {
                string cls = className.ToString();
                if (cls.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
                    cls.Equals("WorkerW", StringComparison.OrdinalIgnoreCase) ||
                    cls.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (GetWindowRect(foregroundWnd, out RECT rect))
            {
                Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                int tolerance = 20; // Allow 20px tolerance to handle DPI scaling and window borders/shadows
                if (rect.Left <= screenBounds.Left + tolerance &&
                    rect.Top <= screenBounds.Top + tolerance &&
                    rect.Right >= screenBounds.Right - tolerance &&
                    rect.Bottom >= screenBounds.Bottom - tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckGameModeVisibility()
        {
            if (!CurrentSettings.GameModeEnabled)
            {
                if (_dockHiddenByGameMode)
                {
                    _dockHiddenByGameMode = false;
                    SetDockVisibility(true);
                }
                return;
            }

            bool isFullscreen = IsFullscreenAppActive();
            if (isFullscreen && !_dockHiddenByGameMode)
            {
                _dockHiddenByGameMode = true;
                SetDockVisibility(false);
            }
            else if (!isFullscreen && _dockHiddenByGameMode)
            {
                _dockHiddenByGameMode = false;
                SetDockVisibility(true);
            }
        }

        private void SetDockVisibility(bool visible)
        {
            if (visible)
            {
                this.Show();
                foreach (var item in _dockItems)
                {
                    item.Owner.Show(this);
                }
            }
            else
            {
                foreach (var item in _dockItems)
                {
                    item.Owner.Hide();
                }
                this.Hide();
                _startMenu?.Close();
            }
        }

        private static string GetStartupRegistryValueName()
        {
            return "OpenDock";
        }

        private static string GetStartupCommand()
        {
            string exePath = Application.ExecutablePath;
            return $"\"{exePath}\"";
        }

        private static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            string? value = key?.GetValue(GetStartupRegistryValueName()) as string;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return string.Equals(value.Trim(), GetStartupCommand(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool SetStartupEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (key == null)
                    return false;

                string valueName = GetStartupRegistryValueName();
                if (enabled)
                {
                    key.SetValue(valueName, GetStartupCommand(), RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(valueName, false);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_clockForm != null && !_clockForm.IsDisposed)
            {
                _clockForm.Close();
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private StartMenuForm? _startMenu;
        private void ToggleCustomStartMenu(Point buttonScreenLoc)
        {
            if (_startMenu != null && !_startMenu.IsDisposed)
            {
                _startMenu.Close();
                return;
            }

            _startMenu = new StartMenuForm();
            _startMenu.FormClosed += (s, e) => _startMenu = null;
            _startMenu.StartPosition = FormStartPosition.Manual;
            
            string pos = CurrentSettings.DockPosition ?? "Bottom";
            int w = 350;
            int h = 450;
            int x, y;

            if (pos.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                x = buttonScreenLoc.X + 32 + 15;
                y = buttonScreenLoc.Y - (h / 2) + 16;
            }
            else if (pos.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                x = buttonScreenLoc.X - w - 15;
                y = buttonScreenLoc.Y - (h / 2) + 16;
            }
            else if (pos.Equals("Top", StringComparison.OrdinalIgnoreCase))
            {
                x = buttonScreenLoc.X - (w / 2) + 16;
                y = buttonScreenLoc.Y + 32 + 15;
            }
            else // Bottom
            {
                x = buttonScreenLoc.X - (w / 2) + 16;
                y = buttonScreenLoc.Y - h - 15;
            }

            Rectangle bounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            if (x < bounds.Left) x = bounds.Left + 10;
            if (x + w > bounds.Right) x = bounds.Right - w - 10;
            if (y < bounds.Top) y = bounds.Top + 10;
            if (y + h > bounds.Bottom) y = bounds.Bottom - h - 10;

            _startMenu.Size = new Size(w, h);
            _startMenu.Location = new Point(x, y);

            _startMenu.Show();
        }
    }

    public class StartMenuForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private struct InstalledApp
        {
            public string Name;
            public string ExePath;
            public Icon AppIcon;
        }

        private static List<InstalledApp>? _installedAppsCache;
        private static DateTime _installedAppsCacheTime;
        private static readonly object InstalledAppsCacheLock = new();
        private static bool _isInstalledAppsCacheWarming;
        private static readonly TimeSpan InstalledAppsCacheDuration = TimeSpan.FromMinutes(30);
        private System.Windows.Forms.Timer? _openAnimationTimer;
        private System.Windows.Forms.Timer? _closeAnimationTimer;
        private Point _openAnimationTarget;
        private Point _closeAnimationStart;
        private int _openAnimationFrame;
        private int _closeAnimationFrame;
        private bool _isClosingWithAnimation;
        private const int OpenAnimationFrames = 10;
        private const int CloseAnimationFrames = 8;
        private const int OpenAnimationOffsetY = 18;

        private sealed class AppRowControl : Control
        {
            private readonly string _appName;
            private readonly string _exePath;
            private readonly Bitmap? _icon;
            private bool _hovered;

            public event EventHandler? Clicked;

            public AppRowControl(string appName, string exePath, Bitmap? icon)
            {
                _appName = appName;
                _exePath = exePath;
                _icon = icon;
                Text = appName;

                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                BackColor = Color.FromArgb(34, 34, 34);
                ForeColor = Color.FromArgb(245, 247, 250);
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
                Cursor = Cursors.Hand;
            }

            public bool MatchesFilter(string filter)
            {
                return string.IsNullOrWhiteSpace(filter) ||
                       _appName.Contains(filter, StringComparison.OrdinalIgnoreCase);
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
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnClick(EventArgs e)
            {
                Clicked?.Invoke(this, e);
                base.OnClick(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedRect(rect, 6))
                using (var bgBrush = new SolidBrush(_hovered ? Color.FromArgb(235, 48, 48, 48) : Color.FromArgb(225, 34, 34, 34)))
                using (var borderPen = new Pen(_hovered ? Color.FromArgb(130, 255, 255, 255) : Color.FromArgb(95, 255, 255, 255), 1f))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }

                if (_icon != null)
                {
                    g.DrawImage(_icon, new Rectangle(8, 8, 24, 24));
                }

                var textRect = new RectangleF(40, 0, Width - 50, Height);
                using var textBrush = new SolidBrush(ForeColor);
                using var textFormat = new StringFormat(StringFormat.GenericTypographic)
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_appName, Font, textBrush, textRect, textFormat);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _icon?.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private sealed class RoundedCommandButton : Control
        {
            private bool _hovered;
            private bool _pressed;

            public RoundedCommandButton()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.Selectable,
                    true);

                BackColor = Color.FromArgb(46, 46, 46);
                ForeColor = Color.FromArgb(248, 249, 251);
                Font = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
                Cursor = Cursors.Hand;
                TabStop = true;
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
                if (e.Button == MouseButtons.Left)
                {
                    _pressed = true;
                    Invalidate();
                }

                base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                _pressed = false;
                Invalidate();
                base.OnMouseUp(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                Color fillColor = _pressed
                    ? Color.FromArgb(70, 70, 70)
                    : _hovered
                        ? Color.FromArgb(62, 62, 62)
                        : BackColor;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedRect(rect, 6))
                using (var fillBrush = new SolidBrush(fillColor))
                using (var borderPen = new Pen(Color.FromArgb(110, 255, 255, 255), 1f))
                {
                    g.FillPath(fillBrush, path);
                    g.DrawPath(borderPen, path);
                }

                using var textBrush = new SolidBrush(ForeColor);
                using var format = new StringFormat(StringFormat.GenericTypographic)
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(Text, Font, textBrush, ClientRectangle, format);
            }
        }

        private sealed class SearchInputControl : Control
        {
            private const string Placeholder = "Aramak icin yazin...";
            private string _value = "";

            public event EventHandler? SearchTextChanged;

            public string SearchText
            {
                get => _value;
                private set
                {
                    if (_value == value)
                        return;

                    _value = value;
                    SearchTextChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }

            public SearchInputControl()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.Selectable,
                    true);

                BackColor = Color.FromArgb(45, 45, 45);
                ForeColor = Color.FromArgb(248, 249, 251);
                Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
                Cursor = Cursors.IBeam;
                TabStop = true;
            }

            protected override bool IsInputKey(Keys keyData)
            {
                return keyData == Keys.Left ||
                       keyData == Keys.Right ||
                       keyData == Keys.Back ||
                       keyData == Keys.Delete ||
                       base.IsInputKey(keyData);
            }

            protected override void OnClick(EventArgs e)
            {
                Focus();
                base.OnClick(e);
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Back && SearchText.Length > 0)
                {
                    SearchText = SearchText.Substring(0, SearchText.Length - 1);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    SearchText = "";
                    e.Handled = true;
                }

                base.OnKeyDown(e);
            }

            protected override void OnKeyPress(KeyPressEventArgs e)
            {
                if (!char.IsControl(e.KeyChar))
                {
                    SearchText += e.KeyChar;
                    e.Handled = true;
                }

                base.OnKeyPress(e);
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

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedRect(rect, 6))
                using (var bgBrush = new SolidBrush(BackColor))
                using (var borderPen = new Pen(Focused ? Color.FromArgb(150, 255, 255, 255) : Color.FromArgb(85, 255, 255, 255), 1f))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }

                string text = string.IsNullOrEmpty(SearchText) ? Placeholder : SearchText;
                Color textColor = string.IsNullOrEmpty(SearchText)
                    ? Color.FromArgb(150, 248, 249, 251)
                    : ForeColor;

                var textRect = new RectangleF(12, 0, Width - 24, Height);
                using var textBrush = new SolidBrush(textColor);
                using var textFormat = new StringFormat(StringFormat.GenericTypographic)
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(text, Font, textBrush, textRect, textFormat);

                if (Focused)
                {
                    float textWidth = string.IsNullOrEmpty(SearchText)
                        ? 0f
                        : g.MeasureString(SearchText, Font, PointF.Empty, StringFormat.GenericTypographic).Width;
                    float caretX = Math.Min(textRect.Left + textWidth + 1f, textRect.Right - 1f);
                    using var caretPen = new Pen(ForeColor, 1f);
                    g.DrawLine(caretPen, caretX, 7, caretX, Height - 7);
                }
            }
        }

        public StartMenuForm()
        {
            var configuredMenuColor = Color.FromArgb(Form1.CurrentSettings.MenuColorArgb);
            var configuredSearchColor = Color.FromArgb(Form1.CurrentSettings.SearchColorArgb);
            var menuColor = Color.FromArgb(255, configuredMenuColor.R, configuredMenuColor.G, configuredMenuColor.B);
            var searchColor = Color.FromArgb(255, configuredSearchColor.R, configuredSearchColor.G, configuredSearchColor.B);

            this.DoubleBuffered = true; // Prevent flickering and graphics accumulation
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = menuColor; // Blur üstünde kullanılan ton
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 0;
            this.Deactivate += (s, e) => this.Close();
            this.HandleCreated += (s, e) => EnableBlur();
            this.Activated += (s, e) => EnableBlur();

            // Set soft rounded corners manually using window region clipping (16px radius)
            this.Region = new Region(RoundedRect(new Rectangle(0, 0, 350, 450), 16));

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = menuColor,
                Padding = new Padding(1)
            };
            contentPanel.Paint += (s, e) =>
            {
                using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                using var titleBrush = new SolidBrush(Color.FromArgb(248, 249, 251));
                using var titleFormat = new StringFormat(StringFormat.GenericTypographic)
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                e.Graphics.DrawString("Uygulamalar", titleFont, titleBrush, new RectangleF(20, 17, 220, 28), titleFormat);
            };
            this.Controls.Add(contentPanel);

            var searchBox = new SearchInputControl
            {
                Width = 310,
                Height = 30,
                Location = new Point(20, 58),
                BackColor = searchColor,
                ForeColor = Color.FromArgb(248, 249, 251)
            };
            contentPanel.Controls.Add(searchBox);

            // Container Panel to hide default scrollbar via clipping
            var flowLayoutContainer = new Panel
            {
                Width = 300,
                Height = 275,
                Location = new Point(20, 105),
                BackColor = menuColor
            };
            contentPanel.Controls.Add(flowLayoutContainer);

            // Pinned Apps flow panel (wider than container to push scrollbar off-screen)
            var flowLayout = new FlowLayoutPanel
            {
                Width = 320,
                Height = 275,
                Location = new Point(0, 0),
                BackColor = menuColor,
                AutoScroll = true
            };
            flowLayoutContainer.Controls.Add(flowLayout);

            // Custom modern rounded scrollbar track
            var scrollTrack = new Panel
            {
                Width = 6,
                Height = 275,
                Location = new Point(325, 105),
                BackColor = menuColor
            };
            contentPanel.Controls.Add(scrollTrack);

            // Custom modern rounded scrollbar thumb
            var scrollThumb = new Panel
            {
                Width = 6,
                Height = 40,
                BackColor = Color.FromArgb(95, 95, 95),
                Cursor = Cursors.Hand
            };
            scrollTrack.Controls.Add(scrollThumb);

            // Bind scroll events to update custom scrollbar
            flowLayout.Scroll += (s, e) => UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height);
            flowLayout.MouseWheel += (s, e) => {
                System.Windows.Forms.Timer wheelTimer = new System.Windows.Forms.Timer { Interval = 10 };
                wheelTimer.Tick += (st, et) => {
                    UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height);
                    wheelTimer.Stop();
                    wheelTimer.Dispose();
                };
                wheelTimer.Start();
            };
            // Scrollbar dragging logic
            bool isDragging = false;
            int dragStartY = 0;
            int dragStartScroll = 0;

            scrollThumb.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStartY = Cursor.Position.Y;
                    dragStartScroll = flowLayout.VerticalScroll.Value;
                }
            };

            scrollThumb.MouseMove += (s, e) => {
                if (isDragging)
                {
                    int deltaY = Cursor.Position.Y - dragStartY;
                    int maxScroll = flowLayout.VerticalScroll.Maximum - flowLayout.ClientRectangle.Height;
                    int maxThumbTravel = scrollTrack.Height - scrollThumb.Height;
                    if (maxThumbTravel > 0)
                    {
                        float scrollDeltaPercent = (float)deltaY / maxThumbTravel;
                        int newScroll = dragStartScroll + (int)(scrollDeltaPercent * maxScroll);
                        newScroll = Math.Max(0, Math.Min(maxScroll, newScroll));
                        flowLayout.AutoScrollPosition = new Point(0, newScroll);
                        UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height);
                    }
                }
            };

            scrollThumb.MouseUp += (s, e) => {
                isDragging = false;
            };

            // Fetch installed apps from registry
            var installedApps = GetInstalledAppsSnapshot();
            WarmInstalledAppsCacheAsync();

            flowLayout.SuspendLayout();
            try
            {
                foreach (var app in installedApps)
                {
                    Bitmap? iconBmp = null;
                    try
                    {
                        iconBmp = new Bitmap(app.AppIcon.ToBitmap(), new Size(24, 24));
                    }
                    catch
                    {
                        iconBmp = new Bitmap(Form1.GetGenericApplicationIcon().ToBitmap(), new Size(24, 24));
                    }

                    var row = new AppRowControl(app.Name, app.ExePath, iconBmp);
                    row.Width = 285;
                    row.Height = 40;
                    row.Cursor = Cursors.Hand;
                    row.Clicked += (s, e) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(app.ExePath) { UseShellExecute = true });
                        }
                        catch { }

                        this.Close();
                    };
                    flowLayout.Controls.Add(row);
                }
            }
            finally
            {
                flowLayout.ResumeLayout();
            }
            UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height);

            // Search filtering logic
            searchBox.SearchTextChanged += (s, e) => {
                string filter = searchBox.SearchText;

                flowLayout.SuspendLayout();
                try
                {
                    foreach (Control ctrl in flowLayout.Controls)
                    {
                        if (ctrl is AppRowControl row)
                        {
                            row.Visible = row.MatchesFilter(filter);
                        }
                    }
                }
                finally
                {
                    flowLayout.ResumeLayout();
                }

                BeginInvoke(new Action(() => UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height)));
            };

            // Power control buttons at the bottom
            var powerBtn = new RoundedCommandButton
            {
                Text = "Kapat",
                Width = 80,
                Height = 30,
                Location = new Point(250, 400),
                ForeColor = Color.FromArgb(248, 249, 251),
                BackColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            powerBtn.Click += (s, e) => {
                if (MessageBox.Show("Bilgisayarı kapatmak istiyor musunuz?", "Sistem", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("shutdown", "/s /t 0");
                }
            };
            contentPanel.Controls.Add(powerBtn);

            var restartBtn = new RoundedCommandButton
            {
                Text = "Yeniden Başlat",
                Width = 110,
                Height = 30,
                Location = new Point(130, 400),
                ForeColor = Color.FromArgb(248, 249, 251),
                BackColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            restartBtn.Click += (s, e) => {
                if (MessageBox.Show("Bilgisayarı yeniden başlatmak istiyor musunuz?", "Sistem", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("shutdown", "/r /t 0");
                }
            };
            contentPanel.Controls.Add(restartBtn);

            contentPanel.BringToFront();
            searchBox.BringToFront();
            flowLayoutContainer.BringToFront();
            scrollTrack.BringToFront();
            powerBtn.BringToFront();
            restartBtn.BringToFront();

            Shown += (s, e) =>
            {
                StartOpenAnimation();
                UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height);
                searchBox.Focus();
            };
        }

        private void StartOpenAnimation()
        {
            if (_isClosingWithAnimation)
                return;

            _openAnimationTimer?.Stop();
            _openAnimationTimer?.Dispose();

            _openAnimationTarget = Location;
            _openAnimationFrame = 0;
            Location = new Point(_openAnimationTarget.X, _openAnimationTarget.Y + OpenAnimationOffsetY);
            Opacity = 0;

            _openAnimationTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _openAnimationTimer.Tick += (s, e) =>
            {
                _openAnimationFrame++;
                double progress = Math.Min(1.0, (double)_openAnimationFrame / OpenAnimationFrames);
                double eased = 1.0 - Math.Pow(1.0 - progress, 3);

                int y = _openAnimationTarget.Y + (int)Math.Round(OpenAnimationOffsetY * (1.0 - eased));
                Location = new Point(_openAnimationTarget.X, y);
                Opacity = eased;

                if (progress >= 1.0)
                {
                    Location = _openAnimationTarget;
                    Opacity = 1;
                    _openAnimationTimer?.Stop();
                    _openAnimationTimer?.Dispose();
                    _openAnimationTimer = null;
                }
            };
            _openAnimationTimer.Start();
        }

        private void StartCloseAnimation()
        {
            _openAnimationTimer?.Stop();
            _openAnimationTimer?.Dispose();
            _openAnimationTimer = null;

            _closeAnimationTimer?.Stop();
            _closeAnimationTimer?.Dispose();

            _closeAnimationStart = Location;
            _closeAnimationFrame = 0;

            _closeAnimationTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _closeAnimationTimer.Tick += (s, e) =>
            {
                _closeAnimationFrame++;
                double progress = Math.Min(1.0, (double)_closeAnimationFrame / CloseAnimationFrames);
                double eased = 1.0 - Math.Pow(1.0 - progress, 3);

                int y = _closeAnimationStart.Y + (int)Math.Round(OpenAnimationOffsetY * eased);
                Location = new Point(_closeAnimationStart.X, y);
                Opacity = 1.0 - eased;

                if (progress >= 1.0)
                {
                    _closeAnimationTimer?.Stop();
                    _closeAnimationTimer?.Dispose();
                    _closeAnimationTimer = null;
                    _isClosingWithAnimation = true;
                    Close();
                }
            };
            _closeAnimationTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isClosingWithAnimation)
            {
                e.Cancel = true;
                StartCloseAnimation();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _openAnimationTimer?.Stop();
            _openAnimationTimer?.Dispose();
            _openAnimationTimer = null;
            _closeAnimationTimer?.Stop();
            _closeAnimationTimer?.Dispose();
            _closeAnimationTimer = null;
            base.OnFormClosed(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Blur efektinin görünmesi için varsayılan arka plan boyamasını kapatıyoruz.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var menuColor = Color.FromArgb(Form1.CurrentSettings.MenuColorArgb);
            using (var path = RoundedRect(rect, 16))
            using (var fillBrush = new SolidBrush(Color.FromArgb(175, menuColor.R, menuColor.G, menuColor.B)))
            using (var borderPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1.2f))
            using (var glossBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect,
                Color.FromArgb(48, 255, 255, 255),
                Color.FromArgb(8, 255, 255, 255),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                g.FillPath(fillBrush, path);
                g.FillPath(glossBrush, path);
                g.DrawPath(borderPen, path);
            }
        }

        private void EnableBlur()
        {
            if (!IsHandleCreated)
                return;

            int cornerPref = 2;
            DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                GradientColor = 0x55181818
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        private static Icon GetSystemIcon(int index)
        {
            try
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, path, index);
                if (hIcon != IntPtr.Zero)
                {
                    return Icon.FromHandle(hIcon);
                }
            }
            catch { }
            return Form1.GetGenericApplicationIcon();
        }

        private static List<InstalledApp> GetInstalledAppsSnapshot()
        {
            lock (InstalledAppsCacheLock)
            {
                if (_installedAppsCache != null && DateTime.UtcNow - _installedAppsCacheTime < InstalledAppsCacheDuration)
                {
                    return _installedAppsCache;
                }
            }

            var list = new List<InstalledApp>();
            AddDefaultInstalledApps(list);
            return list;
        }

        public static void WarmInstalledAppsCacheAsync()
        {
            lock (InstalledAppsCacheLock)
            {
                if (_isInstalledAppsCacheWarming ||
                    (_installedAppsCache != null && DateTime.UtcNow - _installedAppsCacheTime < InstalledAppsCacheDuration))
                {
                    return;
                }

                _isInstalledAppsCacheWarming = true;
            }

            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    GetInstalledApps(forceRefresh: true);
                }
                finally
                {
                    lock (InstalledAppsCacheLock)
                    {
                        _isInstalledAppsCacheWarming = false;
                    }
                }
            });
        }

        private static List<string> SafeGetFiles(string path, string searchPattern)
        {
            var files = new List<string>();
            try
            {
                files.AddRange(System.IO.Directory.GetFiles(path, searchPattern));
                foreach (var directory in System.IO.Directory.GetDirectories(path))
                {
                    files.AddRange(SafeGetFiles(directory, searchPattern));
                }
            }
            catch { }
            return files;
        }

        private static List<InstalledApp> GetInstalledApps(bool forceRefresh = false)
        {
            lock (InstalledAppsCacheLock)
            {
                if (!forceRefresh &&
                    _installedAppsCache != null &&
                    DateTime.UtcNow - _installedAppsCacheTime < InstalledAppsCacheDuration)
                {
                    return _installedAppsCache;
                }
            }

            var list = new List<InstalledApp>();
            AddDefaultInstalledApps(list);

            var registryList = new List<InstalledApp>();
            string[] registryKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            var roots = new[] { Microsoft.Win32.Registry.LocalMachine, Microsoft.Win32.Registry.CurrentUser };

            foreach (var root in roots)
            {
                foreach (var keyPath in registryKeys)
                {
                    try
                    {
                        using (var key = root.OpenSubKey(keyPath))
                        {
                            if (key == null) continue;
                            foreach (var subkeyName in key.GetSubKeyNames())
                            {
                                try
                                {
                                    using (var subkey = key.OpenSubKey(subkeyName))
                                    {
                                        if (subkey == null) continue;
                                        string displayName = subkey.GetValue("DisplayName") as string ?? "";
                                        string displayIcon = subkey.GetValue("DisplayIcon") as string ?? "";
                                        string installLocation = subkey.GetValue("InstallLocation") as string ?? "";

                                        if (string.IsNullOrWhiteSpace(displayName)) continue;

                                        // Geliştirici kitleri, python konsolları, gbt ve sürücüleri temizle
                                        bool isBlacklisted = false;
                                        string[] blacklist = {
                                            "vanguard", "runtime", "redistributable", "directx", "driver",
                                            "update", "framework", "library", "sdk", "windows-sdk", "developer pack",
                                            "redist", "anti-cheat", "anticheat", "vc++", "visual c++", "geforce experience",
                                            "gbt", "development kit", "python", "software development", "targeted pack",
                                            "targeting pack", "clickonce", "microsoft build", "diagnostic profile"
                                        };
                                        foreach (var word in blacklist)
                                        {
                                            if (displayName.Contains(word, StringComparison.OrdinalIgnoreCase))
                                            {
                                                isBlacklisted = true;
                                                break;
                                            }
                                        }
                                        if (isBlacklisted) continue;
                                        if (subkey.GetValue("SystemComponent") != null) continue;
                                        if (subkey.GetValue("ParentKeyName") != null) continue;

                                        string exePath = "";
                                        if (!string.IsNullOrWhiteSpace(displayIcon))
                                        {
                                            int commaIndex = displayIcon.LastIndexOf(',');
                                            if (commaIndex > 0)
                                                exePath = displayIcon.Substring(0, commaIndex).Trim('"', ' ');
                                            else
                                                exePath = displayIcon.Trim('"', ' ');
                                        }

                                        string savedIconPath = "";
                                        bool isIconOrImage = !string.IsNullOrWhiteSpace(exePath) &&
                                            (exePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                                             exePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                             exePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                             exePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                             exePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));

                                        if (isIconOrImage)
                                        {
                                            savedIconPath = exePath;
                                            exePath = "";
                                        }

                                        if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
                                        {
                                            if (!string.IsNullOrWhiteSpace(installLocation) && System.IO.Directory.Exists(installLocation))
                                            {
                                                try
                                                {
                                                    var files = System.IO.Directory.GetFiles(installLocation, "*.exe");
                                                    if (files.Length > 0)
                                                    {
                                                        string bestMatch = "";
                                                        foreach (var file in files)
                                                        {
                                                            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file);
                                                            if (displayName.Contains(nameWithoutExt, StringComparison.OrdinalIgnoreCase) ||
                                                                nameWithoutExt.Contains(displayName, StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                bestMatch = file;
                                                                break;
                                                            }
                                                        }
                                                        exePath = !string.IsNullOrEmpty(bestMatch) ? bestMatch : files[0];
                                                    }
                                                }
                                                catch { }
                                            }
                                        }

                                        if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
                                            continue;

                                        if (list.Exists(x => x.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase) ||
                                                             x.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)) ||
                                            registryList.Exists(x => x.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase) ||
                                                                     x.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            continue;
                                        }

                                        Icon? icon = null;
                                        try
                                        {
                                            if (!string.IsNullOrEmpty(savedIconPath) && System.IO.File.Exists(savedIconPath))
                                            {
                                                if (savedIconPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    icon = new Icon(savedIconPath);
                                                }
                                                else
                                                {
                                                    using (var bmp = new Bitmap(savedIconPath))
                                                    {
                                                        IntPtr hIcon = bmp.GetHicon();
                                                        icon = (Icon)Icon.FromHandle(hIcon).Clone();
                                                        Form1.DestroyIcon(hIcon);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                icon = Icon.ExtractAssociatedIcon(exePath);
                                            }
                                        }
                                        catch { }

                                        if (icon == null) icon = Form1.GetGenericApplicationIcon();

                                        registryList.Add(new InstalledApp
                                        {
                                            Name = displayName,
                                            ExePath = exePath,
                                            AppIcon = icon
                                        });
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3. Scan Start Menu shortcuts to include all modern UWP/Windows Store and other applications
            var shortcutList = new List<InstalledApp>();
            var startMenuDirs = new List<string>();
            try
            {
                startMenuDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs"));
            }
            catch { }
            try
            {
                startMenuDirs.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs"));
            }
            catch { }

            foreach (var dir in startMenuDirs)
            {
                if (!System.IO.Directory.Exists(dir))
                    continue;

                try
                {
                    var files = SafeGetFiles(dir, "*.lnk");
                    foreach (var file in files)
                    {
                        try
                        {
                            string displayName = System.IO.Path.GetFileNameWithoutExtension(file);

                            // Skip common non-application shortcuts and duplicate shortcuts
                            string[] linkBlacklist = { 
                                "uninstall", "help", "manual", "documentation", "readme", "license", 
                                "visit", "website", "web site", "setup", "install", "configuration",
                                "about", "support", "troubleshoot", "feedback"
                            };

                            bool isBlacklisted = false;
                            foreach (var word in linkBlacklist)
                            {
                                if (displayName.Contains(word, StringComparison.OrdinalIgnoreCase))
                                {
                                    isBlacklisted = true;
                                    break;
                                }
                            }
                            if (isBlacklisted) continue;

                            // Avoid adding duplicates of apps already found in default list or registryList
                            if (list.Exists(x => x.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)) ||
                                registryList.Exists(x => x.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)) ||
                                shortcutList.Exists(x => x.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            // Load target icon using SHGetFileInfo
                            Icon? icon = null;
                            try
                            {
                                Form1.SHFILEINFO shinfo = new Form1.SHFILEINFO();
                                IntPtr hImg = Form1.SHGetFileInfo(file, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Form1.SHGFI_ICON | Form1.SHGFI_LARGEICON);
                                if (shinfo.hIcon != IntPtr.Zero)
                                {
                                    icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                                    Form1.DestroyIcon(shinfo.hIcon);
                                }
                            }
                            catch { }

                            if (icon == null)
                            {
                                icon = Form1.GetGenericApplicationIcon();
                            }

                            shortcutList.Add(new InstalledApp
                            {
                                Name = displayName,
                                ExePath = file, // Lnk files will run via shell execute automatically
                                AppIcon = icon
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }

            registryList.AddRange(shortcutList);
            registryList.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            list.AddRange(registryList);

            lock (InstalledAppsCacheLock)
            {
                _installedAppsCache = list;
                _installedAppsCacheTime = DateTime.UtcNow;
            }

            return list;
        }

        private static void AddDefaultInstalledApps(List<InstalledApp> list)
        {
            // Entegre Windows Sistem Araçları (Manuel Ekleme)
            list.Add(new InstalledApp
            {
                Name = "Ayarlar",
                ExePath = "ms-settings:",
                AppIcon = GetSystemIcon(21) // Çark (Settings) ikonu
            });

            list.Add(new InstalledApp
            {
                Name = "Microsoft Store",
                ExePath = "ms-windows-store://home",
                AppIcon = GetSystemIcon(14) // Mağaza/Paket ikonu
            });

            list.Add(new InstalledApp
            {
                Name = "Denetim Masası",
                ExePath = "control.exe",
                AppIcon = GetSystemIcon(26) // Denetim masası ikonu
            });

            AddInstalledAppIfExists(
                list,
                "Notepad",
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "notepad.exe"));

            AddInstalledAppIfExists(
                list,
                "Notepad++",
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Notepad++", "notepad++.exe"));

            AddInstalledAppIfExists(
                list,
                "Notepad++",
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Notepad++", "notepad++.exe"));
        }

        private static void AddInstalledAppIfExists(List<InstalledApp> list, string name, string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
                return;

            if (list.Exists(app =>
                    app.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    app.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Icon? icon = null;
            try
            {
                icon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch { }

            list.Add(new InstalledApp
            {
                Name = name,
                ExePath = exePath,
                AppIcon = icon ?? Form1.GetGenericApplicationIcon()
            });
        }

        private static void UpdateThumb(FlowLayoutPanel flp, Panel thumb, int trackHeight)
        {
            if (flp.IsDisposed || thumb.IsDisposed) return;

            int maxScroll = flp.VerticalScroll.Maximum - flp.ClientRectangle.Height;
            if (maxScroll <= 0)
            {
                thumb.Visible = false;
                return;
            }
            thumb.Visible = true;

            int thumbHeight = Math.Max(30, (flp.ClientRectangle.Height * trackHeight) / flp.VerticalScroll.Maximum);
            thumb.Height = thumbHeight;
            thumb.Region = new Region(RoundedRect(new Rectangle(0, 0, thumb.Width, thumbHeight), 3));

            float percent = (float)flp.VerticalScroll.Value / maxScroll;
            int thumbY = (int)(percent * (trackHeight - thumbHeight));
            thumb.Location = new Point(0, thumbY);
        }


        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ClockForm : Form
    {
        private Label _timeLabel;
        private System.Windows.Forms.Timer _timer;

        public ClockForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black; // Make window background fully transparent
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(180, 60);
            this.StartPosition = FormStartPosition.Manual;

            // Position at top-right corner of primary screen with 20px offset
            var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            this.Location = new Point(screen.Width - this.Width - 20, 20);

            _timeLabel = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(_timeLabel);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => UpdateTime();
            _timer.Start();

            UpdateTime();
        }

        private void UpdateTime()
        {
            _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE - Click-through / no focus
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW - Hide from Alt+Tab
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT - Click-through (mouse events pass to desktop)
                return cp;
            }
        }
    }
}
