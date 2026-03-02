namespace IsOnline;

/// <summary>Options for the <see cref="IsOnlineChecker.CheckAsync"/> method.</summary>
public sealed class IsOnlineOptions
{
    /// <summary>
    /// Milliseconds to wait for any single connectivity check to respond.
    /// Defaults to 5000 ms.
    /// </summary>
    public int Timeout { get; init; } = 5000;

    /// <summary>
    /// IP version to use: 4 (default) or 6.
    /// </summary>
    public int IpVersion { get; init; } = 4;

    /// <summary>
    /// Optional cancellation token. When cancelled the check resolves to <c>false</c>.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// Optional list of fallback HTTP/HTTPS URLs to check if all built-in checks fail.
    /// </summary>
    public IReadOnlyList<string>? FallbackUrls { get; init; }
}
