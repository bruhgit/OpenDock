using System;
using System.Threading;
using System.Windows.Forms;

namespace OpenDock
{
    internal static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main()
        {
            _mutex = new Mutex(true, "OpenDockSingleInstanceMutex", out bool createdNew);

            if (!createdNew)
            {
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());

            GC.KeepAlive(_mutex);
        }
    }
}