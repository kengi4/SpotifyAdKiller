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
        private Process cachedSpotifyProcess = null;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOWMINNOACTIVE = 7;

        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
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
            try
            {
                notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                notifyIcon.Icon = SystemIcons.Information; // Fallback icon
            }
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

            try
            {
                if (cachedSpotifyProcess != null)
                {
                    if (cachedSpotifyProcess.HasExited)
                    {
                        cachedSpotifyProcess.Dispose();
                        cachedSpotifyProcess = null;
                    }
                }

                if (cachedSpotifyProcess == null)
                {
                    var spotifyProcesses = Process.GetProcessesByName("Spotify");
                    foreach (var p in spotifyProcesses)
                    {
                        try
                        {
                            if (cachedSpotifyProcess == null && p.MainWindowHandle != IntPtr.Zero)
                            {
                                cachedSpotifyProcess = p;
                            }
                            else
                            {
                                p.Dispose();
                            }
                        }
                        catch
                        {
                            p.Dispose();
                        }
                    }
                }

                if (cachedSpotifyProcess != null)
                {
                    cachedSpotifyProcess.Refresh();
                    if (cachedSpotifyProcess.MainWindowHandle != IntPtr.Zero)
                    {
                        string title = cachedSpotifyProcess.MainWindowTitle;
                        if (!string.IsNullOrEmpty(title) && title.IndexOf(targetTrackName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            await HandleAdDetection();
                        }
                    }
                }
            }
            catch { }
        }

        private async Task HandleAdDetection()
        {
            pollTimer.Stop(); // Stop polling while we restart
            try
            {
                if (cachedSpotifyProcess != null)
                {
                    cachedSpotifyProcess.Dispose();
                    cachedSpotifyProcess = null;
                }

                // Kill all Spotify processes
                var processes = Process.GetProcessesByName("Spotify");
                foreach (var p in processes)
                {
                    try { p.Kill(); } catch { }
                    p.Dispose();
                }

                await Task.Delay(1000); // Wait for processes to exit

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string spotifyExe = Path.Combine(appData, "Spotify", "Spotify.exe");
                
                if (File.Exists(spotifyExe))
                {
                    var psi = new ProcessStartInfo(spotifyExe) { WindowStyle = ProcessWindowStyle.Minimized };
                    Process.Start(psi);
                }
                else
                {
                    // Fallback to store app alias if possible
                    try { 
                        var psi = new ProcessStartInfo("spotify.exe") { WindowStyle = ProcessWindowStyle.Minimized };
                        Process.Start(psi); 
                    } catch { }
                }

                // Poll actively for up to 3 seconds to find the window as early as possible
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(100);
                    var newSpotifyProcesses = Process.GetProcessesByName("Spotify");
                    var newSpotify = newSpotifyProcesses.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                    
                    if (newSpotify != null)
                    {
                        ShowWindow(newSpotify.MainWindowHandle, SW_SHOWMINNOACTIVE);
                        foreach (var p in newSpotifyProcesses) p.Dispose();
                        break;
                    }
                    
                    foreach (var p in newSpotifyProcesses) p.Dispose();
                }

                // Give Spotify a bit more time to fully initialize before sending media keys
                await Task.Delay(2500);

                // Send Media Next Track
                keybd_event((byte)VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENDEDKEY | 0, UIntPtr.Zero);
                keybd_event((byte)VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
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
