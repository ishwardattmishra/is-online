using System.Net;
using System.Net.Http;

namespace IsOnline.Checkers;

/// <summary>
/// Google connectivity check used by Android and ChromeOS.
/// Expects HTTP 204 No Content (no redirect).
/// </summary>
internal static class GooglePortalChecker
{
    private static readonly string[] Urls =
    [
        "http://connectivitycheck.gstatic.com/generate_204",
        "https://connectivitycheck.gstatic.com/generate_204",
        "http://clients3.google.com/generate_204",
        "https://clients3.google.com/generate_204",
        "http://www.gstatic.com/generate_204",
    ];

    public static async Task<bool> CheckAsync(
        HttpClient httpClient,
        Action<string, Exception> onFailure,
        CancellationToken ct)
    {
        var tasks = Urls.Select(url => CheckUrlAsync(httpClient, url, onFailure, ct));
        return await CheckerHelpers.AnySucceeds(tasks).ConfigureAwait(false);
    }

    private static async Task<bool> CheckUrlAsync(
        HttpClient httpClient, string url, Action<string, Exception> onFailure, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
            // Must be exactly 204 — a redirect would indicate a captive portal.
            return response.StatusCode == HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            onFailure(url, ex);
            return false;
        }
    }
}
