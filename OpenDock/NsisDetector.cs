using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OpenDock
{
    public sealed class NsisMetadata
    {
        public string FilePath { get; set; } = "";
        public string Title { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsNsis { get; set; }
        public long FileSize { get; set; }
    }

    public static class NsisDetector
    {
        private static readonly byte[] NsisSignature = Encoding.ASCII.GetBytes("NullsoftInst");

        public static bool IsNsisInstaller(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int searchLen = (int)Math.Min(fs.Length, 1024 * 1024); // Scan first 1MB
                byte[] buffer = new byte[searchLen];
                int bytesRead = fs.Read(buffer, 0, searchLen);

                return ContainsSequence(buffer, bytesRead, NsisSignature);
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDetect(string filePath, out NsisMetadata metadata)
        {
            metadata = new NsisMetadata { FilePath = filePath };

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                var fi = new FileInfo(filePath);
                metadata.FileSize = fi.Length;

                bool nsis = IsNsisInstaller(filePath);
                metadata.IsNsis = nsis;

                var vi = FileVersionInfo.GetVersionInfo(filePath);
                string title = !string.IsNullOrWhiteSpace(vi.ProductName)
                    ? vi.ProductName
                    : (!string.IsNullOrWhiteSpace(vi.FileDescription) ? vi.FileDescription : Path.GetFileNameWithoutExtension(filePath));

                // Clean common installer suffixes for display (e.g. "Spotify Setup" -> "Spotify")
                title = CleanInstallerTitle(title);

                metadata.Title = title;
                metadata.Publisher = vi.CompanyName ?? "";
                metadata.Version = vi.ProductVersion ?? vi.FileVersion ?? "";

                // Consider it an installer if signature matches or filename contains install/setup indicators
                string lower = Path.GetFileName(filePath).ToLowerInvariant();
                bool hasInstallerName = lower.Contains("setup") || lower.Contains("install") || lower.Contains("update");

                return nsis || hasInstallerName;
            }
            catch
            {
                return false;
            }
        }

        private static string CleanInstallerTitle(string raw)
        {
            string clean = raw.Trim();
            string[] suffixes = { " Installer", " Setup", " Yükleyici", " Kurulum", "-Setup", "_Setup" };
            foreach (var suffix in suffixes)
            {
                if (clean.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    clean = clean.Substring(0, clean.Length - suffix.Length).Trim();
                }
            }
            return clean;
        }

        private static bool ContainsSequence(byte[] source, int length, byte[] pattern)
        {
            if (pattern.Length == 0 || length < pattern.Length)
                return false;

            int maxIndex = length - pattern.Length;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (source[i] != pattern[0])
                    continue;

                bool match = true;
                for (int j = 1; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return true;
            }

            return false;
        }
    }
}
