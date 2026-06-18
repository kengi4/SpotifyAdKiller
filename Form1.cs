using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpotifyAdKiller
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private ContextMenu contextMenu;
        private MenuItem menuItemEnable;
        private MenuItem menuItemExit;
        private Timer pollTimer;
        
        private string targetTrackName = "Слухайте музику без реклами";
        private bool isEnabled = true;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;

        public Form1()
        {
            InitializeComponent();
            InitializeApp();
        }

        protected override void SetVisibleCore(bool value)
        {
            // Always hide the main window
            base.SetVisibleCore(false);
        }

        private void InitializeApp()
        {
            try
            {
                string configTrack = ConfigurationManager.AppSettings["TargetTrackName"];
                if (!string.IsNullOrEmpty(configTrack))
                {
                    targetTrackName = configTrack;
                }
            }
            catch { }

            contextMenu = new ContextMenu();
            menuItemEnable = new MenuItem("Enabled", OnToggleEnable);
            menuItemEnable.Checked = true;
            menuItemExit = new MenuItem("Exit", OnExit);
            contextMenu.MenuItems.Add(menuItemEnable);
            contextMenu.MenuItems.Add(menuItemExit);

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Information; // Fallback icon
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Text = "Spotify Ad Killer";
            notifyIcon.Visible = true;

            pollTimer = new Timer();
            pollTimer.Interval = 2000; // 2 seconds
            pollTimer.Tick += PollTimer_Tick;
            pollTimer.Start();
        }

        private void OnToggleEnable(object sender, EventArgs e)
        {
            isEnabled = !isEnabled;
            menuItemEnable.Checked = isEnabled;
        }

        private void OnExit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private async void PollTimer_Tick(object sender, EventArgs e)
        {
            if (!isEnabled) return;

            var spotifyProcesses = Process.GetProcessesByName("Spotify");
            if (spotifyProcesses.Length == 0) return;

            foreach (var p in spotifyProcesses)
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    string title = p.MainWindowTitle;
                    
                    if (!string.IsNullOrEmpty(title) && title.IndexOf(targetTrackName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        await HandleAdDetection(spotifyProcesses);
                        break;
                    }
                }
            }
        }

        private async Task HandleAdDetection(Process[] processes)
        {
            pollTimer.Stop(); // Stop polling while we restart
            try
            {
                // Kill all Spotify processes
                foreach (var p in processes)
                {
                    try { p.Kill(); } catch { }
                }

                await Task.Delay(1000); // Wait for processes to exit

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string spotifyExe = Path.Combine(appData, "Spotify", "Spotify.exe");
                
                if (File.Exists(spotifyExe))
                {
                    Process.Start(spotifyExe);
                }
                else
                {
                    // Fallback to store app alias if possible
                    try { Process.Start("spotify.exe"); } catch { }
                }

                // Wait for Spotify to launch and initialize
                await Task.Delay(4000);

                // Send Media Play/Pause
                keybd_event((byte)VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY | 0, UIntPtr.Zero);
                keybd_event((byte)VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error restarting Spotify: " + ex.Message);
            }
            finally
            {
                pollTimer.Start();
            }
        }
    }
}
