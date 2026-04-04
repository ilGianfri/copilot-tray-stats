namespace CopilotTrayStats.Models;

public class DailyUsageEntry
{
    public DateOnly Date { get; set; }
    public int PremiumRemaining { get; set; }
    public int PremiumTotal { get; set; }
    /// <summary>Raw UTC string from the API, e.g. "2026-04-01T00:00:00Z". Used to filter since-last-reset.</summary>
    public string? QuotaResetDateUtc { get; set; }
}
