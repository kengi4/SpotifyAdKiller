using System;
using System.Threading;
using System.Windows.Forms;

namespace SpotifyAdKiller
{
    static class Program
    {
        static Mutex mutex = new Mutex(true, "{SpotifyAdKiller-SingleInstance-Mutex}");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("Приложение уже запущено!", "Spotify Ad Killer", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
