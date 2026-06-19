using AssetManager.DTOs;
using AssetManager.Models;
using AssetManager.Storage;

namespace AssetManager.Services;

public class AssetService : IAssetService
{
    private readonly IAssetStorage _storage;

    public AssetService(IAssetStorage storage) => _storage = storage;

    public (Asset asset, string? error) Create(CreateAssetRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return (null!, "name is required");
        if (!Asset.ValidTypes.Contains(req.Type))
            return (null!, $"type must be one of: {string.Join(", ", Asset.ValidTypes)}");

        var asset = new Asset { Name = req.Name.Trim(), Type = req.Type };
        _storage.Create(asset);
        return (asset, null);
    }

    public (Asset? asset, string? error) GetById(string id)
    {
        var asset = _storage.GetById(id);
        return asset == null ? (null, "asset not found") : (asset, null);
    }

    public (Asset? asset, string? error) Update(string id, UpdateAssetRequest req)
    {
        var existing = _storage.GetById(id);
        if (existing == null) return (null, "asset not found");

        if (req.Type != null && !Asset.ValidTypes.Contains(req.Type))
            return (null, $"type must be one of: {string.Join(", ", Asset.ValidTypes)}");
        if (req.Status != null && !Asset.ValidStatuses.Contains(req.Status))
            return (null, $"status must be one of: {string.Join(", ", Asset.ValidStatuses)}");

        if (req.Name != null) existing.Name = req.Name.Trim();
        if (req.Type != null) existing.Type = req.Type;
        if (req.Status != null) existing.Status = req.Status;

        _storage.Update(existing);
        return (existing, null);
    }

    public bool Delete(string id) => _storage.Delete(id);

    public (BatchCreateResponse? response, string? error) BatchCreate(BatchCreateRequest req)
    {
        if (req.Assets == null || req.Assets.Count == 0)
            return (null, "assets list is required and must not be empty");

        var assets = new List<Asset>();
        foreach (var item in req.Assets)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                return (null, "each asset must have a name");
            if (!Asset.ValidTypes.Contains(item.Type))
                return (null, $"invalid type '{item.Type}' — must be one of: {string.Join(", ", Asset.ValidTypes)}");
            assets.Add(new Asset { Name = item.Name.Trim(), Type = item.Type });
        }

        var ids = _storage.BatchCreate(assets);
        return (new BatchCreateResponse { Ids = ids, Created = ids.Count }, null);
    }

    public BatchDeleteResponse BatchDelete(List<string> ids)
    {
        var (deleted, notFound) = _storage.BatchDelete(ids);
        return new BatchDeleteResponse { Deleted = deleted, NotFound = notFound };
    }

    public StatsResponse GetStats()
    {
        var (total, byType, byStatus) = _storage.Stats();
        return new StatsResponse { Total = total, ByType = byType, ByStatus = byStatus };
    }

    public CountResponse CountByFilter(string? type, string? status) =>
        new() { Count = _storage.CountByFilter(type, status) };

    public PaginatedResponse ListFiltered(string? type, string? status, int page, int limit)
    {
        if (page < 1) page = 1;
        if (limit < 1 || limit > 100) limit = 20;

        var (items, total) = _storage.ListFiltered(type, status, page, limit);
        return new PaginatedResponse
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit,
            TotalPages = (int)Math.Ceiling((double)total / limit)
        };
    }

    public List<Asset> SearchByName(string query) =>
        _storage.SearchByName(query, 50);

    public int Count() => _storage.Count();
}
