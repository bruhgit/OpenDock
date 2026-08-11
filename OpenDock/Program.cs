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
        static void Main()
        {
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