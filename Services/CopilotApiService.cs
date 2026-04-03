using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using CopilotTrayStats.Models;

namespace CopilotTrayStats.Services;

public class CopilotApiService
{
    private const string ApiUrl = "https://api.github.com/copilot_internal/user";

    private readonly GitHubAuthService _authService;
    private readonly HttpClient _httpClient;

    public CopilotApiService(GitHubAuthService authService)
    {
        _authService = authService;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotTrayStats/1.0");
    }

    public async Task<(CopilotUserResponse Data, string RawJson)> GetUserDataAsync()
    {
        string token = await _authService.GetTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await _httpClient.SendAsync(request);

        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GitHub API returned {(int)response.StatusCode} {response.ReasonPhrase}.\n\n{json}");
        }

        // Pretty-print for debug display
        string prettyJson = json;
        try
        {
            using var doc = JsonDocument.Parse(json);
            prettyJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { /* keep raw */ }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<CopilotUserResponse>(json, options);

        return (result ?? throw new InvalidOperationException("Empty response from GitHub API."), prettyJson);
    }
}
