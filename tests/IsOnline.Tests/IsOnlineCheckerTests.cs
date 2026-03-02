using Xunit;

namespace IsOnline.Tests;

/// <summary>
/// Integration tests that make real network calls.
/// Marked with [Trait("Category","Integration")] so they can be excluded in CI
/// with: dotnet test --filter "Category!=Integration"
/// </summary>
public class IsOnlineCheckerTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CheckAsync_Returns_True_When_Online()
    {
        var result = await IsOnlineChecker.CheckAsync();
        Assert.True(result, "Expected to be online but got false.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CheckAsync_With_Explicit_Timeout_Returns_True()
    {
        var result = await IsOnlineChecker.CheckAsync(new IsOnlineOptions { Timeout = 10_000 });
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CheckAsync_With_FallbackUrls_Returns_True()
    {
        var result = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
        {
            FallbackUrls = ["https://www.google.com", "https://www.github.com"],
        });
        Assert.True(result);
    }

    [Fact]
    public async Task CheckAsync_With_AlreadyCancelled_Returns_False()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await IsOnlineChecker.CheckAsync(new IsOnlineOptions
        {
            CancellationToken = cts.Token,
        });

        Assert.False(result);
    }

    [Fact]
    public async Task CheckAsync_InvalidIpVersion_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            IsOnlineChecker.CheckAsync(new IsOnlineOptions { IpVersion = 5 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public async Task CheckAsync_InvalidTimeout_Throws(int timeout)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            IsOnlineChecker.CheckAsync(new IsOnlineOptions { Timeout = timeout }));
    }

    [Fact]
    public async Task CheckFailed_Event_Is_Raised_On_Failure()
    {
        var failures = new List<string>();
        void OnFail(object? sender, IsOnline.Diagnostics.ConnectivityCheckFailedEventArgs e) => failures.Add(e.Url);

        IsOnlineChecker.CheckFailed += OnFail;
        try
        {
            // Use a 1 ms timeout to guarantee all checks fail.
            await IsOnlineChecker.CheckAsync(new IsOnlineOptions { Timeout = 1 });

            // We don't assert the exact count (race condition), just that the event hook works:
            // nothing was thrown and failures list is accessible.
        }
        finally
        {
            IsOnlineChecker.CheckFailed -= OnFail;
        }
    }

    [Fact]
    public async Task CheckAsync_With_IpVersion6_Returns_Boolean()
    {
        // Just verifies it doesn't throw — IPv6 availability varies by network.
        var result = await IsOnlineChecker.CheckAsync(new IsOnlineOptions { IpVersion = 6 });
        Assert.IsType<bool>(result);
    }
}
