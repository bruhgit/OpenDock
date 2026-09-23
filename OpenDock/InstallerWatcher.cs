using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace OpenDock
{
    public static class InstallerWatcher
    {
        private static Thread? _watcherThread;
        private static bool _running = false;
        private static readonly HashSet<int> KnownProcessIds = new();
        private static readonly HashSet<string> HandledPaths = new(StringComparer.OrdinalIgnoreCase);

        public static void Start()
        {
            if (_running) return;
            _running = true;

            // Seed currently running processes so we only detect new ones
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    KnownProcessIds.Add(p.Id);
                }
            }
            catch { }

            _watcherThread = new Thread(WatcherLoop)
            {
                IsBackground = true,
                Name = "OpenDockInstallerWatcher"
            };
            _watcherThread.Start();
        }

        public static void Stop()
        {
            _running = false;
        }

        private static void WatcherLoop()
        {
            while (_running)
            {
                try
                {
                    var current = Process.GetProcesses();
                    var currentIds = new HashSet<int>();

                    foreach (var proc in current)
                    {
                        int id = proc.Id;
                        currentIds.Add(id);

                        if (!KnownProcessIds.Contains(id))
                        {
                            KnownProcessIds.Add(id);
                            CheckNewProcess(proc);
                        }
                    }

                    // Prune terminated process ids
                    KnownProcessIds.IntersectWith(currentIds);
                }
                catch
                {
                    // Ignored in monitoring loop
                }

                Thread.Sleep(1000);
            }
        }

        private static void CheckNewProcess(Process proc)
        {
            try
            {
                string? exePath = null;
                try
                {
                    exePath = proc.MainModule?.FileName;
                }
                catch { }

                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                    return;

                string fileName = Path.GetFileName(exePath).ToLowerInvariant();

                // Exclude system tools and compilers
                if (fileName.Contains("dotnet") || fileName.Contains("devenv") || fileName.Contains("msbuild") || fileName.Contains("git"))
                    return;

                // Check for NSIS installer signature or setup naming
                if (NsisDetector.IsNsisInstaller(exePath) || fileName.EndsWith("setup.exe") || fileName.EndsWith("installer.exe"))
                {
                    lock (HandledPaths)
                    {
                        if (HandledPaths.Contains(exePath))
                            return;
                        HandledPaths.Add(exePath);
                    }

                    // Launch MacInstallerForm on UI thread
                    if (Application.OpenForms.Count > 0)
                    {
                        var mainForm = Application.OpenForms[0];
                        mainForm?.BeginInvoke(new Action(() =>
                        {
                            var installerForm = new MacInstallerForm(exePath);
                            installerForm.Show();
                            installerForm.BringToFront();
                        }));
                    }
                }
            }
            catch
            {
                // Access denied or process exited
            }
        }
    }
}
