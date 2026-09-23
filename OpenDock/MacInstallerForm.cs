using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenDock
{
    public sealed class MacInstallerForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Windows Shortcut Creation via COM IShellLink
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214EE-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder ppszFileName);
        }

        // Layout Constants - Pixel Perfect macOS DMG Window
        private const int FormWidth = 640;
        private const int FormHeight = 420;
        private const int AppIconSize = 106;
        private const int FolderIconSize = 124;

        // UI Positions
        private readonly Point _appInitialPos = new(98, 145);
        private readonly Point _folderPos = new(418, 136);
        private Point _appCurrentPos;
        private bool _isDragging = false;
        private Point _dragOffset;
        private bool _isFolderHovered = false;
        private bool _isInstalling = false;
        private bool _installFinished = false;
        private float _installProgress = 0f;
        private string _statusText;

        // Target Installer Metadata
        private readonly string? _targetInstallerExe;
        private readonly NsisMetadata? _installerMetadata;
        private readonly string _appName = "OpenDock";

        // Traffic Light Hover State
        private bool _trafficHovered = false;
        private bool _closeHovered = false;
        private bool _minHovered = false;

        // Visual Assets
        private Image? _appIconImage;
        private Image? _folderImage;
        private readonly bool _isDarkMode;

        public MacInstallerForm(string? targetInstallerExe = null)
        {
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(FormWidth, FormHeight);
            ShowInTaskbar = true;

            _targetInstallerExe = targetInstallerExe;
            if (!string.IsNullOrWhiteSpace(_targetInstallerExe) && File.Exists(_targetInstallerExe))
            {
                if (NsisDetector.TryDetect(_targetInstallerExe, out var meta))
                {
                    _installerMetadata = meta;
                    _appName = meta.Title;
                }
                else
                {
                    _appName = Path.GetFileNameWithoutExtension(_targetInstallerExe);
                }
            }

            Text = _appName + " Installer";
            _statusText = InstallerLocalization.Current.FormatDragInstruction(_appName);

            _isDarkMode = DetectDarkMode();
            BackColor = _isDarkMode ? Color.FromArgb(28, 30, 36) : Color.FromArgb(242, 244, 247);

            // Windows 11 Rounded Corners attribute
            int cornerPref = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33, ref cornerPref, sizeof(int));

            // Set Form Region for crisp rounded corners on all Windows versions
            using (var path = GetRoundedRectPath(new Rectangle(0, 0, FormWidth, FormHeight), 16))
            {
                Region = new Region(path);
            }

            _appCurrentPos = _appInitialPos;
            LoadVisualAssets();

            MouseDown += OnFormMouseDown;
            MouseMove += OnFormMouseMove;
            MouseUp += OnFormMouseUp;
            MouseLeave += (s, e) =>
            {
                if (_trafficHovered)
                {
                    _trafficHovered = false;
                    _closeHovered = false;
                    _minHovered = false;
                    Invalidate(new Rectangle(12, 8, 70, 24));
                }
            };
        }

        private static bool DetectDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                return val is int i && i == 0;
            }
            catch
            {
                return true;
            }
        }

        private void LoadVisualAssets()
        {
            // 1. Load Application Icon / Stamp Extracted Logo
            try
            {
                if (!string.IsNullOrWhiteSpace(_targetInstallerExe) && File.Exists(_targetInstallerExe))
                {
                    var rawLogo = InstallerLogoExtractor.ExtractRawLogo(_targetInstallerExe, 256);
                    if (rawLogo != null)
                    {
                        _appIconImage = InstallerLogoExtractor.CreateStampedMacSquircle(rawLogo, AppIconSize);
                    }
                }

                if (_appIconImage == null)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string iconPng = Path.Combine(baseDir, "icon.png");

                    if (File.Exists(iconPng))
                    {
                        using var raw = Image.FromFile(iconPng);
                        _appIconImage = InstallerLogoExtractor.CreateStampedMacSquircle(raw, AppIconSize);
                    }
                    else
                    {
                        var asm = Assembly.GetExecutingAssembly();
                        using var stream = asm.GetManifestResourceStream("OpenDock.icon.png");
                        if (stream != null)
                        {
                            using var raw = Image.FromStream(stream);
                            _appIconImage = InstallerLogoExtractor.CreateStampedMacSquircle(raw, AppIconSize);
                        }
                        else
                        {
                            string icoPath = Path.Combine(baseDir, "icon.ico");
                            if (File.Exists(icoPath))
                            {
                                using var ico = new Icon(icoPath, 128, 128);
                                using var raw = ico.ToBitmap();
                                _appIconImage = InstallerLogoExtractor.CreateStampedMacSquircle(raw, AppIconSize);
                            }
                        }
                    }
                }
            }
            catch
            {
                _appIconImage = null;
            }

            // 2. Load Folder Icon (User's high-res macOS folder PNG)
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string folderPng = Path.Combine(baseDir, "folder.png");

                if (File.Exists(folderPng))
                {
                    _folderImage = Image.FromFile(folderPng);
                }
                else
                {
                    var asm = Assembly.GetExecutingAssembly();
                    using var stream = asm.GetManifestResourceStream("OpenDock.folder.png");
                    if (stream != null)
                    {
                        _folderImage = Image.FromStream(stream);
                    }
                    else
                    {
                        string altPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "folder.png"));
                        if (File.Exists(altPath))
                        {
                            _folderImage = Image.FromFile(altPath);
                        }
                    }
                }
            }
            catch
            {
                _folderImage = null;
            }
        }

        private void OnFormMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            // Check Traffic Light Close Button
            if (new Rectangle(16, 13, 14, 14).Contains(e.Location))
            {
                Close();
                return;
            }

            // Check Traffic Light Minimize Button
            if (new Rectangle(36, 13, 14, 14).Contains(e.Location))
            {
                WindowState = FormWindowState.Minimized;
                return;
            }

            // If already installing, ignore icon drags
            if (_isInstalling || _installFinished)
                return;

            // Check App Icon Drag Start
            var appRect = new Rectangle(_appCurrentPos.X, _appCurrentPos.Y, AppIconSize, AppIconSize);
            if (appRect.Contains(e.Location))
            {
                _isDragging = true;
                _dragOffset = new Point(e.X - _appCurrentPos.X, e.Y - _appCurrentPos.Y);
                Cursor = Cursors.Hand;
                Invalidate();
                return;
            }

            // Title Bar Dragging
            if (e.Y <= 38)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        private void OnFormMouseMove(object? sender, MouseEventArgs e)
        {
            // Traffic Light Hover detection
            bool wasHovered = _trafficHovered;
            bool wasClose = _closeHovered;
            bool wasMin = _minHovered;

            _trafficHovered = new Rectangle(12, 8, 70, 24).Contains(e.Location);
            _closeHovered = new Rectangle(16, 13, 14, 14).Contains(e.Location);
            _minHovered = new Rectangle(36, 13, 14, 14).Contains(e.Location);

            if (wasHovered != _trafficHovered || wasClose != _closeHovered || wasMin != _minHovered)
            {
                Invalidate(new Rectangle(12, 8, 70, 24));
            }

            if (!_isDragging)
                return;

            _appCurrentPos = new Point(e.X - _dragOffset.X, e.Y - _dragOffset.Y);

            // Calculate Distance to Applications Folder for Auto-Detection
            var folderCenter = new Point(_folderPos.X + FolderIconSize / 2, _folderPos.Y + FolderIconSize / 2);
            var appCenter = new Point(_appCurrentPos.X + AppIconSize / 2, _appCurrentPos.Y + AppIconSize / 2);
            double distance = Math.Sqrt(Math.Pow(folderCenter.X - appCenter.X, 2) + Math.Pow(folderCenter.Y - appCenter.Y, 2));

            bool shouldHover = distance < 90;
            if (shouldHover != _isFolderHovered)
            {
                _isFolderHovered = shouldHover;
            }

            Invalidate();
        }

        private void OnFormMouseUp(object? sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            Cursor = Cursors.Default;

            // Check if dropped near or on Applications Folder
            var folderCenter = new Point(_folderPos.X + FolderIconSize / 2, _folderPos.Y + FolderIconSize / 2);
            var appCenter = new Point(_appCurrentPos.X + AppIconSize / 2, _appCurrentPos.Y + AppIconSize / 2);
            double distance = Math.Sqrt(Math.Pow(folderCenter.X - appCenter.X, 2) + Math.Pow(folderCenter.Y - appCenter.Y, 2));

            if (distance < 100)
            {
                // Auto-detected drop: snap into folder center and start install
                _appCurrentPos = new Point(_folderPos.X + (FolderIconSize - AppIconSize) / 2, _folderPos.Y + (FolderIconSize - AppIconSize) / 2);
                _isFolderHovered = true;
                Invalidate();
                StartInstallationAsync();
            }
            else
            {
                // Snap back to initial position
                _appCurrentPos = _appInitialPos;
                _isFolderHovered = false;
                Invalidate();
            }
        }

        private async void StartInstallationAsync()
        {
            if (_isInstalling || _installFinished)
                return;

            _isInstalling = true;
            _statusText = InstallerLocalization.Current.FormatInstalling(_appName);
            Invalidate();

            await Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_targetInstallerExe) && File.Exists(_targetInstallerExe))
                    {
                        // Real NSIS or native installer execution
                        ProcessStartInfo psi;
                        if (_installerMetadata?.IsNsis == true)
                        {
                            // Standard NSIS silent install switch
                            psi = new ProcessStartInfo(_targetInstallerExe, "/S")
                            {
                                UseShellExecute = true,
                                Verb = "runas"
                            };
                        }
                        else
                        {
                            psi = new ProcessStartInfo(_targetInstallerExe)
                            {
                                UseShellExecute = true
                            };
                        }

                        var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            for (int i = 0; i < 60 && !proc.HasExited; i++)
                            {
                                _installProgress = Math.Min(0.92f, (float)(i + 1) / 50f);
                                Invoke(new Action(() => Invalidate()));
                                System.Threading.Thread.Sleep(150);
                            }
                            proc.WaitForExit();
                            _installProgress = 1.0f;
                        }
                    }
                    else
                    {
                        // Production installation of OpenDock itself
                        string targetDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Programs",
                            "OpenDock");

                        if (!Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                        var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                        int totalFiles = Math.Max(1, files.Length);

                        for (int i = 0; i < files.Length; i++)
                        {
                            string srcFile = files[i];
                            string relPath = Path.GetRelativePath(sourceDir, srcFile);
                            string destFile = Path.Combine(targetDir, relPath);

                            string? destSubDir = Path.GetDirectoryName(destFile);
                            if (!string.IsNullOrEmpty(destSubDir) && !Directory.Exists(destSubDir))
                            {
                                Directory.CreateDirectory(destSubDir);
                            }

                            try
                            {
                                File.Copy(srcFile, destFile, true);
                            }
                            catch { }

                            _installProgress = (float)(i + 1) / totalFiles;
                            Invoke(new Action(() => Invalidate()));
                            System.Threading.Thread.Sleep(6);
                        }

                        // Create Tag file
                        File.WriteAllText(Path.Combine(targetDir, "installed.tag"), DateTime.UtcNow.ToString("O"));

                        // Create Shortcuts
                        string targetExe = Path.Combine(targetDir, "OpenDock.exe");
                        string startMenuShortcut = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                            "OpenDock.lnk");
                        string desktopShortcut = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                            "OpenDock.lnk");

                        CreateShortcut(targetExe, startMenuShortcut, "macOS Dock for Windows");
                        CreateShortcut(targetExe, desktopShortcut, "macOS Dock for Windows");

                        _installProgress = 1.0f;
                    }
                }
                catch (Exception ex)
                {
                    _statusText = InstallerLocalization.Current.Error + ex.Message;
                }
            });

            _statusText = InstallerLocalization.Current.Complete;
            _installFinished = true;
            Invalidate();

            await Task.Delay(1100);

            // Launch installed app and close installer
            try
            {
                if (string.IsNullOrWhiteSpace(_targetInstallerExe))
                {
                    string targetExe = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Programs",
                        "OpenDock",
                        "OpenDock.exe");

                    if (File.Exists(targetExe))
                    {
                        Process.Start(new ProcessStartInfo(targetExe) { UseShellExecute = true });
                    }
                }
            }
            catch { }

            Close();
        }

        private static void CreateShortcut(string targetPath, string shortcutPath, string description)
        {
            try
            {
                var link = (IShellLinkW)new ShellLink();
                link.SetDescription(description);
                link.SetPath(targetPath);
                link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? "");
                var file = (IPersistFile)link;
                file.Save(shortcutPath, false);
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. macOS Window Outer Frame & Subtle Apple Satin Background
            Color bgTop = _isDarkMode ? Color.FromArgb(36, 38, 44) : Color.FromArgb(250, 251, 253);
            Color bgBottom = _isDarkMode ? Color.FromArgb(22, 23, 27) : Color.FromArgb(236, 238, 243);

            using (var brush = new LinearGradientBrush(ClientRectangle, bgTop, bgBottom, 90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Window Border Pen
            Color borderColor = _isDarkMode ? Color.FromArgb(55, 58, 68) : Color.FromArgb(205, 209, 217);
            using (var borderPen = new Pen(borderColor, 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, FormWidth - 1, FormHeight - 1);
            }

            // 2. macOS Finder Title Bar
            Color titleBarBg = _isDarkMode ? Color.FromArgb(30, 32, 38) : Color.FromArgb(242, 244, 247);
            using (var titleBrush = new SolidBrush(titleBarBg))
            {
                g.FillRectangle(titleBrush, 0, 0, FormWidth, 38);
            }

            Color titleSepColor = _isDarkMode ? Color.FromArgb(46, 49, 58) : Color.FromArgb(218, 221, 228);
            using (var linePen = new Pen(titleSepColor, 1f))
            {
                g.DrawLine(linePen, 0, 38, FormWidth, 38);
            }

            // 3. Traffic Light Buttons with authentic macOS glyph hover states
            DrawTrafficLights(g);

            // 4. Centered Title Bar Text
            using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(_isDarkMode ? Color.FromArgb(228, 232, 240) : Color.FromArgb(35, 38, 45)))
            {
                string title = _appName;
                SizeF titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, titleBrush, (FormWidth - titleSize.Width) / 2f, 10);
            }

            // 5. Subtitle / Instructional Status Text
            Color statusColor;
            if (_installFinished)
            {
                statusColor = Color.FromArgb(40, 195, 95);
            }
            else
            {
                statusColor = _isDarkMode ? Color.FromArgb(170, 178, 195) : Color.FromArgb(85, 92, 108);
            }

            using (var statusFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
            using (var statusBrush = new SolidBrush(statusColor))
            {
                SizeF statusSize = g.MeasureString(_statusText, statusFont);
                g.DrawString(_statusText, statusFont, statusBrush, (FormWidth - statusSize.Width) / 2f, 74);
            }

            // 6. Draw Connecting Arrow (➔)
            DrawConnectingArrow(g);

            // 7. Draw Applications Folder (Target, using User's macOS folder.png)
            DrawApplicationsFolder(g, _folderPos.X, _folderPos.Y, FolderIconSize, _isFolderHovered);

            // 8. Draw Draggable Application Bundle Icon (Stamped Logo)
            DrawAppIcon(g, _appCurrentPos.X, _appCurrentPos.Y, AppIconSize);

            // 9. macOS Progress Bar if Installing
            if (_isInstalling)
            {
                int barWidth = 340;
                int barHeight = 7;
                int barX = (FormWidth - barWidth) / 2;
                int barY = 352;

                // Track
                Color trackColor = _isDarkMode ? Color.FromArgb(45, 48, 58) : Color.FromArgb(215, 218, 226);
                using (var trackPath = GetRoundedRectPath(new Rectangle(barX, barY, barWidth, barHeight), 3))
                using (var trackBrush = new SolidBrush(trackColor))
                {
                    g.FillPath(trackBrush, trackPath);
                }

                // Fill
                int fillWidth = Math.Max(6, (int)(barWidth * Math.Clamp(_installProgress, 0f, 1f)));
                using (var fillPath = GetRoundedRectPath(new Rectangle(barX, barY, fillWidth, barHeight), 3))
                using (var fillBrush = new SolidBrush(Color.FromArgb(0, 122, 255)))
                {
                    g.FillPath(fillBrush, fillPath);
                }
            }
        }

        private void DrawTrafficLights(Graphics g)
        {
            // Red (Close)
            int rX = 18, rY = 13, dia = 12;
            using (var redBrush = new SolidBrush(_closeHovered ? Color.FromArgb(255, 95, 86) : Color.FromArgb(235, 82, 75)))
            {
                g.FillEllipse(redBrush, rX, rY, dia, dia);
            }
            using (var redBorder = new Pen(Color.FromArgb(210, 55, 50), 1f))
            {
                g.DrawEllipse(redBorder, rX, rY, dia, dia);
            }

            // Yellow (Minimize)
            int yX = 38, yY = 13;
            using (var yellowBrush = new SolidBrush(_minHovered ? Color.FromArgb(255, 189, 46) : Color.FromArgb(242, 178, 42)))
            {
                g.FillEllipse(yellowBrush, yX, yY, dia, dia);
            }
            using (var yellowBorder = new Pen(Color.FromArgb(215, 150, 30), 1f))
            {
                g.DrawEllipse(yellowBorder, yX, yY, dia, dia);
            }

            // Green (Zoom / Disabled)
            int gX = 58, gY = 13;
            using (var greenBrush = new SolidBrush(Color.FromArgb(39, 201, 63)))
            {
                g.FillEllipse(greenBrush, gX, gY, dia, dia);
            }
            using (var greenBorder = new Pen(Color.FromArgb(28, 168, 48), 1f))
            {
                g.DrawEllipse(greenBorder, gX, gY, dia, dia);
            }

            // Inner macOS glyphs when hovering traffic lights
            if (_trafficHovered)
            {
                using var glyphPen = new Pen(Color.FromArgb(170, 40, 10, 10), 1.2f);
                // 'x' for close
                g.DrawLine(glyphPen, rX + 3.5f, rY + 3.5f, rX + dia - 3.5f, rY + dia - 3.5f);
                g.DrawLine(glyphPen, rX + dia - 3.5f, rY + 3.5f, rX + 3.5f, rY + dia - 3.5f);

                // '-' for minimize
                using var minPen = new Pen(Color.FromArgb(170, 70, 45, 0), 1.3f);
                g.DrawLine(minPen, yX + 3f, yY + dia / 2f, yX + dia - 3f, yY + dia / 2f);
            }
        }

        private void DrawConnectingArrow(Graphics g)
        {
            int startX = 230;
            int endX = 398;
            int arrowY = 196;

            Color arrowColor = _isDarkMode ? Color.FromArgb(90, 110, 138) : Color.FromArgb(160, 172, 192);

            // Subtle drop shadow for arrow
            using (var shadowPen = new Pen(Color.FromArgb(25, 0, 0, 0), 2.5f))
            {
                shadowPen.DashStyle = DashStyle.Dash;
                shadowPen.DashPattern = new float[] { 5f, 4f };
                shadowPen.CustomEndCap = new AdjustableArrowCap(5f, 5.5f, true);
                g.DrawLine(shadowPen, startX, arrowY + 1.2f, endX, arrowY + 1.2f);
            }

            // Main dashed connecting arrow
            using (var pen = new Pen(arrowColor, 2.5f))
            {
                pen.DashStyle = DashStyle.Dash;
                pen.DashPattern = new float[] { 5f, 4f };
                pen.CustomEndCap = new AdjustableArrowCap(5f, 5.5f, true);
                g.DrawLine(pen, startX, arrowY, endX, arrowY);
            }
        }

        private void DrawAppIcon(Graphics g, int x, int y, int size)
        {
            // Drop Shadows - realistic macOS layered contact shadow
            int shadowOffsetY = _isDragging ? 12 : 6;
            int shadowAlpha = _isDragging ? 60 : 40;

            using (var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, x + 8, y + size - shadowOffsetY, size - 16, 16);
            }

            var rect = new Rectangle(x, y, size, size);

            if (_appIconImage != null)
            {
                g.DrawImage(_appIconImage, rect);
            }
            else
            {
                using var path = GetRoundedRectPath(rect, 24);
                using var bgBrush = new LinearGradientBrush(rect, Color.FromArgb(40, 145, 255), Color.FromArgb(10, 85, 210), 45f);
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1.5f);
                g.DrawPath(borderPen, path);
            }

            // Label Below Icon
            Color labelColor = _isDarkMode ? Color.FromArgb(235, 240, 250) : Color.FromArgb(25, 28, 35);
            using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(labelColor))
            {
                string name = _appName + ".app";
                SizeF textSize = g.MeasureString(name, font);
                g.DrawString(name, font, textBrush, x + (size - textSize.Width) / 2f, y + size + 10);
            }
        }

        private void DrawApplicationsFolder(Graphics g, int x, int y, int size, bool isHovered)
        {
            // Magnetic snap glow when hovering
            if (isHovered)
            {
                using (var outerGlow = new Pen(Color.FromArgb(60, 0, 122, 255), 6f))
                using (var innerGlow = new Pen(Color.FromArgb(200, 0, 122, 255), 2.5f))
                {
                    var glowRect = new Rectangle(x + 4, y + 10, size - 8, size - 14);
                    using var glowPath = GetRoundedRectPath(glowRect, 18);
                    g.DrawPath(outerGlow, glowPath);
                    g.DrawPath(innerGlow, glowPath);
                }
            }

            var folderRect = new Rectangle(x, y, size, size);

            if (_folderImage != null)
            {
                g.DrawImage(_folderImage, folderRect);
            }
            else
            {
                DrawFallbackVectorFolder(g, x + 6, y + 12, size - 12);
            }

            // Label Below Folder (Multi-Language Supported)
            Color labelColor = _isDarkMode ? Color.FromArgb(235, 240, 250) : Color.FromArgb(25, 28, 35);
            using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(labelColor))
            {
                string name = InstallerLocalization.Current.Applications;
                SizeF textSize = g.MeasureString(name, font);
                g.DrawString(name, font, textBrush, x + (size - textSize.Width) / 2f, y + size - 2);
            }
        }

        private void DrawFallbackVectorFolder(Graphics g, int x, int y, int size)
        {
            var backFlap = new Rectangle(x + 8, y + 10, 48, 22);
            using (var flapPath = GetRoundedRectPath(backFlap, 7))
            using (var flapBrush = new SolidBrush(Color.FromArgb(35, 130, 230)))
            {
                g.FillPath(flapBrush, flapPath);
            }

            var bodyRect = new Rectangle(x + 4, y + 22, size - 8, size - 26);
            using (var folderPath = GetRoundedRectPath(bodyRect, 16))
            using (var folderBrush = new LinearGradientBrush(bodyRect, Color.FromArgb(70, 175, 255), Color.FromArgb(20, 115, 225), 90f))
            {
                g.FillPath(folderBrush, folderPath);
                using var folderBorder = new Pen(Color.FromArgb(120, 255, 255, 255), 1.2f);
                g.DrawPath(folderBorder, folderPath);
            }
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _appIconImage?.Dispose();
                _folderImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
