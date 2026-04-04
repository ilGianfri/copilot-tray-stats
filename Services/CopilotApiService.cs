using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using CopilotTrayStats.Models;

namespace CopilotTrayStats.Services;

public class CopilotApiService
{
    private const string ApiUrl = "https://api.github.com/copilot_internal/user";

    // Serialize options - avoids creating new options object on every call, since we only need it for pretty-printing debug info
    private static readonly JsonSerializerOptions s_prettyPrintOptions = new() { WriteIndented = true };
    // Deserialize options - ignore case to be resilient to any changes in GitHub's JSON property naming
    private static readonly JsonSerializerOptions s_deserializeOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly GitHubAuthService _authService;
    private readonly HttpClient _httpClient;

    public CopilotApiService(GitHubAuthService authService)
    {
        _authService = authService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotTrayStats/1.0");
    }

    public async Task<(CopilotUserResponse Data, string RawJson)> GetUserDataAsync(CancellationToken ct = default)
    {
        string token = await _authService.GetTokenAsync();

        HttpResponseMessage response = await SendRequestAsync(token, ct);

        // On 401 the cached token may be stale — invalidate and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            _authService.InvalidateToken();
            token = await _authService.GetTokenAsync();
            response = await SendRequestAsync(token, ct);
        }

        string json = await response.Content.ReadAsStringAsync(ct);
        response.Dispose();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GitHub API returned {(int)response.StatusCode} {response.ReasonPhrase}.\n\n{json}");
        }

        // Pretty-print for debug display
        string prettyJson = json;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            prettyJson = JsonSerializer.Serialize(doc, s_prettyPrintOptions);
        }
        catch { /* keep raw */ }

        CopilotUserResponse? result = JsonSerializer.Deserialize<CopilotUserResponse>(json, s_deserializeOptions);

        return (result ?? throw new InvalidOperationException("Empty response from GitHub API."), prettyJson);
    }

    private Task<HttpResponseMessage> SendRequestAsync(string token, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _httpClient.SendAsync(request, ct);
    }
}
