using AssetManager.DTOs;
using AssetManager.Models;
using AssetManager.Services;
using AssetManager.Storage;
using Moq;
using Xunit;

namespace AssetManager.Tests.Services;

// 3.2/3.3 Bonus: service tests with mock storage
public class AssetServiceTests
{
    private readonly Mock<IAssetStorage> _mockStorage;
    private readonly AssetService _service;

    public AssetServiceTests()
    {
        _mockStorage = new Mock<IAssetStorage>();
        _service = new AssetService(_mockStorage.Object);
    }

    // --- Create tests ---

    [Fact]
    public void Create_ValidDomain_CallsStorageAndReturnsAsset()
    {
        _mockStorage.Setup(s => s.Create(It.IsAny<Asset>())).Returns("new-id");

        var (asset, error) = _service.Create(new CreateAssetRequest { Name = "example.com", Type = "domain" });

        Assert.Null(error);
        Assert.Equal("example.com", asset.Name);
        Assert.Equal("domain", asset.Type);
        _mockStorage.Verify(s => s.Create(It.IsAny<Asset>()), Times.Once);
    }

    [Fact]
    public void Create_EmptyName_ReturnsError()
    {
        var (_, error) = _service.Create(new CreateAssetRequest { Name = "", Type = "domain" });
        Assert.NotNull(error);
        _mockStorage.Verify(s => s.Create(It.IsAny<Asset>()), Times.Never);
    }

    [Fact]
    public void Create_InvalidType_ReturnsError()
    {
        var (_, error) = _service.Create(new CreateAssetRequest { Name = "test", Type = "server" });
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("domain")]
    [InlineData("ip")]
    [InlineData("service")]
    public void Create_AllValidTypes_Succeed(string type)
    {
        _mockStorage.Setup(s => s.Create(It.IsAny<Asset>())).Returns("id");
        var (asset, error) = _service.Create(new CreateAssetRequest { Name = "test", Type = type });
        Assert.Null(error);
        Assert.Equal(type, asset.Type);
    }

    // --- GetById tests ---

    [Fact]
    public void GetById_ExistingAsset_ReturnsIt()
    {
        var expected = new Asset { Id = "abc", Name = "test.com", Type = "domain" };
        _mockStorage.Setup(s => s.GetById("abc")).Returns(expected);

        var (asset, error) = _service.GetById("abc");

        Assert.Null(error);
        Assert.Equal("test.com", asset!.Name);
    }

    [Fact]
    public void GetById_NotFound_ReturnsError()
    {
        _mockStorage.Setup(s => s.GetById("missing")).Returns((Asset?)null);
        var (asset, error) = _service.GetById("missing");
        Assert.Null(asset);
        Assert.Equal("asset not found", error);
    }

    // --- Delete tests ---

    [Fact]
    public void Delete_ExistingAsset_ReturnsTrue()
    {
        _mockStorage.Setup(s => s.Delete("id-1")).Returns(true);
        Assert.True(_service.Delete("id-1"));
    }

    [Fact]
    public void Delete_NotFound_ReturnsFalse()
    {
        _mockStorage.Setup(s => s.Delete("missing")).Returns(false);
        Assert.False(_service.Delete("missing"));
    }

    // --- Update tests ---

    [Fact]
    public void Update_ValidRequest_UpdatesFields()
    {
        var existing = new Asset { Id = "id-1", Name = "old.com", Type = "domain", Status = "active" };
        _mockStorage.Setup(s => s.GetById("id-1")).Returns(existing);
        _mockStorage.Setup(s => s.Update(It.IsAny<Asset>())).Returns(true);

        var (asset, error) = _service.Update("id-1", new UpdateAssetRequest { Name = "new.com", Status = "inactive" });

        Assert.Null(error);
        Assert.Equal("new.com", asset!.Name);
        Assert.Equal("inactive", asset.Status);
    }

    [Fact]
    public void Update_InvalidStatus_ReturnsError()
    {
        var existing = new Asset { Id = "id-1", Name = "test.com", Type = "domain" };
        _mockStorage.Setup(s => s.GetById("id-1")).Returns(existing);

        var (_, error) = _service.Update("id-1", new UpdateAssetRequest { Status = "invalid" });
        Assert.NotNull(error);
    }

    // --- Stats tests ---

    [Fact]
    public void GetStats_ReturnsCorrectStructure()
    {
        _mockStorage.Setup(s => s.Stats()).Returns((
            5,
            new Dictionary<string, int> { ["domain"] = 3, ["ip"] = 2 },
            new Dictionary<string, int> { ["active"] = 4, ["inactive"] = 1 }
        ));

        var stats = _service.GetStats();

        Assert.Equal(5, stats.Total);
        Assert.Equal(3, stats.ByType["domain"]);
        Assert.Equal(4, stats.ByStatus["active"]);
    }

    // --- Pagination tests ---

    [Fact]
    public void ListFiltered_PageTooLow_ClampsToOne()
    {
        _mockStorage.Setup(s => s.ListFiltered(null, null, 1, 20))
            .Returns((new List<Asset>(), 0));

        var result = _service.ListFiltered(null, null, page: -5, limit: 20);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public void ListFiltered_LimitTooHigh_ClampsTo20()
    {
        _mockStorage.Setup(s => s.ListFiltered(null, null, 1, 20))
            .Returns((new List<Asset>(), 0));

        var result = _service.ListFiltered(null, null, page: 1, limit: 9999);
        Assert.Equal(20, result.Limit);
    }
}
