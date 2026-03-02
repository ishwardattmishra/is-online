namespace IsOnline.Diagnostics;

/// <summary>
/// Event arguments published when a connectivity check URL fails.
/// Mirrors the Node.js diagnostics_channel behaviour of the original is-online package.
/// </summary>
public sealed class ConnectivityCheckFailedEventArgs : EventArgs
{
    /// <summary>UTC timestamp of the failure.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>The URL that was being checked.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Human-readable error message.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Optional exception that caused the failure.</summary>
    public Exception? Exception { get; init; }
}
