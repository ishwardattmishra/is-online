using System.Net.Http;

namespace IsOnline.Checkers;

/// <summary>
/// Apple captive-portal check used by iOS, macOS, and iPadOS.
/// Expects a small HTML page whose body contains the word "Success".
/// When behind a captive portal, these URLs are redirected to a login page
/// (AllowAutoRedirect is false on the shared HttpClient, so the redirect is
/// surfaced as a non-success status / different body — correctly yielding false).
/// </summary>
internal static class ApplePortalChecker
{
    private static readonly string[] Urls =
    [
        "https://captive.apple.com/hotspot-detect.html",
        "http://captive.apple.com/hotspot-detect.html",
        "http://www.apple.com/library/test/success.html",
    ];

    private const string UserAgent = "CaptiveNetworkSupport/1.0 wispr";

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
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return body.Contains("Success", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            onFailure(url, ex);
            return false;
        }
    }
}
