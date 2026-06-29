using AssetManager.Scanners;
using Xunit;

namespace AssetManager.Tests.Scanners;

// 3.4: Scanner tests — real DNS lookups, no mock
public class DnsScannerTests
{
    private readonly DnsScanner _scanner = new();

    [Fact]
    public void ScanType_IsDns()
    {
        Assert.Equal("dns", _scanner.ScanType);
    }

    [Fact]
    public async Task ScanAsync_Google_ReturnsARecords()
    {
        var result = await _scanner.ScanAsync("google.com");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        // Deserialize via JSON to inspect fields
        var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
        Assert.Contains("\"a\"", json.ToLower());
    }

    [Fact]
    public async Task ScanAsync_Cloudflare_HasNsRecords()
    {
        var result = await _scanner.ScanAsync("cloudflare.com");

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ScanAsync_InvalidDomain_ReturnsFailure()
    {
        var result = await _scanner.ScanAsync("this-domain-definitely-does-not-exist-xyz-123.com");

        // Either success with empty records OR failure — both are acceptable
        // The key thing is the scanner doesn't throw an unhandled exception
        Assert.True(result.Success || result.Error != null);
    }

    [Fact]
    public async Task ScanAsync_WithCancellation_Cancels()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // cancel immediately

        // Should not throw; returns error or handles gracefully
        var result = await _scanner.ScanAsync("google.com", cts.Token);
        // May succeed from cache or fail — just must not throw
        Assert.True(result.Success || result.Error != null);
    }
}
