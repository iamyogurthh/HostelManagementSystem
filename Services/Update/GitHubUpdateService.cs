using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HostelManagementSystem.Models;

namespace HostelManagementSystem.Services.Update;

public sealed class GitHubUpdateService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/iamyogurthh/HostelManagementSystem/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<GitHubRelease?> GetLatestStableReleaseAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.GetAsync(
                LatestReleaseUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(
                cancellationToken: cancellationToken);

            if (release is null || release.Draft || release.Prerelease)
            {
                return null;
            }

            return release;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public static bool IsNewerVersion(string? remoteTag, string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(remoteTag))
        {
            return false;
        }

        var remoteVersion = remoteTag.Trim().TrimStart('v', 'V');
        var localVersion = currentVersion.Trim().TrimStart('v', 'V');

        if (!Version.TryParse(remoteVersion, out var parsedRemote) ||
            !Version.TryParse(localVersion, out var parsedLocal))
        {
            return false;
        }

        return parsedRemote > parsedLocal;
    }

    public static void OpenReleasePage(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("HostelManagementSystem", AppVersionInfo.CurrentVersion));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        return client;
    }
}

public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }
}
