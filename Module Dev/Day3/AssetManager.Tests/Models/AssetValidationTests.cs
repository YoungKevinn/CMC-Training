using AssetManager.Models;
using Xunit;

namespace AssetManager.Tests.Models;

public class AssetValidationTests
{
    // 3.1: Model validation tests — no mock needed

    [Theory]
    [InlineData("example.com", "domain", true)]
    [InlineData("192.168.1.1", "ip",     true)]
    [InlineData("http://svc",  "service", true)]
    [InlineData("",            "domain", false)]  // empty name
    [InlineData("  ",          "domain", false)]  // whitespace name
    [InlineData("example.com", "",       false)]  // empty type
    [InlineData("example.com", "unknown", false)] // invalid type
    [InlineData("example.com", "Domain", false)]  // case-sensitive check
    public void Validate_ReturnsExpectedResult(string name, string type, bool expectedValid)
    {
        var asset = new Asset { Name = name, Type = type };
        var (isValid, error) = Asset.Validate(asset);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid) Assert.NotNull(error);
        if (expectedValid)  Assert.Null(error);
    }

    [Fact]
    public void ValidTypes_ContainsAllExpectedValues()
    {
        Assert.Contains("domain", Asset.ValidTypes);
        Assert.Contains("ip",     Asset.ValidTypes);
        Assert.Contains("service", Asset.ValidTypes);
        Assert.Equal(3, Asset.ValidTypes.Count);
    }

    [Fact]
    public void ValidStatuses_ContainsAllExpectedValues()
    {
        Assert.Contains("active",   Asset.ValidStatuses);
        Assert.Contains("inactive", Asset.ValidStatuses);
        Assert.Equal(2, Asset.ValidStatuses.Count);
    }

    [Fact]
    public void Asset_DefaultStatus_IsActive()
    {
        var asset = new Asset();
        Assert.Equal("active", asset.Status);
    }

    [Fact]
    public void Validate_ValidDomain_ReturnsNoError()
    {
        var asset = new Asset { Name = "google.com", Type = "domain" };
        var (isValid, error) = Asset.Validate(asset);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_InvalidType_ErrorMentionsValidTypes()
    {
        var asset = new Asset { Name = "test", Type = "server" };
        var (_, error) = Asset.Validate(asset);
        Assert.NotNull(error);
        // Error should tell user what the valid values are
        Assert.Contains("domain", error, StringComparison.OrdinalIgnoreCase);
    }
}
