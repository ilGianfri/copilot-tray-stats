using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotTrayStats.Services;
using Microsoft.Win32;
using System.Diagnostics;

namespace CopilotTrayStats.ViewModels;

public record RefreshOption(int Minutes, string Label);

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly UpdateService _updateService;
    private const string StartupKey = "CopilotTrayStats";
    private const string StartupRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public SettingsViewModel(SettingsService settingsService, UpdateService updateService)
    {
        _settingsService = settingsService;
        _updateService = updateService;
        AppSettings s = settingsService.Load();
        _runOnStartup = GetStartupEnabled();
        _showUsedRequests = s.ShowUsedRequests;
        _selectedRefreshOption = RefreshOptions.Find(o => o.Minutes == s.RefreshIntervalMinutes)
            ?? RefreshOptions.Find(o => o.Minutes == 5)!;
    }

    public List<RefreshOption> RefreshOptions { get; } =
    [
        new(1,  "1 minute"),
        new(2,  "2 minutes"),
        new(5,  "5 minutes"),
        new(10, "10 minutes"),
        new(15, "15 minutes"),
        new(30, "30 minutes"),
        new(60, "1 hour"),
    ];

    [ObservableProperty]
    private bool _runOnStartup;

    [ObservableProperty]
    private RefreshOption _selectedRefreshOption;

    [ObservableProperty]
    private bool _showUsedRequests;

    public Action<int>? RefreshIntervalChanged { get; set; }
    public Action<bool>? ShowUsedRequestsChanged { get; set; }
    public Action? CloseRequested { get; set; }

    // ── Update checking ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateButtonLabel))]
    private string _updateStatus = "";

    [ObservableProperty]
    private bool _isCheckingUpdate;

    private string? _pendingDownloadUrl;
    private string? _pendingReleaseUrl;
    private bool _updateAvailable;

    public string UpdateButtonLabel => _updateAvailable ? "Download update" : "Check for updates";

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (_updateAvailable && (_pendingDownloadUrl ?? _pendingReleaseUrl) is string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            return;
        }

        IsCheckingUpdate = true;
        UpdateStatus = "Checking…";
        try
        {
            var info = await _updateService.CheckForUpdateAsync();
            if (info is null)
            {
                UpdateStatus = "You're up to date.";
            }
            else
            {
                _updateAvailable = true;
                _pendingDownloadUrl = info.DownloadUrl;
                _pendingReleaseUrl  = info.ReleasePageUrl;
                UpdateStatus = $"v{info.Version} is available!";
                OnPropertyChanged(nameof(UpdateButtonLabel));
            }
        }
        catch
        {
            UpdateStatus = "Could not check for updates.";
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    public static string AppVersion
    {
        get
        {
#if DEBUG
            return "DEV";
#else
            var v = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            return v is null ? "—" : $"{v.Major}.{v.Minor}.{v.Build}";
#endif
        }
    }

    [RelayCommand]
    private void Save()
    {
        SetStartupEnabled(RunOnStartup);
        _settingsService.Save(new AppSettings
        {
            RefreshIntervalMinutes = SelectedRefreshOption.Minutes,
            RunOnStartup = RunOnStartup,
            ShowUsedRequests = ShowUsedRequests,
        });
        RefreshIntervalChanged?.Invoke(SelectedRefreshOption.Minutes);
        ShowUsedRequestsChanged?.Invoke(ShowUsedRequests);
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    private static bool GetStartupEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupRegPath, false);
        return key?.GetValue(StartupKey) is not null;
    }

    private static void SetStartupEnabled(bool enabled)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupRegPath, true);
        if (key is null) return;
        if (enabled)
        {
            var exePath = Environment.ProcessPath
                ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath is not null)
                key.SetValue(StartupKey, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(StartupKey, false);
        }
    }
}
