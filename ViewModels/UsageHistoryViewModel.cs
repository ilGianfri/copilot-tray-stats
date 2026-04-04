using CopilotTrayStats.Models;
using CopilotTrayStats.Services;

namespace CopilotTrayStats.ViewModels;

public class UsageHistoryViewModel
{
    private readonly UsageHistoryService _historyService;
    private const double MaxBarHeight = 100.0;

    public List<ChartBarViewModel> Bars { get; private set; } = [];
    public int TotalUsed { get; private set; }
    public int MaxUsed { get; private set; }
    public Action? CloseRequested { get; set; }

    public UsageHistoryViewModel(UsageHistoryService historyService)
    {
        _historyService = historyService;
    }

    public void Reload()
    {
        List<DailyUsageEntry> history = _historyService.Load();

        if (history.Count == 0)
        {
            Bars = [];
            TotalUsed = 0;
            return;
        }

        // Filter to entries in the current quota cycle.
        // QuotaResetDateUtc is the *next* reset date (end of current cycle),
        // so the cycle started approximately one month before that.
        DailyUsageEntry latest = history[^1];
        DateOnly cycleStart = DateOnly.MinValue;
        if (!string.IsNullOrEmpty(latest.QuotaResetDateUtc)
            && DateTime.TryParse(latest.QuotaResetDateUtc, out DateTime resetDt))
        {
            cycleStart = DateOnly.FromDateTime(resetDt.ToLocalTime().AddMonths(-1));
        }

        List<DailyUsageEntry> sinceReset = [.. history
            .Where(e => e.Date >= cycleStart)
            .OrderBy(e => e.Date)];

        // Compute per-day "requests used" as delta.
        // For the first entry: total - remaining (usage up to that point in the cycle).
        // For subsequent entries: previous.Remaining - current.Remaining.
        int[] usedPerDay = new int[sinceReset.Count];
        if (sinceReset.Count > 0)
        {
            DailyUsageEntry first = sinceReset[0];
            usedPerDay[0] = Math.Max(0, first.PremiumTotal - first.PremiumRemaining);

            for (int i = 1; i < sinceReset.Count; i++)
            {
                int delta = sinceReset[i - 1].PremiumRemaining - sinceReset[i].PremiumRemaining;
                usedPerDay[i] = Math.Max(0, delta);
            }
        }

        int maxUsed = usedPerDay.Length > 0 ? usedPerDay.Max() : 0;
        MaxUsed = maxUsed;
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        Bars = sinceReset
            .Select((entry, i) =>
            {
                int used = usedPerDay[i];
                double height = maxUsed > 0 ? (double)used / maxUsed * MaxBarHeight : 0;
                string dateLabel = entry.Date == today
                    ? "Today"
                    : entry.Date.ToString("MMM d");

                return new ChartBarViewModel
                {
                    Date      = entry.Date,
                    Used      = used,
                    BarHeight = height,
                    IsToday   = entry.Date == today,
                    Tooltip   = $"{dateLabel}: {used} request{(used == 1 ? "" : "s")} used"
                };
            })
            .ToList();

        TotalUsed = usedPerDay.Sum();
    }
}
