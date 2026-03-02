using System.Net;
using System.Net.NetworkInformation;
using System.Net.Http;
using IsOnline.Checkers;
using IsOnline.Diagnostics;

namespace IsOnline;

/// <summary>
/// Checks whether the device has an active internet connection.
/// Runs Apple, Google, and Microsoft captive-portal tests plus public-IP checks in parallel.
/// The first check that succeeds causes <see cref="CheckAsync"/> to return <c>true</c>.
/// </summary>
public static class IsOnlineChecker
{
    // Shared HttpClient — configured to NOT auto-follow redirects so we can
    // detect captive portals by the redirect response itself.
    // Uses default TLS validation (no DangerousAcceptAnyServerCertificateValidator).
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
    });

    /// <summary>
    /// Raised whenever a connectivity check URL fails.
    /// Subscribe to this for diagnostic/logging purposes.
    /// </summary>
    public static event EventHandler<ConnectivityCheckFailedEventArgs>? CheckFailed;

    /// <summary>
    /// Returns <c>true</c> if the device has internet access, <c>false</c> otherwise.
    /// </summary>
    /// <param name="options">Optional configuration. Uses defaults when <c>null</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="IsOnlineOptions.IpVersion"/> is not 4 or 6,
    /// or when <see cref="IsOnlineOptions.Timeout"/> is &lt;= 0.
    /// </exception>
    public static async Task<bool> CheckAsync(IsOnlineOptions? options = null)
    {
        options ??= new IsOnlineOptions();

        // ── Validation ──────────────────────────────────────────────────
        if (options.IpVersion is not (4 or 6))
            throw new ArgumentOutOfRangeException(nameof(options), "IpVersion must be 4 or 6.");

        if (options.Timeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be greater than 0.");

        // Fast-path: if every NIC is loopback/link-local we cannot be online.
        if (IsAllInternal())
            return false;

        if (options.CancellationToken.IsCancellationRequested)
            return false;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);
        cts.CancelAfter(options.Timeout);
        var ct = cts.Token;

        void PublishFailure(string url, Exception ex) => RaiseCheckFailed(url, ex);

        // Launch all four checker groups in parallel.
        var tasks = new List<Task<bool>>
        {
            PublicIpChecker.CheckAsync(Http, options.IpVersion, PublishFailure, ct),
            ApplePortalChecker.CheckAsync(Http, PublishFailure, ct),
            GooglePortalChecker.CheckAsync(Http, PublishFailure, ct),
            MicrosoftPortalChecker.CheckAsync(Http, PublishFailure, ct),
        };

        try
        {
            var result = await CheckerHelpers.AnySucceeds(tasks).ConfigureAwait(false);
            if (result) return true;
        }
        catch (OperationCanceledException)
        {
            // Timed out or cancelled — try fallbacks with a fresh timeout.
        }

        // Fallback URLs (if provided)
        if (options.FallbackUrls?.Count > 0)
        {
            using var fallbackCts = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);
            fallbackCts.CancelAfter(options.Timeout);
            try
            {
                return await UrlChecker.CheckAnyAsync(Http, options.FallbackUrls, PublishFailure, fallbackCts.Token)
                                       .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when all network interfaces are internal
    /// (loopback or IPv6 link-local), meaning no real connectivity is possible.
    /// </summary>
    private static bool IsAllInternal()
    {
        try
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .All(addr =>
                    IPAddress.IsLoopback(addr.Address) || addr.Address.IsIPv6LinkLocal);
        }
        catch
        {
            // If we can't enumerate interfaces, don't block the check.
            return false;
        }
    }

    private static void RaiseCheckFailed(string url, Exception ex)
    {
        try
        {
            CheckFailed?.Invoke(null, new ConnectivityCheckFailedEventArgs
            {
                Timestamp    = DateTimeOffset.UtcNow,
                Url          = url,
                ErrorMessage = ex.Message,
                Exception    = ex,
            });
        }
        catch
        {
            // Diagnostics must never affect main functionality.
        }
    }
}
