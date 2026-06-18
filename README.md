# Spotify Ad Killer

A lightweight, unobtrusive Windows desktop application designed to monitor your local Spotify client and automatically skip unwanted tracks or advertisements.

> **Note**: This application was generated completely from scratch with the help of **Gemini 3.1 Pro**.

## Get Started

📥 **[Download the latest release here](https://github.com/kengi4/SpotifyAdKiller/releases/latest)**

Simply download the `.exe` file, run it, and it will immediately start working in your system tray!

## How it works

The application runs silently in the system tray and polls the active Spotify processes with minimal CPU overhead. When it detects that the currently playing track matches your designated "target track", it automatically:
1. Closes the Spotify process.
2. Restarts Spotify.
3. Simulates a `Next Track` media key press to instantly resume your listening session, bypassing the unwanted track.

## Features

- **Lightweight & Fast**: Uses `System.Windows.Forms.Timer` and targets specific processes to prevent unnecessary CPU load. You can work in other applications without lag.
- **System Tray Integration**: Operates completely in the background. You can right-click the tray icon to easily Enable/Disable the checking logic or Exit the application entirely.
- **Single Instance**: Built-in Mutex ensures only one instance of the application runs at any given time.
- **Configurable**: Change the target track directly in the application configuration file without needing to recompile the source code.

## Configuration & Finding Your Ad Title

By default, the target track name is set to `Слухайте музику без реклами` (which is the default ad title for the Ukrainian region). However, since ad titles differ by language and region, you will likely need to change this value to match your region's ad title.

### How to find your advertisement title:
1. Open the Spotify desktop application.
2. Listen to music until an advertisement starts playing.
3. Hover your mouse over the Spotify icon on your Windows taskbar or look at the top window title bar of the Spotify application.
4. Note the **exact text** that is displayed there.

### How to update the config file:
1. Go to the folder where `SpotifyAdKiller.exe` is located.
2. Open the file `SpotifyAdKiller.exe.config` in any text editor (like Notepad).
3. Find the line with `TargetTrackName` and replace the `value` with the exact text you found in the previous step:

```xml
<appSettings>
    <add key="TargetTrackName" value="Your Exact Ad Title Here" />
</appSettings>
```

4. Save the file and restart the application from your system tray for the changes to take effect.

## Compilation & Build

To build the project from source, you need the .NET Framework 4.7.2 SDK.

Clone the repository 
```bash
git clone https://github.com/kengi4/SpotifyAdKiller.git
```

and run the following command in the project directory:

```bash
dotnet build SpotifyAdKiller.csproj -c Release
```

The compiled executable will be generated at `bin\Release\`.
