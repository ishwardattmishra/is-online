using System.Net.Http;

namespace IsOnline.Checkers;

/// <summary>
/// Microsoft Network Connectivity Status Indicator (NCSI) check used by Windows.
/// Note: msftncsi.com is an older endpoint; modern Windows primarily uses
/// msftconnecttest.com. Both are included for broader compatibility.
/// </summary>
internal static class MicrosoftPortalChecker
{
    private static readonly (string url, string? expectedBody)[] Endpoints =
    [
        ("http://www.msftconnecttest.com/connecttest.txt", "Microsoft Connect Test"),
        ("http://www.msftncsi.com/ncsi.txt",               "Microsoft NCSI"),
        ("http://www.msftconnecttest.com/redirect",         null), // just expects a 200
    ];

    public static async Task<bool> CheckAsync(
        HttpClient httpClient,
        Action<string, Exception> onFailure,
        CancellationToken ct)
    {
        var tasks = Endpoints.Select(e => CheckEndpointAsync(httpClient, e.url, e.expectedBody, onFailure, ct));
        return await CheckerHelpers.AnySucceeds(tasks).ConfigureAwait(false);
    }

    private static async Task<bool> CheckEndpointAsync(
        HttpClient httpClient, string url, string? expectedBody,
        Action<string, Exception> onFailure, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return false;

            if (expectedBody is null) return true;

            var body = (await response.Content.ReadAsStringAsync().ConfigureAwait(false)).Trim();
            return body.Contains(expectedBody, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            onFailure(url, ex);
            return false;
        }
    }
}
