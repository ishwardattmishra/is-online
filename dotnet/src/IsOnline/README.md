# IsOnline.NET

> Check if the internet connection is up — a C#/.NET port of [sindresorhus/is-online](https://github.com/sindresorhus/is-online).

[![NuGet](https://img.shields.io/nuget/v/IsOnline.NET.svg)](https://www.nuget.org/packages/IsOnline.NET)

## Why?

`NetworkInterface.GetIsNetworkAvailable()` and `Ping` only tell you if there's a _local_ connection — not whether the internet is actually reachable. Behind a captive portal (hotel, airport Wi-Fi), your device has an IP address but can't reach anything until you log in. **IsOnline.NET** detects this correctly.

## Install

```sh
dotnet add package IsOnline.NET
```

**Targets:** `netstandard2.1` · `net8.0`

## Quick Start

```csharp
using IsOnline;

Console.WriteLine(await IsOnlineChecker.CheckAsync());
// => true
```

## Usage Examples

### With timeout

```csharp
bool online = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
{
    Timeout = 10_000, // 10 seconds
});
```

### With cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

bool online = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
{
    CancellationToken = cts.Token,
});
// => false (if cancelled before any check succeeds)
```

### With fallback URLs

```csharp
bool online = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
{
    FallbackUrls = ["https://www.google.com", "https://www.github.com"],
});
```

### With IPv6

```csharp
bool online = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
{
    IpVersion = 6,
});
```

### All options combined

```csharp
bool online = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
{
    Timeout           = 8_000,
    IpVersion         = 4,
    CancellationToken = cts.Token,
    FallbackUrls      = ["https://your-api.example.com/health"],
});
```

## API

### `IsOnlineChecker.CheckAsync(IsOnlineOptions? options = null)`

Returns `Task<bool>` — `true` if online, `false` otherwise.

- Never throws on network failure (always returns `false`).
- Throws `ArgumentOutOfRangeException` if `IpVersion` is not 4 or 6, or `Timeout` is ≤ 0.

### `IsOnlineOptions`

| Property            | Type                     | Default   | Description                                        |
| ------------------- | ------------------------ | --------- | -------------------------------------------------- |
| `Timeout`           | `int` (ms)               | `5000`    | Max wait time per check group                      |
| `IpVersion`         | `int`                    | `4`       | `4` or `6`                                         |
| `CancellationToken` | `CancellationToken`      | `default` | Cancels the check; resolves to `false`             |
| `FallbackUrls`      | `IReadOnlyList<string>?` | `null`    | Checked sequentially when all built-in checks fail |

### `IsOnlineChecker.CheckFailed` event

Subscribe to receive diagnostics when individual check URLs fail:

```csharp
IsOnlineChecker.CheckFailed += (_, e) =>
{
    Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] {e.Url} — {e.ErrorMessage}");
};
```

Each event provides:

- `Timestamp` (`DateTimeOffset`) — when the failure occurred
- `Url` (`string`) — the URL that failed
- `ErrorMessage` (`string`) — human-readable error
- `Exception` (`Exception?`) — the original exception

## How It Works

Four checker groups run **in parallel**. The **first success wins** — `CheckAsync` returns `true` immediately without waiting for the others.

### 🌐 Public IP Check

| URL                                                              | Success Condition                   |
| ---------------------------------------------------------------- | ----------------------------------- |
| `https://icanhazip.com`                                          | Response body is a valid IP address |
| `https://api.ipify.org` (IPv4) / `https://api6.ipify.org` (IPv6) | Response body is a valid IP address |

### 🍎 Apple Captive Portal (iOS / macOS / iPadOS)

| URL                                              | Success Condition         |
| ------------------------------------------------ | ------------------------- |
| `https://captive.apple.com/hotspot-detect.html`  | Body contains `"Success"` |
| `http://captive.apple.com/hotspot-detect.html`   | Body contains `"Success"` |
| `http://www.apple.com/library/test/success.html` | Body contains `"Success"` |

User-Agent: `CaptiveNetworkSupport/1.0 wispr`

If behind a captive portal, these URLs are redirected to a login page → body won't contain "Success" → correctly returns `false`.

### 🤖 Google Connectivity Check (Android / ChromeOS)

| URL                                                  | Success Condition   |
| ---------------------------------------------------- | ------------------- |
| `http://connectivitycheck.gstatic.com/generate_204`  | HTTP 204 No Content |
| `https://connectivitycheck.gstatic.com/generate_204` | HTTP 204 No Content |
| `http://clients3.google.com/generate_204`            | HTTP 204 No Content |
| `https://clients3.google.com/generate_204`           | HTTP 204 No Content |
| `http://www.gstatic.com/generate_204`                | HTTP 204 No Content |

A redirect or any other status code → captive portal suspected → `false`.

### 🪟 Microsoft NCSI (Windows)

| URL                                              | Success Condition                 |
| ------------------------------------------------ | --------------------------------- |
| `http://www.msftconnecttest.com/connecttest.txt` | Body = `"Microsoft Connect Test"` |
| `http://www.msftncsi.com/ncsi.txt`               | Body = `"Microsoft NCSI"`         |
| `http://www.msftconnecttest.com/redirect`        | HTTP 200 OK                       |

### Fallback URLs

If all four checker groups fail and `FallbackUrls` is provided, those are checked **sequentially**:

- HEAD request first (minimal data transfer)
- Falls back to GET on HTTP 405 Method Not Allowed
- Success = any 2xx or 3xx response

## Architecture

```
IsOnlineChecker.CheckAsync()
     │
     ├── 1. All NICs internal? → false
     ├── 2. CancellationToken cancelled? → false
     │
     └── 3. Run in parallel (first true wins):
           ├── PublicIpChecker       (2 URLs)
           ├── ApplePortalChecker    (3 URLs)
           ├── GooglePortalChecker   (5 URLs)
           └── MicrosoftPortalChecker(3 URLs)
                     │
                     └── All failed? → try FallbackUrls sequentially
                               │
                               └── All failed? → false
```

Key design decisions:

- **Shared static `HttpClient`** — avoids socket exhaustion
- **`AllowAutoRedirect = false`** — detects captive portals via redirect
- **`ConfigureAwait(false)`** — safe for ASP.NET, WPF, WinForms contexts
- **Diagnostics never throw** — `CheckFailed` event errors are swallowed

## Proxy Support

To use through a proxy, configure `HttpClient.DefaultProxy` before calling `CheckAsync`:

```csharp
HttpClient.DefaultProxy = new WebProxy("http://proxy.example.com:8080");
```

## Requirements

- .NET Standard 2.1+ or .NET 8.0+
- No external NuGet dependencies

## License

MIT — Original package by [Sindre Sorhus](https://sindresorhus.com). C# port by Ishwardatt Mishra.
