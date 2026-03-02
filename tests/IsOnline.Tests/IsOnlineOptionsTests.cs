using IsOnline;
using IsOnline.Diagnostics;
using Xunit;

namespace IsOnline.Tests;

public class IsOnlineOptionsTests
{
    [Fact]
    public void Default_Timeout_Is_5000()
    {
        var options = new IsOnlineOptions();
        Assert.Equal(5000, options.Timeout);
    }

    [Fact]
    public void Default_IpVersion_Is_4()
    {
        var options = new IsOnlineOptions();
        Assert.Equal(4, options.IpVersion);
    }

    [Fact]
    public void Default_FallbackUrls_Is_Null()
    {
        var options = new IsOnlineOptions();
        Assert.Null(options.FallbackUrls);
    }

    [Fact]
    public void Default_CancellationToken_Is_Default()
    {
        var options = new IsOnlineOptions();
        Assert.Equal(default, options.CancellationToken);
    }

    [Fact]
    public void Can_Set_Timeout()
    {
        var options = new IsOnlineOptions { Timeout = 10_000 };
        Assert.Equal(10_000, options.Timeout);
    }

    [Fact]
    public void Can_Set_IpVersion_6()
    {
        var options = new IsOnlineOptions { IpVersion = 6 };
        Assert.Equal(6, options.IpVersion);
    }

    [Fact]
    public void Can_Set_FallbackUrls()
    {
        var urls = new[] { "https://example.com" };
        var options = new IsOnlineOptions { FallbackUrls = urls };
        Assert.Single(options.FallbackUrls!);
        Assert.Equal("https://example.com", options.FallbackUrls![0]);
    }
}

public class ConnectivityEventsTests
{
    [Fact]
    public void ConnectivityCheckFailedEventArgs_Defaults()
    {
        var args = new ConnectivityCheckFailedEventArgs();
        Assert.Equal(string.Empty, args.Url);
        Assert.Equal(string.Empty, args.ErrorMessage);
        Assert.Null(args.Exception);
    }

    [Fact]
    public void ConnectivityCheckFailedEventArgs_CanBePopulated()
    {
        var ex = new Exception("test");
        var args = new ConnectivityCheckFailedEventArgs
        {
            Timestamp = DateTimeOffset.UtcNow,
            Url = "https://example.com",
            ErrorMessage = "test",
            Exception = ex,
        };

        Assert.Equal("https://example.com", args.Url);
        Assert.Equal("test", args.ErrorMessage);
        Assert.Same(ex, args.Exception);
    }
}
