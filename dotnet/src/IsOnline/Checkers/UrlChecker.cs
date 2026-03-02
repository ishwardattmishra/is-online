using System.Net.Http;

namespace IsOnline.Checkers;

/// <summary>
/// Checks individual HTTP/HTTPS URLs for connectivity.
/// Used for user-supplied <see cref="IsOnlineOptions.FallbackUrls"/>.
/// Uses HEAD first; falls back to GET on HTTP 405 Method Not Allowed.
/// </summary>
internal static class UrlChecker
{
    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https" };

    public static async Task<bool> CheckAsync(
        HttpClient httpClient,
        string url,
        Action<string, Exception> onFailure,
        CancellationToken ct)
    {
        // Validate URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var ex = new UriFormatException($"Invalid URL: {url}");
            onFailure(url, ex);
            return false;
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            var ex = new InvalidOperationException($"Unsupported protocol: {uri.Scheme}");
            onFailure(url, ex);
            return false;
        }

        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await httpClient.SendAsync(headRequest, ct).ConfigureAwait(false);

            if ((int)response.StatusCode == 405)
            {
                // Fall back to GET
                var getResponse = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
                return getResponse.IsSuccessStatusCode ||
                       ((int)getResponse.StatusCode >= 300 && (int)getResponse.StatusCode < 400);
            }

            return response.IsSuccessStatusCode ||
                   ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400);
        }
        catch (Exception ex)
        {
            onFailure(url, ex);
            return false;
        }
    }

    /// <summary>
    /// Checks a list of fallback URLs and returns true if any of them succeeds.
    /// </summary>
    public static async Task<bool> CheckAnyAsync(
        HttpClient httpClient,
        IReadOnlyList<string> urls,
        Action<string, Exception> onFailure,
        CancellationToken ct)
    {
        foreach (var url in urls)
        {
            ct.ThrowIfCancellationRequested();
            if (await CheckAsync(httpClient, url, onFailure, ct).ConfigureAwait(false))
                return true;
        }
        return false;
    }
}
