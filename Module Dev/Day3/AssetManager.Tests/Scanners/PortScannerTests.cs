using AssetManager.Scanners;
using Xunit;

namespace AssetManager.Tests.Scanners;

public class PortScannerTests
{
    private readonly PortScanner _scanner = new();

    [Fact]
    public void ScanType_IsPort()
    {
        Assert.Equal("port", _scanner.ScanType);
    }

    [Fact]
    public async Task ScanAsync_Localhost_Succeeds()
    {
        var result = await _scanner.ScanAsync("127.0.0.1");

        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.NotNull(result.Data);

        var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
        Assert.Contains("open_ports", json.ToLower());
        Assert.Contains("total_scanned", json.ToLower());
    }

    [Theory]
    [InlineData("8.8.8.8")]       // public IP
    [InlineData("1.1.1.1")]       // Cloudflare DNS
    [InlineData("104.21.32.1")]   // random public IP
    public async Task ScanAsync_PublicIp_ReturnsError(string publicIp)
    {
        var result = await _scanner.ScanAsync(publicIp);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("127.0.0.1",  true)]
    [InlineData("localhost",   true)]
    [InlineData("10.0.0.1",   true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("172.16.0.1",  true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("8.8.8.8",    false)]
    [InlineData("1.1.1.1",    false)]
    [InlineData("172.32.0.1", false)]  // just outside 172.16-31 range
    public void IsPrivateOrLocalhost_ReturnsCorrectResult(string ip, bool expected)
    {
        Assert.Equal(expected, PortScanner.IsPrivateOrLocalhost(ip));
    }
}
