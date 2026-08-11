using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OpenDock
{
    public partial class Form1 : Form
    {
        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

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

            IntPtr owner = GetWindow(hWnd, GW_OWNER);
            if (owner != IntPtr.Zero) return false;

            long exStyle = GetWindowLong(hWnd, GWL_EXSTYLE).ToInt64();
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
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
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;

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

                int windowWidth = size;
                int windowHeight = size;
                int drawX = 0;
                int drawY = 0;
                int windowX = x;
                int windowY = y;

                bool showText = (size >= 48 && !string.IsNullOrEmpty(_appName));

                if (showText)
                {
                    windowWidth = 150;
                    windowHeight = size + 20; // 48 + 20 = 68
                    drawX = (windowWidth - size) / 2; // Center icon horizontally
                    drawY = 20; // Push icon down to leave space for text
                    windowX = x - (windowWidth - size) / 2;
                    windowY = y - 20;
                }
                else
                {
                    _scrollOffset = 0f; // Reset scroll when not hovered or sizing up
                }

                var scaled = new Bitmap(windowWidth, windowHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Draw the icon
                    g.DrawImage(_originalBitmap, drawX, drawY, size, size);

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

        // Animasyon durumlar�n� ve hedef de�erleri saklayan g�ncel veri s�n�f�
        private class DockItemData
        {
            public IntPtr WindowHandle { get; set; }
            public Point OriginalLocation { get; set; } // Ekran koordinat�
            public Size OriginalSize { get; set; }

            public double CurrentProgress { get; set; } = 0; // 0.0 (en k���k) ile 1.0 (en b�y�k) aras�
            public bool IsHovered { get; set; } = false;
            public System.Windows.Forms.Timer AnimTimer { get; set; } = null!;
            public IconWindow Owner { get; set; } = null!;
        }

        private readonly List<DockItemData> _dockItems = new();
        private const int MaxIconSize = 48; // pencerelerin SABİT, hiç değişmeyen tuval boyutu
        private System.Windows.Forms.Timer _hoverCheckTimer = null!;
        private NotifyIcon? _trayIcon;
        private int _separatorX = -1;

        // Fareyi her ikonun SABT orijinal (bymeden nceki) alanna gre kontrol eder.
        // Bylece byyen/kayan pencere snrlar hover durumunu etkilemez, titreme biter.
        private void CheckHoverStates()
        {
            Point cursor = Cursor.Position;
            int padding = 10; // biraz tolerans, tam kenarda titremeyi de nler

            foreach (var item in _dockItems)
            {
                var rect = new Rectangle(
                    item.OriginalLocation.X - padding,
                    item.OriginalLocation.Y - padding,
                    item.OriginalSize.Width + padding * 2,
                    item.OriginalSize.Height + padding * 2);

                bool isOver = rect.Contains(cursor);

                if (isOver && !item.IsHovered)
                    PicBox_MouseEnter(item);
                else if (!isOver && item.IsHovered)
                    PicBox_MouseLeave(item);
            }
        }

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(980, 50);
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(0, 0, 0);
            this.TopMost = true;
            this.ShowInTaskbar = false;

            if (Screen.PrimaryScreen == null)
            {
                MessageBox.Show("error : PrimaryScreen is NULL", "Error");
                return;
            }
            Rectangle workspace = Screen.PrimaryScreen.WorkingArea;
            int x = (workspace.Width - this.Width) / 2;
            int y = workspace.Height - this.Height - 10;
            this.Location = new Point(x, y);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.SuspendLayout();
            RefreshDockIcons();
            EnableBlur();
            _hoverCheckTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _hoverCheckTimer.Tick += (s, e) => CheckHoverStates();
            _hoverCheckTimer.Start();
            this.Activated += (s, e) => EnableBlur();
            this.Deactivate += (s, e) => EnableBlur();
            InitializeTrayIcon();
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
                item.Owner.Close();
                item.Owner.Dispose();
            }
            _dockItems.Clear();

            int startX = 25;      // Sol boşluk
            int spacing = 15;     // ikonlar arası mesafe
            int baseSize = 32;    // Başlangıç boyutu
            int defaultY = this.Height - baseSize - 12;

            var seenProcessNames = new HashSet<string>();
            var windowList = new List<IntPtr>();

            EnumWindows((hWnd, lParam) =>
            {
                windowList.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            foreach (IntPtr hwnd in windowList)
            {
                IconWindow? iconWindow = null;
                Process? proc = null;
                try
                {
                    if (hwnd == IntPtr.Zero) continue;

                    string title = GetWindowTitle(hwnd);
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid == 0) continue;

                    try
                    {
                        proc = Process.GetProcessById((int)pid);
                    }
                    catch
                    {
                        continue;
                    }

                    if (proc == null) continue;

                    string procName = proc.ProcessName;
                    if (procName.Equals("OpenDock", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (procName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                    {
                        string className = GetWindowClassName(hwnd);
                        if (!className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) &&
                            !className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip explorer taskbars, start menus, backgrounds, tray, etc.
                        }
                    }

                    if (!ShouldShowProcessWindow(hwnd)) continue;

                    if (!seenProcessNames.Add(procName)) continue;

                    string path = GetProcessFilePath(proc);
                    if (string.IsNullOrEmpty(path)) continue;

                    Icon? icon = GetHighQualityIcon(path);
                    if (icon == null) continue;

                    // Dock içindeki göreli konumu ekran koordinatına çeviriyoruz,
                    // çünkü IconWindow artık bağımsız bir top-level pencere.
                    Point screenLocation = new Point(this.Left + startX, this.Top + defaultY);
                    iconWindow = new IconWindow(MaxIconSize);
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

                    iconWindow.SetIconImage(icon.ToBitmap(), baseSize, displayName);
                    icon.Dispose(); // ToBitmap kopyaladıktan sonra artık lazım değil

                    var animData = new DockItemData
                    {
                        WindowHandle = hwnd,
                        OriginalLocation = screenLocation,
                        OriginalSize = new Size(baseSize, baseSize),
                        Owner = iconWindow,
                        AnimTimer = new System.Windows.Forms.Timer { Interval = 15 }
                    };

                    animData.AnimTimer.Tick += (s, e) => UpdateAnimation(animData);

                    iconWindow.Cursor = Cursors.Hand;
                    iconWindow.Click += (s, e) => FocusWindow(animData.WindowHandle);

                    iconWindow.Show(this); // this = sahibi (owner), her zaman dock'un üzerinde durur
                    iconWindow.UpdateBounds(screenLocation.X, screenLocation.Y, baseSize); // handle artık var - ilk çizimi garanti altına al
                    _dockItems.Add(animData);
                    startX += baseSize + spacing;
                }
                catch
                {
                    if (iconWindow != null)
                    {
                        iconWindow.Close();
                        iconWindow.Dispose();
                    }
                    continue;
                }
                finally
                {
                    proc?.Dispose();
                }
            }

            // Draw a separator line and place the Windows button at the very right of the dock
            int winButtonX = this.Width - baseSize - 25;
            _separatorX = winButtonX - 15;

            try
            {
                Point screenLocation = new Point(this.Left + winButtonX, this.Top + defaultY);
                var winIconWindow = new IconWindow(MaxIconSize);
                winIconWindow.SetIconImage(GetWindowsLogoBitmap(baseSize), baseSize, "Menü");

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

        private Bitmap GetWindowsLogoBitmap(int size)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Draw modern Windows 11 logo (4 white/semi-transparent squares)
                using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                {
                    float gap = size * 0.08f;
                    float half = (size - gap) / 2f;

                    // Top Left
                    g.FillRectangle(brush, 0, 0, half, half);
                    // Top Right
                    g.FillRectangle(brush, half + gap, 0, half, half);
                    // Bottom Left
                    g.FillRectangle(brush, 0, half + gap, half, half);
                    // Bottom Right
                    g.FillRectangle(brush, half + gap, half + gap, half, half);
                }
            }
            return bmp;
        }

        // Animasyon Hesaplama Motoru (Delta Zamanlı Geçiş)
        private void UpdateAnimation(DockItemData data)
        {
            double step = 0.12;

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
                    data.AnimTimer.Stop();
                }
            }

            int minSize = data.OriginalSize.Width;
            int maxSize = 48;
            int currentSize = minSize + (int)((maxSize - minSize) * data.CurrentProgress);

            // Bamsz pencere olduu iin tama snrlamas YOK  ikon dock'un
            // stne, hatta ekrann st kenarna kadar serbeste taabilir.
            int maxYukseklik = 24;
            int currentY = data.OriginalLocation.Y - (int)(maxYukseklik * data.CurrentProgress);

            int currentX = data.OriginalLocation.X - (currentSize - minSize) / 2;

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
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

            if (shinfo.hIcon != IntPtr.Zero)
            {
                Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                DestroyIcon(shinfo.hIcon);
                return icon;
            }
            return null;
        }

        private void FocusWindow(IntPtr hWnd)
        {
            const int SW_RESTORE = 9;
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }

        private const int WM_ERASEBKGND = 0x0014;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                // GDI'nn dz siyah dolgusunu engelle  Mica'nn zerine boyanmasn.
                // "lendi" diyoruz ama hibir ey izmiyoruz, DWM zaten arkada composite ediyor.
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // DWM arka plan (Mica/Acrylic) efektinin görünmesi için arka planı düz renkle boyamıyoruz.
        }

        private void EnableBlur()
        {
            // DWM corner rounding
            int cornerPref = 2; // DWMWCP_ROUND - Köşeleri yuvarla
            DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));

            // Set Window Composition Accent Policy for Acrylic Blur
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = 0x60121212 // Yarı saydam koyu cam tonu (Acrylic) ve arkadaki pencerelerin renklerini blurlama
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
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int softness = 8;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = RoundedRect(rect, softness))
            {
                // Hafif stten-alta gradient  cam zerindeki k yansmas hissi
                using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect, Color.FromArgb(28, 255, 255, 255), Color.FromArgb(6, 255, 255, 255),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillPath(gradient, path);
                }

                // İnce, yarı saydam kenarlık - camın kenar çizgisi
                using (var borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw vertical separator line for the Windows button
            if (_separatorX > 0)
            {
                using (var separatorPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
                {
                    g.DrawLine(separatorPen, _separatorX, 10, _separatorX, Height - 10);
                }
            }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "OpenDock";
            _trayIcon.Icon = this.Icon ?? SystemIcons.Application;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Yenile", null, (s, e) => RefreshDockIcons());
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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
                _startMenu = null;
                return;
            }

            _startMenu = new StartMenuForm();
            _startMenu.StartPosition = FormStartPosition.Manual;
            
            int w = 350;
            int h = 450;
            // Center above Windows button and offset up by height + 15px
            int x = buttonScreenLoc.X - (w / 2) + 24;
            int y = buttonScreenLoc.Y - h - 15;
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

        public StartMenuForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(28, 28, 28); // Windows 11 Koyu Tema Rengi
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Deactivate += (s, e) => this.Close();

            // Set soft rounded corners manually using window region clipping (16px radius)
            this.Region = new Region(RoundedRect(new Rectangle(0, 0, 350, 450), 16));

            // Add title
            var titleLabel = new Label
            {
                Text = "Uygulamalar",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.Controls.Add(titleLabel);

            // Search Box Placeholder (Borderless inside a painted rounded panel)
            var searchBox = new TextBox
            {
                Width = 294,
                Location = new Point(28, 66), // Positioned inside the rounded region drawn in OnPaint
                Font = new Font("Segoe UI", 11f),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Text = "Aramak için yazın..."
            };
            this.Controls.Add(searchBox);

            // Container Panel to hide default scrollbar via clipping
            var flowLayoutContainer = new Panel
            {
                Width = 300,
                Height = 275,
                Location = new Point(20, 105),
                BackColor = Color.Transparent
            };
            this.Controls.Add(flowLayoutContainer);

            // Pinned Apps flow panel (wider than container to push scrollbar off-screen)
            var flowLayout = new FlowLayoutPanel
            {
                Width = 320,
                Height = 275,
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                AutoScroll = true
            };
            flowLayoutContainer.Controls.Add(flowLayout);

            // Custom modern rounded scrollbar track
            var scrollTrack = new Panel
            {
                Width = 6,
                Height = 275,
                Location = new Point(325, 105),
                BackColor = Color.Transparent
            };
            this.Controls.Add(scrollTrack);

            // Custom modern rounded scrollbar thumb
            var scrollThumb = new Panel
            {
                Width = 6,
                Height = 40,
                BackColor = Color.FromArgb(80, 255, 255, 255),
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
            flowLayout.ControlAdded += (s, e) => UpdateThumb(flowLayout, scrollThumb, scrollTrack.Height);

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
            var installedApps = GetInstalledApps();

            foreach (var app in installedApps)
            {
                Bitmap? iconBmp = null;
                try
                {
                    iconBmp = new Bitmap(app.AppIcon.ToBitmap(), new Size(24, 24));
                }
                catch
                {
                    iconBmp = new Bitmap(SystemIcons.Application.ToBitmap(), new Size(24, 24));
                }

                var btn = new Button
                {
                    Text = app.Name,
                    Width = 285,
                    Height = 40,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(40, 40, 40),
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText, // Yazı ve ikonun iç içe girmesini engeller
                    Padding = new Padding(8, 0, 0, 0), // Kenar boşluğu
                    Font = new Font("Segoe UI", 9.5f),
                    Image = iconBmp
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Region = new Region(RoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), 6)); // Yumuşak buton köşeleri
                btn.Click += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(app.ExePath) { UseShellExecute = true });
                    }
                    catch { }
                    this.Close();
                };
                flowLayout.Controls.Add(btn);
            }

            // Search filtering logic
            searchBox.GotFocus += (s, e) => {
                if (searchBox.Text == "Aramak için yazın...")
                    searchBox.Text = "";
            };
            searchBox.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                    searchBox.Text = "Aramak için yazın...";
            };
            searchBox.TextChanged += (s, e) => {
                string filter = searchBox.Text;
                if (filter == "Aramak için yazın...") filter = "";
                
                foreach (Control ctrl in flowLayout.Controls)
                {
                    if (ctrl is Button btn)
                    {
                        bool matches = string.IsNullOrEmpty(filter) || 
                                       btn.Text.Contains(filter, StringComparison.OrdinalIgnoreCase);
                        btn.Visible = matches;
                    }
                }
            };

            // Power control buttons at the bottom
            var powerBtn = new Button
            {
                Text = "Kapat",
                Width = 80,
                Height = 30,
                Location = new Point(250, 400),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            powerBtn.FlatAppearance.BorderSize = 0;
            powerBtn.Region = new Region(RoundedRect(new Rectangle(0, 0, powerBtn.Width, powerBtn.Height), 6));
            powerBtn.Click += (s, e) => {
                if (MessageBox.Show("Bilgisayarı kapatmak istiyor musunuz?", "Sistem", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("shutdown", "/s /t 0");
                }
            };
            this.Controls.Add(powerBtn);

            var restartBtn = new Button
            {
                Text = "Yeniden Başlat",
                Width = 110,
                Height = 30,
                Location = new Point(130, 400),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            restartBtn.FlatAppearance.BorderSize = 0;
            restartBtn.Region = new Region(RoundedRect(new Rectangle(0, 0, restartBtn.Width, restartBtn.Height), 6));
            restartBtn.Click += (s, e) => {
                if (MessageBox.Show("Bilgisayarı yeniden başlatmak istiyor musunuz?", "Sistem", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("shutdown", "/r /t 0");
                }
            };
            this.Controls.Add(restartBtn);
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
            return SystemIcons.Application;
        }

        private static List<InstalledApp> GetInstalledApps()
        {
            var list = new List<InstalledApp>();

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
                    using (var key = root.OpenSubKey(keyPath))
                    {
                        if (key == null) continue;
                        foreach (var subkeyName in key.GetSubKeyNames())
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

                                if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
                                {
                                    if (!string.IsNullOrWhiteSpace(installLocation) && System.IO.Directory.Exists(installLocation))
                                    {
                                        try
                                        {
                                            var files = System.IO.Directory.GetFiles(installLocation, "*.exe");
                                            if (files.Length > 0) exePath = files[0];
                                        }
                                        catch { }
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
                                    continue;

                                if (registryList.Exists(x => x.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                                    continue;

                                Icon? icon = null;
                                try
                                {
                                    icon = Icon.ExtractAssociatedIcon(exePath);
                                }
                                catch { }

                                if (icon == null) icon = SystemIcons.Application;

                                registryList.Add(new InstalledApp
                                {
                                    Name = displayName,
                                    ExePath = exePath,
                                    AppIcon = icon
                                });
                            }
                        }
                    }
                }
            }

            registryList.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            list.AddRange(registryList);

            return list;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw a subtle white border matching the main dock (using same 16px radius)
            using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 16))
            using (var borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1.5f))
            {
                g.DrawPath(borderPen, path);
            }

            // Draw a rounded background for the Search Box (Y = 60, Height = 28, Width = 310, X = 20)
            using (var searchPath = RoundedRect(new Rectangle(20, 60, 310, 28), 6))
            using (var searchBgBrush = new SolidBrush(Color.FromArgb(45, 45, 45)))
            using (var searchBorderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
            {
                g.FillPath(searchBgBrush, searchPath);
                g.DrawPath(searchBorderPen, searchPath);
            }
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
}
