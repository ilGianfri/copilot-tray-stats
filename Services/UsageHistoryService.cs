using System.IO;
using System.Text.Json;
using CopilotTrayStats.Models;

namespace CopilotTrayStats.Services;

public class UsageHistoryService
{
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CopilotTrayStats", "history.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public List<DailyUsageEntry> Load()
    {
        try
        {
            if (File.Exists(HistoryPath))
            {
                string json = File.ReadAllText(HistoryPath);
                return JsonSerializer.Deserialize<List<DailyUsageEntry>>(json) ?? [];
            }
        }
        catch { }
        return [];
    }

    public void RecordEntry(DailyUsageEntry entry)
    {
        List<DailyUsageEntry> history = Load();

        int idx = history.FindIndex(e => e.Date == entry.Date);
        if (idx >= 0)
            history[idx] = entry;
        else
            history.Add(entry);

        // Keep sorted ascending by date
        history.Sort((a, b) => a.Date.CompareTo(b.Date));

        Save(history);
    }

    private static void Save(List<DailyUsageEntry> history)
    {
        try
        {
            string dir = Path.GetDirectoryName(HistoryPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(HistoryPath, JsonSerializer.Serialize(history, SerializerOptions));
        }
        catch { }
    }
}
