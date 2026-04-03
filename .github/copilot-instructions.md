# Copilot Instructions — copilot-tray-stats

WPF system-tray app (.NET 10) that polls GitHub's `copilot_internal/user` API and displays premium request quota in the taskbar.

## Build & Run

```powershell
dotnet build CopilotTrayStats.csproj
dotnet run --project CopilotTrayStats.csproj
```

**Prerequisite**: GitHub CLI must be installed and authenticated (`gh auth login`). The app shells out to `gh auth token` to obtain the bearer token.

## Architecture

```
App.xaml.cs (OnStartup)
  ├─ GitHubAuthService   → shells out to `gh auth token`
  ├─ CopilotApiService   → GET https://api.github.com/copilot_internal/user
  │    └─ CopilotUserResponse (Model/DTO, all fields nullable)
  ├─ SettingsService     → JSON in %AppData%\CopilotTrayStats\settings.json
  ├─ MainViewModel       → ObservableObject, DispatcherTimer refresh
  ├─ SettingsViewModel   → refresh interval + HKCU Run registry key
  ├─ MainWindow          → popup bound to MainViewModel
  └─ TaskbarIcon         → H.NotifyIcon.Wpf, icon color reflects UsageLevel
```

## Conventions

### MVVM
- Use `CommunityToolkit.Mvvm` source generators: `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`.
- All UI state lives in ViewModels. Code-behind is limited to event handlers that can't be expressed as commands (e.g. `DragMove`, `PositionNearTray`).
- `App.xaml.cs` owns service construction and wires ViewModels together — there is no DI container.

### WPF / Styling
- Global brushes and styles are defined in `App.xaml`. **Exception**: `UsageBar` style uses `{x:Static vm:UsageLevel.*}` and must stay in `MainWindow.Resources` (App.xaml is compiled before ViewModel types resolve).
- Custom `ComboBox`/`ComboBoxItem` templates use dark GitHub-inspired colors. Do not use `DisplayMemberPath` — use an explicit `<DataTemplate>` with  `ItemTemplate` so the selection box renders correctly.
- The window is `WindowStyle="None"` with `AllowsTransparency="True"`. Use `DragMove()` in `TitleBar_MouseLeftButtonDown` for dragging.
- `ShutdownMode="OnExplicitShutdown"` — the app stays alive when windows are hidden.

### Tray Icon
- `TaskbarIcon` is created in code, not XAML. **Always call `_trayIcon.ForceCreate()`** after setting it up or the icon won't appear in the shell.
- Icon color maps to `UsageLevel`: Good (green `#2ea043`), Warning (amber `#d29922`), Critical (red `#da3633`), Unknown/default (gray).
- The icon is rendered from an SVG path via `DrawingVisual` → `RenderTargetBitmap` → GDI `HICON`. Use `FillRule.EvenOdd` for the Copilot path.

### Settings Persistence
- `SettingsService` stores `AppSettings` as JSON. Path: `%AppData%\CopilotTrayStats\settings.json`.
- Run-on-startup is handled by writing to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

## Key Gotchas

- **API is internal**: `copilot_internal/user` is undocumented. The response schema is flattened — `login` is at the top level, quota is under `quota_snapshots.premium_interactions`.
- **All model fields are nullable**: always use `?.` and `?? fallback` when mapping API data in the ViewModel.
- **Debug easter egg**: clicking the Copilot icon in the title bar 5 consecutive times toggles `IsDebugVisible`, which shows the raw API JSON expander.
- **`DispatcherTimer` refresh**: executes on the UI thread; async/await keeps it non-blocking but be careful not to introduce synchronous work in `RefreshAsync`.

## Color Reference

| Key | Hex | Meaning |
|-----|-----|---------|
| `GoodBrush` | `#2ea043` | >50% remaining |
| `WarningBrush` | `#d29922` | 25–50% remaining |
| `CriticalBrush` | `#da3633` | <25% remaining |
| `UnknownBrush` | `#57606a` | Unlimited / no data |
| `CardBg` | `#1c2128` | Window background |
| `SubtleBg` | `#161b22` | Title bar, secondary panels |
| `TextPrimary` | `#e6edf3` | Main text |
| `TextSecondary` | `#8b949e` | Labels, hints |
| `BorderColor` | `#30363d` | Borders and dividers |
