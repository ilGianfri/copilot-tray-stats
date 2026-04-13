using System.IO;
using System.Text.Json;

namespace CopilotTrayStats.Services;

public class AppSettings
{
    public int RefreshIntervalMinutes { get; set; } = 5;
    public bool RunOnStartup { get; set; } = false;
    public bool ShowUsedRequests { get; set; } = false;
}

public class CachedState
{
    public string Username { get; set; } = "";
    public string PlanType { get; set; } = "";
    public int PremiumRemaining { get; set; }
    public int PremiumTotal { get; set; }
    public int PremiumUsed { get; set; }
    public string ResetAt { get; set; } = "—";
    public bool IsUnlimited { get; set; }
    public int OverageCount { get; set; }
    public bool IsMcpEnabled { get; set; }
    public bool IsChatEnabled { get; set; }
    public string PercentRemainingLabel { get; set; } = "";
    public string ChatStatus { get; set; } = "—";
    public string CompletionsStatus { get; set; } = "—";
    public string RawJson { get; set; } = "";
    public string LastRefreshed { get; set; } = "";
    public DateTime LastRefreshTime { get; set; }
    public string? QuotaResetDateUtc { get; set; }
}

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CopilotTrayStats", "settings.json");

    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CopilotTrayStats", "state.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
    }

    public CachedState? LoadState()
    {
        try
        {
            if (File.Exists(StatePath))
            {
                var json = File.ReadAllText(StatePath);
                return JsonSerializer.Deserialize<CachedState>(json);
            }
        }
        catch { }
        return null;
    }

    public void SaveState(CachedState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            File.WriteAllText(StatePath, JsonSerializer.Serialize(state, SerializerOptions));
        }
        catch { }
    }
}
