# copilot-tray-stats

A Windows system-tray app that shows your GitHub Copilot premium request quota at a glance. It polls the GitHub Copilot API on a configurable interval and reflects your remaining requests through a color-coded tray icon.

## Features

- Tray icon changes color based on quota remaining (green / amber / red)
- Popup window with remaining requests, reset date, Chat and Completions status
- Configurable refresh interval and run-on-startup via built-in settings
- Tooltip shows a compact summary without opening the popup

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [GitHub CLI](https://cli.github.com/) installed and authenticated

```
gh auth login
```

## Build & Run

```powershell
dotnet build CopilotTrayStats.csproj
dotnet run --project CopilotTrayStats.csproj
```

## Usage

After launching, the app lives in the system tray. Click the icon to open the popup. Right-click for a context menu with Refresh and Exit options.

The settings window (gear icon in the popup title bar) lets you configure:

- **Run on startup** — registers the app in `HKCU\...\Run`
- **Refresh interval** — 1 minute to 1 hour

## Architecture

```
App.xaml.cs
  GitHubAuthService    shells out to `gh auth token`
  CopilotApiService    GET https://api.github.com/copilot_internal/user
  SettingsService      JSON in %AppData%\CopilotTrayStats\settings.json
  MainViewModel        observable state, DispatcherTimer refresh
  SettingsViewModel    refresh interval + Windows startup registry
  MainWindow           popup window
  TaskbarIcon          H.NotifyIcon.Wpf
```

No dependency injection container — `App.xaml.cs` constructs and wires all services.

## Stack

| Component | Library |
|-----------|---------|
| UI framework | WPF (.NET 10) |
| MVVM | CommunityToolkit.Mvvm 8.4.0 |
| Tray icon | H.NotifyIcon.Wpf 2.1.3 |
| Auth | GitHub CLI (`gh auth token`) |

## Notes

- The API endpoint (`copilot_internal/user`) is undocumented and may change without notice.
- The tray icon is rendered from the GitHub Copilot SVG path at runtime — no icon file is required.
- Clicking the Copilot icon in the popup title bar five times toggles a raw API response panel for debugging.
