using AssetManager.Scanners;
using Xunit;

namespace AssetManager.Tests.Scanners;

// 3.4: SSL scanner tests — real TLS handshake
public class SslScannerTests
{
    private readonly SslScanner _scanner = new();

    [Fact]
    public void ScanType_IsSsl()
    {
        Assert.Equal("ssl", _scanner.ScanType);
    }

    [Fact]
    public async Task ScanAsync_Google_ReturnsCertificateInfo()
    {
        var result = await _scanner.ScanAsync("google.com");

        Assert.True(result.Success, $"Scan failed: {result.Error}");
        Assert.NotNull(result.Data);

        var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
        // Should contain certificate fields
        Assert.Contains("certificate", json.ToLower());
        Assert.Contains("tls_version", json.ToLower());
    }

    [Fact]
    public async Task ScanAsync_Google_NotExpired()
    {
        var result = await _scanner.ScanAsync("google.com");

        Assert.True(result.Success);

        // Deserialize to check expiry
        var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
        // Should not say is_expired: true
        Assert.DoesNotContain("\"is_expired\":true", json);
    }

    [Fact]
    public async Task ScanAsync_InvalidHost_ReturnsFailure()
    {
        var result = await _scanner.ScanAsync("definitely-not-a-real-host-xyz-999.com");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
