using System.Net;
using System.Net.Http;

namespace IsOnline.Checkers;

/// <summary>
/// Checks internet connectivity by resolving the machine's public IP address
/// from icanhazip.com and ipify.org (same services used by the original JS package).
/// </summary>
internal static class PublicIpChecker
{
    private static readonly string[] IPv4Endpoints =
    [
        "https://icanhazip.com",
        "https://api.ipify.org",
    ];

    // icanhazip.com resolves to both A and AAAA records, so it works for IPv6 too.
    // api6.ipify.org is the explicit IPv6-only endpoint from ipify.
    private static readonly string[] IPv6Endpoints =
    [
        "https://icanhazip.com",
        "https://api6.ipify.org",
    ];

    /// <summary>
    /// Returns true when any public-IP endpoint replies with a valid IP address.
    /// </summary>
    public static async Task<bool> CheckAsync(
        HttpClient httpClient,
        int ipVersion,
        Action<string, Exception> onFailure,
        CancellationToken ct)
    {
        var endpoints = ipVersion == 6 ? IPv6Endpoints : IPv4Endpoints;
        var tasks = endpoints.Select(url => CheckEndpointAsync(httpClient, url, onFailure, ct));
        return await CheckerHelpers.AnySucceeds(tasks).ConfigureAwait(false);
    }

    private static async Task<bool> CheckEndpointAsync(
        HttpClient httpClient, string url, Action<string, Exception> onFailure, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var body = (await response.Content.ReadAsStringAsync().ConfigureAwait(false)).Trim();
            return IPAddress.TryParse(body, out _);
        }
        catch (Exception ex)
        {
            onFailure(url, ex);
            return false;
        }
    }
}
