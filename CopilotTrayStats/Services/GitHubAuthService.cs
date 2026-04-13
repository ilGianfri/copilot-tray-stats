using System.Diagnostics;

namespace CopilotTrayStats.Services;

public class GitHubAuthService
{
    private string? _cachedToken;

    public async Task<string> GetTokenAsync()
    {
        if (_cachedToken is not null)
            return _cachedToken;

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth token",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "GitHub CLI (gh) not found. Please install it from https://cli.github.com/ and run 'gh auth login'.", ex);
        }

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            string detail = string.IsNullOrWhiteSpace(error) ? "No token returned." : error.Trim();
            throw new InvalidOperationException(
                $"Failed to retrieve GitHub token via 'gh auth token'. {detail}\nRun 'gh auth login' to authenticate.");
        }

        _cachedToken = output.Trim();
        return _cachedToken;
    }

    public void InvalidateToken()
    {
        _cachedToken = null;
    }
}
