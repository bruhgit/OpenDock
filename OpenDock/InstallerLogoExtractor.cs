using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenDock
{
    public static class InstallerLogoExtractor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint PrivateExtractIcons(
            string lpszFile,
            int nIconIndex,
            int cxIcon,
            int cyIcon,
            IntPtr[] phicon,
            uint[] piconid,
            uint nIcons,
            uint flags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Bitmap? ExtractRawLogo(string exePath, int preferredSize = 256)
        {
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                return null;

            try
            {
                IntPtr[] phicon = new IntPtr[1];
                uint[] piconid = new uint[1];

                // Attempt to extract 256x256 high-resolution icon resource
                uint extracted = PrivateExtractIcons(exePath, 0, preferredSize, preferredSize, phicon, piconid, 1, 0);

                if (extracted > 0 && phicon[0] != IntPtr.Zero)
                {
                    using var icon = Icon.FromHandle(phicon[0]);
                    var bitmap = new Bitmap(icon.ToBitmap());
                    DestroyIcon(phicon[0]);
                    return bitmap;
                }

                // Fallback to standard 128x128
                extracted = PrivateExtractIcons(exePath, 0, 128, 128, phicon, piconid, 1, 0);
                if (extracted > 0 && phicon[0] != IntPtr.Zero)
                {
                    using var icon = Icon.FromHandle(phicon[0]);
                    var bitmap = new Bitmap(icon.ToBitmap());
                    DestroyIcon(phicon[0]);
                    return bitmap;
                }

                // Fallback to Shell associated icon
                using var assoc = Icon.ExtractAssociatedIcon(exePath);
                if (assoc != null)
                {
                    return new Bitmap(assoc.ToBitmap());
                }
            }
            catch
            {
                // Ignored
            }

            return null;
        }

        public static Bitmap CreateStampedMacSquircle(Image rawLogo, int targetSize = 112)
        {
            var output = new Bitmap(targetSize, targetSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(output);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int padding = 4;
            int boxSize = targetSize - (padding * 2);
            var rect = new Rectangle(padding, padding, boxSize, boxSize);

            int radius = (int)(boxSize * 0.22);
            using var squirclePath = GetRoundedRectPath(rect, radius);

            // Background base for squircle
            using (var baseBrush = new SolidBrush(Color.FromArgb(248, 249, 252)))
            {
                g.FillPath(baseBrush, squirclePath);
            }

            // Draw stamped logo centered and clipped to squircle
            var clipState = g.Save();
            g.SetClip(squirclePath, CombineMode.Intersect);

            // Compute aspect fit rectangle
            float scale = Math.Min((float)(boxSize - 12) / rawLogo.Width, (float)(boxSize - 12) / rawLogo.Height);
            int drawW = (int)(rawLogo.Width * scale);
            int drawH = (int)(rawLogo.Height * scale);
            int drawX = rect.X + (boxSize - drawW) / 2;
            int drawY = rect.Y + (boxSize - drawH) / 2;

            g.DrawImage(rawLogo, new Rectangle(drawX, drawY, drawW, drawH));

            // Subtle top highlight (macOS glass finish)
            var highlightRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 2);
            using (var grad = new LinearGradientBrush(highlightRect, Color.FromArgb(40, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
            {
                g.FillRectangle(grad, highlightRect);
            }

            g.Restore(clipState);

            // Outer boundary border
            using (var borderPen = new Pen(Color.FromArgb(50, 0, 0, 0), 1.2f))
            {
                g.DrawPath(borderPen, squirclePath);
            }

            return output;
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
    }
}
