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
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS { public int Left, Right, Top, Bottom; }

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

        // Her dock ikonu için ayrý, baðýmsýz (top-level) bir pencere.
        // Parent Form'un Region/clip sýnýrlarýna takýlmadan taþabilmesi için
        // PictureBox'ý doðrudan Form1'e eklemek yerine kendi penceresine koyuyoruz.
        private class IconWindow : Form
        {
            private Bitmap? _originalBitmap; // asla küçültülmemiþ, her zaman kaliteli kaynak
            private Bitmap? _bitmap;         // o an ekranda gösterilen, ölçeklenmiþ versiyon

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

            // Z-order'ý gerçekten öne getirmek için (TopMost toggle'ý yeterli olmuyordu,
            // çünkü tüm ikon pencereleri zaten TopMost = true idi).
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

            public IconWindow()
            {
                AutoScaleMode = AutoScaleMode.None; // DPI'nin ilk boyutu þiþirmesini engelle
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
            }

            // PNG'nin gerçek alpha kanalýný kullanarak pencereyi çiziyoruz.
            // Magenta yok, renk-anahtarý yok — kenarlar pürüzsüz.
            public void SetIconImage(Bitmap bmp, int targetSize)
            {
                _originalBitmap?.Dispose();
                _originalBitmap = new Bitmap(bmp); // pristine kopya, bir daha dokunulmayacak

                RescaleFromOriginal(targetSize);
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
                Size = _bitmap.Size;
                Redraw();
            }

            private void Redraw()
            {
                if (_bitmap == null || !IsHandleCreated) return;

                IntPtr screenDc = GetDC(IntPtr.Zero);
                IntPtr memDc = CreateCompatibleDC(screenDc);
                IntPtr hBitmap = _bitmap.GetHbitmap(Color.FromArgb(0));
                IntPtr oldBitmap = SelectObject(memDc, hBitmap);

                var size = new SIZE(_bitmap.Width, _bitmap.Height);
                var pointSource = new POINT(0, 0);
                var topPos = new POINT(Left, Top);
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

            // Boyut deðiþtiðinde (hover animasyonunda) bitmap'i yeniden ölçekleyip çiz
            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                if (_originalBitmap != null && Width > 0 && Height > 0)
                {
                    // HER ZAMAN orijinal kaynaktan çiz, bir önceki ölçeklenmiþ
                    // sonuçtan deðil — kalite kaybý birikmesin.
                    RescaleFromOriginal(Width);
                }
            }

            protected override void OnLocationChanged(EventArgs e)
            {
                base.OnLocationChanged(e);
                Redraw();
            }

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

        // Animasyon durumlarýný ve hedef deðerleri saklayan güncel veri sýnýfý
        private class DockItemData
        {
            public IntPtr WindowHandle { get; set; }
            public Point OriginalLocation { get; set; } // Ekran koordinatý
            public Size OriginalSize { get; set; }

            public double CurrentProgress { get; set; } = 0; // 0.0 (en küçük) ile 1.0 (en büyük) arasý
            public bool IsHovered { get; set; } = false;
            public System.Windows.Forms.Timer AnimTimer { get; set; } = null!;
            public IconWindow Owner { get; set; } = null!;
        }

        private readonly List<DockItemData> _dockItems = new();
        private System.Windows.Forms.Timer _hoverCheckTimer = null!;

        // Fareyi her ikonun SABÝT orijinal (büyümeden önceki) alanýna göre kontrol eder.
        // Böylece büyüyen/kayan pencere sýnýrlarý hover durumunu etkilemez, titreme biter.
        private void CheckHoverStates()
        {
            Point cursor = Cursor.Position;
            int padding = 10; // biraz tolerans, tam kenarda titremeyi de önler

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

            if (Screen.PrimaryScreen == null)
            {
                MessageBox.Show("error : PrimaryScreen is NULL", "Error");
                return;
            }
            Rectangle workspace = Screen.PrimaryScreen.WorkingArea;
            int x = (workspace.Width - this.Width) / 2;
            int y = workspace.Height - this.Height - 10;
            this.Location = new Point(x, y);

            int softness = 20;
            this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, softness, softness));
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
            this.ResumeLayout();
        }

        private void RefreshDockIcons()
        {
            // Önce eski ikon pencerelerini ve timer'larýný temizle
            foreach (var item in _dockItems)
            {
                item.AnimTimer.Stop();
                item.AnimTimer.Dispose();
                item.Owner.Close();
                item.Owner.Dispose();
            }
            _dockItems.Clear();

            int startX = 25;      // Sol boþluk
            int spacing = 15;     // Ýkonlar arasý mesafe
            int baseSize = 32;    // Baþlangýç boyutu
            int defaultY = this.Height - baseSize - 12;

            var seenProcessNames = new HashSet<string>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainWindowHandle == IntPtr.Zero) continue;
                    if (string.IsNullOrWhiteSpace(proc.MainWindowTitle)) continue;
                    if (!seenProcessNames.Add(proc.ProcessName)) continue;

                    string? path = proc.MainModule?.FileName;
                    if (string.IsNullOrEmpty(path)) continue;

                    Icon? icon = GetHighQualityIcon(path);
                    if (icon == null) continue;

                    // Dock içindeki göreli konumu ekran koordinatýna çeviriyoruz,
                    // çünkü IconWindow artýk baðýmsýz bir top-level pencere.
                    Point screenLocation = new Point(this.Left + startX, this.Top + defaultY);

                    var iconWindow = new IconWindow
                    {
                        Size = new Size(baseSize, baseSize),
                        Location = screenLocation
                    };
                    iconWindow.SetIconImage(icon.ToBitmap(), baseSize);
                    icon.Dispose(); // ToBitmap kopyaladýktan sonra artýk lazým deðil

                    var animData = new DockItemData
                    {
                        WindowHandle = proc.MainWindowHandle,
                        OriginalLocation = screenLocation,
                        OriginalSize = new Size(baseSize, baseSize),
                        Owner = iconWindow,
                        AnimTimer = new System.Windows.Forms.Timer { Interval = 15 }
                    };

                    animData.AnimTimer.Tick += (s, e) => UpdateAnimation(animData);

                    iconWindow.Cursor = Cursors.Hand;
                    iconWindow.Click += (s, e) => FocusWindow(animData.WindowHandle);

                    iconWindow.Show(this); // this = sahibi (owner), her zaman dock'un üzerinde durur
                    iconWindow.Size = new Size(baseSize, baseSize); // handle oluþturulduktan sonra boyutu garanti altýna al
                    _dockItems.Add(animData);
                    startX += baseSize + spacing;
                }
                catch { continue; }
                finally { proc.Dispose(); }
            }
        }

        // Animasyon Hesaplama Motoru (Delta Zamanlý Geçiþ)
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

            // Baðýmsýz pencere olduðu için taþma sýnýrlamasý YOK — ikon dock'un
            // üstüne, hatta ekranýn üst kenarýna kadar serbestçe taþabilir.
            int maxYukseklik = 24;
            int currentY = data.OriginalLocation.Y - (int)(maxYukseklik * data.CurrentProgress);

            int currentX = data.OriginalLocation.X - (currentSize - minSize) / 2;

            data.Owner.Size = new Size(currentSize, currentSize);
            data.Owner.Location = new Point(currentX, currentY);
        }

        private void PicBox_MouseEnter(DockItemData data)
        {
            data.IsHovered = true;
            data.Owner.BringToTopNoActivate(); // gerçek z-order güncellemesi — focus çalmadan en öne getirir
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
                // GDI'nýn düz siyah dolgusunu engelle — Mica'nýn üzerine boyanmasýn.
                // "Ýþlendi" diyoruz ama hiçbir þey çizmiyoruz, DWM zaten arkada composite ediyor.
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        private void EnableBlur()
        {
            // Mica'nýn render olabilmesi için frame client alana "uzatýlmalý" —
            // bu olmadan DWM hiç composite etmiyor, sadece çýplak pencere kalýyor.
            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(this.Handle, ref margins);

            int backdrop = 4; // DWMSBT_TABBEDWINDOW — Mica'dan daha yoðun blur, cam hissi daha güçlü
            DwmSetWindowAttribute(this.Handle, 38, ref backdrop, sizeof(int));

            int cornerPref = 2;
            DwmSetWindowAttribute(this.Handle, 33, ref cornerPref, sizeof(int));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int softness = 20;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = RoundedRect(rect, softness))
            {
                // Hafif üstten-alta gradient — cam üzerindeki ýþýk yansýmasý hissi
                using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect, Color.FromArgb(28, 255, 255, 255), Color.FromArgb(6, 255, 255, 255),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillPath(gradient, path);
                }

                // Ýnce, yarý saydam kenarlýk — camýn kenar çizgisi
                using (var borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                {
                    g.DrawPath(borderPen, path);
                }
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