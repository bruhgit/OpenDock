using System;
using System.Threading;
using System.Windows.Forms;

namespace OpenDock
{
    internal static class Program
    {
        private static Mutex? _mutex;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string procName = System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "").ToLowerInvariant();
            bool isInstallerExe = procName.Contains("install") || procName.Contains("setup");
            bool hasInstallArg = Array.Exists(args, a => a.Equals("--install", StringComparison.OrdinalIgnoreCase) ||
                                                         a.Equals("-i", StringComparison.OrdinalIgnoreCase) ||
                                                         a.Equals("/install", StringComparison.OrdinalIgnoreCase) ||
                                                         a.Equals("/setup", StringComparison.OrdinalIgnoreCase) ||
                                                         a.Equals("install", StringComparison.OrdinalIgnoreCase) ||
                                                         a.Equals("setup", StringComparison.OrdinalIgnoreCase));

            if (isInstallerExe || hasInstallArg)
            {
                string? targetExe = null;
                foreach (var arg in args)
                {
                    if (!arg.StartsWith("-") && !arg.StartsWith("/") && System.IO.File.Exists(arg))
                    {
                        targetExe = arg;
                        break;
                    }
                }

                ApplicationConfiguration.Initialize();
                Application.Run(new MacInstallerForm(targetExe));
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            bool isDevBuild = baseDir.Contains("bin\\Debug", StringComparison.OrdinalIgnoreCase) ||
                              baseDir.Contains("bin\\Release", StringComparison.OrdinalIgnoreCase);

            string installedDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "OpenDock");
            string tagFile = System.IO.Path.Combine(installedDir, "installed.tag");

            bool isInstalled = System.IO.File.Exists(tagFile) &&
                               baseDir.TrimEnd('\\').Equals(installedDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

            if (!isDevBuild && !isInstalled && !System.IO.File.Exists(System.IO.Path.Combine(baseDir, "installed.tag")))
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MacInstallerForm());
                return;
            }

            _mutex = new Mutex(true, "OpenDockSingleInstanceMutex", out bool createdNew);

            if (!createdNew)
            {
                // Başka bir OpenDock penceresi zaten açık, kapatıyoruz.
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());

            GC.KeepAlive(_mutex);
        }
    }
}