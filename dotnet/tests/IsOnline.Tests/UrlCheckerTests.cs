using IsOnline.Checkers;
using System.Net;
using System.Net.Http;
using Xunit;

namespace IsOnline.Tests;

/// <summary>
/// Tests for UrlChecker using a real HttpClient against localhost (no mocking needed
/// for scheme-validation tests since those fail before any network call).
/// </summary>
public class UrlCheckerTests
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false });

    private static readonly List<(string url, Exception? ex)> Failures = [];
    private static void Capture(string url, Exception ex) => Failures.Add((url, ex));

    [Fact]
    public async Task InvalidUrl_ReturnsFalse()
    {
        var result = await UrlChecker.CheckAsync(Http, "not a url!!", Capture, default);
        Assert.False(result);
    }

    [Fact]
    public async Task UnsupportedScheme_ReturnsFalse()
    {
        var result = await UrlChecker.CheckAsync(Http, "ftp://example.com", Capture, default);
        Assert.False(result);
    }

    [Fact]
    public async Task CheckAnyAsync_EmptyList_ReturnsFalse()
    {
        var result = await UrlChecker.CheckAnyAsync(Http, Array.Empty<string>(), Capture, default);
        Assert.False(result);
    }
}
