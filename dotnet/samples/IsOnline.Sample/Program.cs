using IsOnline;
using IsOnline.Diagnostics;

// ─────────────────────────────────────────────────────────────────────
// IsOnline.NET — Sample Application
// Demonstrates all features of the IsOnline.NET library
// ─────────────────────────────────────────────────────────────────────

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔═══════════════════════════════════════════════════╗");
Console.WriteLine("║          IsOnline.NET — Sample Application       ║");
Console.WriteLine("╚═══════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ─── 1. Subscribe to diagnostics (optional) ─────────────────────────
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("📡 Subscribing to diagnostics events...");
Console.ResetColor();

IsOnlineChecker.CheckFailed += OnCheckFailed;

void OnCheckFailed(object? sender, ConnectivityCheckFailedEventArgs e)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($"   ⚠ [{e.Timestamp:HH:mm:ss.fff}] Check failed: {e.Url}");
    Console.WriteLine($"     └─ {e.ErrorMessage}");
    Console.ResetColor();
}

// ─── 2. Basic connectivity check ────────────────────────────────────
await RunDemo("Basic Check (defaults: timeout=5s, IPv4)",
    async () => await IsOnlineChecker.CheckAsync());

// ─── 3. Custom timeout ──────────────────────────────────────────────
await RunDemo("Custom Timeout (10 seconds)",
    async () => await IsOnlineChecker.CheckAsync(new IsOnlineOptions
    {
        Timeout = 10_000,
    }));

// ─── 4. IPv6 check ──────────────────────────────────────────────────
await RunDemo("IPv6 Check",
    async () => await IsOnlineChecker.CheckAsync(new IsOnlineOptions
    {
        IpVersion = 6,
    }));

// ─── 5. With fallback URLs ──────────────────────────────────────────
await RunDemo("With Fallback URLs",
    async () => await IsOnlineChecker.CheckAsync(new IsOnlineOptions
    {
        FallbackUrls = new[]
        {
            "https://www.google.com",
            "https://www.github.com",
            "https://www.microsoft.com",
        },
    }));

// ─── 6. Cancellation (abort after 500ms) ────────────────────────────
await RunDemo("Cancellation (abort after 500ms)",
    async () =>
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        return await IsOnlineChecker.CheckAsync(new IsOnlineOptions
        {
            Timeout = 10_000,
            CancellationToken = cts.Token,
        });
    });

// ─── 7. Very short timeout (likely fails → shows diagnostics) ───────
await RunDemo("Short Timeout (50ms — expect failures & diagnostics)",
    async () => await IsOnlineChecker.CheckAsync(new IsOnlineOptions
    {
        Timeout = 50,
    }));

// ─── 8. All options combined ────────────────────────────────────────
await RunDemo("All Options Combined",
    async () =>
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        return await IsOnlineChecker.CheckAsync(new IsOnlineOptions
        {
            Timeout           = 8_000,
            IpVersion         = 4,
            CancellationToken = cts.Token,
            FallbackUrls      = new[] { "https://example.com" },
        });
    });

// ─── 9. Invalid IpVersion (demonstrates error handling) ─────────────
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("─── Demo: Invalid IpVersion (error handling) ───");
Console.ResetColor();
try
{
    await IsOnlineChecker.CheckAsync(new IsOnlineOptions { IpVersion = 5 });
}
catch (ArgumentOutOfRangeException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"   ✗ Caught expected error: {ex.Message.Split('\n')[0]}");
    Console.ResetColor();
}

Console.WriteLine();

// ─── 10. Invalid Timeout (demonstrates error handling) ──────────────
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("─── Demo: Invalid Timeout (error handling) ───");
Console.ResetColor();
try
{
    await IsOnlineChecker.CheckAsync(new IsOnlineOptions { Timeout = 0 });
}
catch (ArgumentOutOfRangeException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"   ✗ Caught expected error: {ex.Message.Split('\n')[0]}");
    Console.ResetColor();
}

Console.WriteLine();

// ─── 11. Continuous monitoring loop ─────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("─── Demo: Continuous Monitoring (5 checks, 2s apart) ───");
Console.ResetColor();

for (int i = 1; i <= 5; i++)
{
    var online = await IsOnlineChecker.CheckAsync(new IsOnlineOptions { Timeout = 3000 });
    var symbol = online ? "●" : "○";
    var color  = online ? ConsoleColor.Green : ConsoleColor.Red;
    var status = online ? "ONLINE" : "OFFLINE";

    Console.ForegroundColor = color;
    Console.Write($"   {symbol} ");
    Console.ResetColor();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Check {i}/5 — {status}");

    if (i < 5)
        await Task.Delay(2000);
}

Console.WriteLine();

// ─── Cleanup ────────────────────────────────────────────────────────
IsOnlineChecker.CheckFailed -= OnCheckFailed;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔═══════════════════════════════════════════════════╗");
Console.WriteLine("║              All demos completed! ✓              ║");
Console.WriteLine("╚═══════════════════════════════════════════════════╝");
Console.ResetColor();

// ─── Helper ─────────────────────────────────────────────────────────
static async Task RunDemo(string title, Func<Task<bool>> check)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"─── Demo: {title} ───");
    Console.ResetColor();

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var result = await check();
    sw.Stop();

    if (result)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   ✓ Online ({sw.ElapsedMilliseconds}ms)");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"   ✗ Offline ({sw.ElapsedMilliseconds}ms)");
    }

    Console.ResetColor();
    Console.WriteLine();
}
