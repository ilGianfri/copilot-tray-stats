using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotTrayStats.Models;
using CopilotTrayStats.Services;

namespace CopilotTrayStats.ViewModels;

public enum UsageLevel { Good, Warning, Critical, Unknown }

public partial class MainViewModel : ObservableObject
{
    private readonly CopilotApiService _apiService;
    private CancellationTokenSource? _timerCts;
    private CancellationTokenSource? _refreshCts;
    private int _refreshIntervalMinutes = 5;
    private DateTime? _lastRefreshTime;

    public Action<CachedState>? OnSuccessfulRefresh { get; set; }

    public MainViewModel(CopilotApiService apiService)
    {
        _apiService = apiService;
        StartRefreshLoop();
    }

    private void StartRefreshLoop()
    {
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = new CancellationTokenSource();
        _ = RunRefreshLoopAsync(_timerCts.Token);
    }

    private async Task RunRefreshLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_refreshIntervalMinutes));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                _ = Application.Current.Dispatcher.InvokeAsync(RefreshAsync);
        }
        catch (OperationCanceledException) { }
    }

    // ── Observable Properties ────────────────────────────────────────────────

    [ObservableProperty]
    private string _username = "—";

    [ObservableProperty]
    private string _planType = "—";

    [ObservableProperty]
    private int _premiumRemaining;

    [ObservableProperty]
    private int _premiumTotal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsageLevel))]
    [NotifyPropertyChangedFor(nameof(UsagePercent))]
    private int _premiumUsed;

    [ObservableProperty]
    private string _resetAt = "—";

    [ObservableProperty]
    private string _lastRefreshed = "Never";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isUnlimited;

    [ObservableProperty]
    private string _tooltipText = "Copilot Stats — loading…";

    [ObservableProperty]
    private string _rawJson = "(no data yet)";

    [ObservableProperty]
    private bool _isDebugVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOverage))]
    private int _overageCount;

    [ObservableProperty]
    private bool _isMcpEnabled;

    [ObservableProperty]
    private bool _isChatEnabled;

    [ObservableProperty]
    private string _percentRemainingLabel = "";

    [ObservableProperty]
    private string _chatStatus = "\u2014";

    [ObservableProperty]
    private string _completionsStatus = "\u2014";

    // ── Computed Properties ──────────────────────────────────────────────────

    public double UsagePercent =>
        PremiumTotal > 0 ? (double)PremiumUsed / PremiumTotal * 100.0 : 0;

    public bool HasOverage => OverageCount > 0;

    public UsageLevel UsageLevel
    {
        get
        {
            if (IsUnlimited) return UsageLevel.Good;
            if (PremiumTotal <= 0) return UsageLevel.Unknown;
            double remaining = (double)PremiumRemaining / PremiumTotal * 100.0;
            return remaining switch
            {
                > 50 => UsageLevel.Good,
                > 25 => UsageLevel.Warning,
                _ => UsageLevel.Critical
            };
        }
    }

    public bool IsStale(int minutes) =>
        _lastRefreshTime is null || (DateTime.Now - _lastRefreshTime.Value).TotalMinutes >= minutes;

    public void ApplyCachedState(CachedState state)
    {
        Username             = state.Username;
        PlanType             = state.PlanType;
        PremiumRemaining     = state.PremiumRemaining;
        PremiumTotal         = state.PremiumTotal;
        PremiumUsed          = state.PremiumUsed;
        ResetAt              = state.ResetAt;
        IsUnlimited          = state.IsUnlimited;
        OverageCount         = state.OverageCount;
        IsMcpEnabled         = state.IsMcpEnabled;
        IsChatEnabled        = state.IsChatEnabled;
        PercentRemainingLabel = state.PercentRemainingLabel;
        ChatStatus           = state.ChatStatus;
        CompletionsStatus    = state.CompletionsStatus;
        RawJson              = state.RawJson;
        LastRefreshed        = state.LastRefreshed;
        _lastRefreshTime     = state.LastRefreshTime;
        OnPropertyChanged(nameof(UsageLevel));
        OnPropertyChanged(nameof(UsagePercent));
        UpdateTooltip();
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        var ct = _refreshCts.Token;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            (CopilotUserResponse? data, string? raw) = await _apiService.GetUserDataAsync(ct);
            RawJson = raw;

            Username = data.Login ?? "unknown";
            PlanType = FormatPlan(data.CopilotPlan);

            QuotaEntry? premium = data.QuotaSnapshots?.PremiumInteractions;
            IsUnlimited = premium?.Unlimited ?? false;

            PremiumTotal = premium?.Entitlement ?? 0;
            PremiumRemaining = premium?.Remaining ?? 0;
            PremiumUsed = Math.Max(0, PremiumTotal - PremiumRemaining);

            string? resetSource = data.QuotaResetDateUtc ?? data.QuotaResetDate;
            ResetAt = FormatResetDate(resetSource);

            OverageCount = premium?.OverageCount ?? 0;
            PercentRemainingLabel = premium?.PercentRemaining is double pct ? $"({pct:F1}%)" : "";

            QuotaEntry? chat = data.QuotaSnapshots?.Chat;
            ChatStatus = chat?.Unlimited == true ? "\u221e" : (chat?.Remaining?.ToString() ?? "\u2014");

            QuotaEntry? completions = data.QuotaSnapshots?.Completions;
            CompletionsStatus = completions?.Unlimited == true ? "\u221e" : (completions?.Remaining?.ToString() ?? "\u2014");

            IsMcpEnabled = data.IsMcpEnabled ?? false;
            IsChatEnabled = data.ChatEnabled ?? false;

            LastRefreshed = DateTime.Now.ToString("HH:mm:ss");
            _lastRefreshTime = DateTime.Now;

            UpdateTooltip();
            OnPropertyChanged(nameof(UsageLevel));
            OnPropertyChanged(nameof(UsagePercent));

            OnSuccessfulRefresh?.Invoke(new CachedState
            {
                Username             = Username,
                PlanType             = PlanType,
                PremiumRemaining     = PremiumRemaining,
                PremiumTotal         = PremiumTotal,
                PremiumUsed          = PremiumUsed,
                ResetAt              = ResetAt,
                IsUnlimited          = IsUnlimited,
                OverageCount         = OverageCount,
                IsMcpEnabled         = IsMcpEnabled,
                IsChatEnabled        = IsChatEnabled,
                PercentRemainingLabel = PercentRemainingLabel,
                ChatStatus           = ChatStatus,
                CompletionsStatus    = CompletionsStatus,
                RawJson              = RawJson,
                LastRefreshed        = LastRefreshed,
                LastRefreshTime      = _lastRefreshTime.Value,
                QuotaResetDateUtc    = data.QuotaResetDateUtc ?? data.QuotaResetDate,
            });
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer refresh — leave UI state as-is
            return;
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
            {
                ErrorMessage = ex.Message;
                RawJson = ex.ToString();
                TooltipText = "Copilot Stats — error";
            }
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                IsLoading = false;
        }
    }

    public void SetRefreshInterval(int minutes)
    {
        _refreshIntervalMinutes = minutes;
        StartRefreshLoop();
    }

    [RelayCommand]
    private void CopyJson()
    {
        System.Windows.Clipboard.SetText(RawJson);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void UpdateTooltip()
    {
        if (IsUnlimited)
        {
            TooltipText = $"Copilot ({Username}): Unlimited requests";
            return;
        }

        TooltipText = PremiumTotal > 0
            ? $"Copilot ({Username}): {PremiumRemaining}/{PremiumTotal} premium requests left — resets {ResetAt}"
            : $"Copilot ({Username}): No premium quota info available";
    }

    private static string FormatPlan(string? raw) =>
        raw switch
        {
            "free" => "Free",
            "pro" or "individual_pro" => "Pro",
            "pro_plus" or "pro+" or "individual_pro_plus" => "Pro+",
            "business" => "Business",
            "enterprise" => "Enterprise",
            null or "" => "Unknown",
            _ => raw
        };

    private static string FormatResetDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return "—";
        if (DateTimeOffset.TryParse(iso, out DateTimeOffset dto))
        {
            DateTime local = dto.LocalDateTime;
            TimeSpan diff = local - DateTime.Now;
            var dateFormat = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            // Remove year from date format
            dateFormat = dateFormat.Replace("yyyy", "").Replace("yy", "").Trim().TrimEnd('/').TrimEnd('.').Trim();
            if (diff.TotalDays >= 1)
                return $"{local.ToString(dateFormat)} ({(int)diff.TotalDays}d)";
            if (diff.TotalHours >= 1)
                return $"{local:HH:mm} ({(int)diff.TotalHours}h)";
            return local.ToString("HH:mm");
        }
        return iso;
    }
}
