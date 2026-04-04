using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotTrayStats.Services;
using Microsoft.Win32;

namespace CopilotTrayStats.ViewModels;

public record RefreshOption(int Minutes, string Label);

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private const string StartupKey = "CopilotTrayStats";
    private const string StartupRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        AppSettings s = settingsService.Load();
        _runOnStartup = GetStartupEnabled();
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

    public Action<int>? RefreshIntervalChanged { get; set; }
    public Action? CloseRequested { get; set; }

    [RelayCommand]
    private void Save()
    {
        SetStartupEnabled(RunOnStartup);
        _settingsService.Save(new AppSettings
        {
            RefreshIntervalMinutes = SelectedRefreshOption.Minutes,
            RunOnStartup = RunOnStartup,
        });
        RefreshIntervalChanged?.Invoke(SelectedRefreshOption.Minutes);
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
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath is not null)
                key.SetValue(StartupKey, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(StartupKey, false);
        }
    }
}
