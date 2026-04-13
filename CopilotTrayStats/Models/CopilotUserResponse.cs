using System.Text.Json.Serialization;

namespace CopilotTrayStats.Models;

public class CopilotUserResponse
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("copilot_plan")]
    public string? CopilotPlan { get; set; }

    [JsonPropertyName("access_type_sku")]
    public string? AccessTypeSku { get; set; }

    [JsonPropertyName("chat_enabled")]
    public bool? ChatEnabled { get; set; }

    [JsonPropertyName("is_mcp_enabled")]
    public bool? IsMcpEnabled { get; set; }

    [JsonPropertyName("quota_reset_date_utc")]
    public string? QuotaResetDateUtc { get; set; }

    [JsonPropertyName("quota_reset_date")]
    public string? QuotaResetDate { get; set; }

    [JsonPropertyName("quota_snapshots")]
    public QuotaSnapshots? QuotaSnapshots { get; set; }
}

public class QuotaSnapshots
{
    [JsonPropertyName("premium_interactions")]
    public QuotaEntry? PremiumInteractions { get; set; }

    [JsonPropertyName("chat")]
    public QuotaEntry? Chat { get; set; }

    [JsonPropertyName("completions")]
    public QuotaEntry? Completions { get; set; }
}

public class QuotaEntry
{
    [JsonPropertyName("entitlement")]
    public int? Entitlement { get; set; }

    [JsonPropertyName("remaining")]
    public int? Remaining { get; set; }

    [JsonPropertyName("quota_remaining")]
    public double? QuotaRemaining { get; set; }

    [JsonPropertyName("percent_remaining")]
    public double? PercentRemaining { get; set; }

    [JsonPropertyName("overage_count")]
    public int? OverageCount { get; set; }

    [JsonPropertyName("overage_permitted")]
    public bool? OveragePermitted { get; set; }

    [JsonPropertyName("unlimited")]
    public bool? Unlimited { get; set; }

    [JsonPropertyName("quota_id")]
    public string? QuotaId { get; set; }

    [JsonPropertyName("timestamp_utc")]
    public string? TimestampUtc { get; set; }
}
