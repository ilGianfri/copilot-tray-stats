using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CopilotTrayStats.Services;

public record UpdateInfo(string Version, string ReleasePageUrl, string DownloadUrl);

public class UpdateService : IDisposable
{
    private const string GitHubRepo = "ilGianfri/copilot-tray-stats";
    private const string LatestReleaseUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private readonly GitHubAuthService _authService;

    public UpdateService(GitHubAuthService authService)
    {
        _authService = authService;
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("CopilotTrayStats", GetCurrentVersion()));
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    /// <summary>
    /// Returns an <see cref="UpdateInfo"/> if a newer version is available, or null if already up-to-date.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        string? token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(ct);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        string? tagName = root.GetProperty("tag_name").GetString();
        string? htmlUrl = root.GetProperty("html_url").GetString();

        if (tagName is null || htmlUrl is null)
            return null;

        string latestVersion = tagName.TrimStart('v');

        // Find the .exe asset in the release
        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out JsonElement assets))
        {
            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string? name = asset.GetProperty("name").GetString();
                if (name is not null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        }

        // In DEBUG there's no real version — always report available for testing
#if DEBUG
        return new UpdateInfo(latestVersion, htmlUrl, downloadUrl ?? htmlUrl);
#else
        string current = GetCurrentVersion();
        if (!IsNewer(latestVersion, current))
            return null;

        return new UpdateInfo(latestVersion, htmlUrl, downloadUrl ?? htmlUrl);
#endif
    }

    private static string GetCurrentVersion()
    {
        var v = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        return v is null ? "0.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
    }

    private static bool IsNewer(string latest, string current)
    {
        if (!Version.TryParse(latest, out Version? l)) return false;
        if (!Version.TryParse(current, out Version? c)) return false;
        return l > c;
    }

    public void Dispose() => _http.Dispose();
}
