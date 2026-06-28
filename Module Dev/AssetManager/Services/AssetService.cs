using AssetManager.DTOs;
using AssetManager.Models;
using AssetManager.Storage;

namespace AssetManager.Services;

public class AssetService(IAssetStorage storage) : IAssetService
{
    private const int MaxBatchSize = 100;
    private const int MaxSearchResults = 100;

    // --- Validation helpers ---

    private static string? ValidateAsset(CreateAssetRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return "name is required";

        if (string.IsNullOrWhiteSpace(req.Type))
            return "type is required";

        if (!Asset.ValidTypes.Contains(req.Type))
            return $"invalid type '{req.Type}', must be one of: {string.Join(", ", Asset.ValidTypes)}";

        if (req.Status != null && !Asset.ValidStatuses.Contains(req.Status))
            return $"invalid status '{req.Status}', must be one of: {string.Join(", ", Asset.ValidStatuses)}";

        return null;
    }

    private static Asset ToAsset(CreateAssetRequest req) => new()
    {
        Name = req.Name.Trim(),
        Type = req.Type,
        Status = req.Status ?? "active"
    };

    // --- Single CRUD ---

    public (Asset asset, string? error) Create(CreateAssetRequest req)
    {
        var err = ValidateAsset(req);
        if (err != null) return (null!, err);

        var asset = ToAsset(req);
        storage.Create(asset);
        return (asset, null);
    }

    public (Asset? asset, string? error) GetById(string id)
    {
        var asset = storage.GetById(id);
        if (asset == null) return (null, "asset not found");
        return (asset, null);
    }

    public (Asset? asset, string? error) Update(string id, UpdateAssetRequest req)
    {
        var existing = storage.GetById(id);
        if (existing == null) return (null, "asset not found");

        if (req.Name != null) existing.Name = req.Name.Trim();

        if (req.Type != null)
        {
            if (!Asset.ValidTypes.Contains(req.Type))
                return (null, $"invalid type '{req.Type}'");
            existing.Type = req.Type;
        }

        if (req.Status != null)
        {
            if (!Asset.ValidStatuses.Contains(req.Status))
                return (null, $"invalid status '{req.Status}'");
            existing.Status = req.Status;
        }

        storage.Update(existing);
        return (existing, null);
    }

    public bool Delete(string id) => storage.Delete(id);

    // --- Bài 2: Batch Create (all-or-nothing) ---

    public (BatchCreateResponse? response, string? error) BatchCreate(BatchCreateRequest req)
    {
        if (req.Assets == null || req.Assets.Count == 0)
            return (null, "assets list is required and cannot be empty");

        if (req.Assets.Count > MaxBatchSize)
            return (null, $"maximum {MaxBatchSize} assets per request");

        // Validate ALL before writing (all-or-nothing).
        for (int i = 0; i < req.Assets.Count; i++)
        {
            var err = ValidateAsset(req.Assets[i]);
            if (err != null)
                return (null, $"asset[{i}]: {err}");
        }

        // All valid → convert and insert atomically.
        var assets = req.Assets.Select(ToAsset).ToList();
        var ids = storage.BatchCreate(assets);

        return (new BatchCreateResponse { Created = ids.Count, Ids = ids }, null);
    }

    // --- Bài 3: Batch Delete ---

    public BatchDeleteResponse BatchDelete(List<string> ids)
    {
        var (deleted, notFound) = storage.BatchDelete(ids);
        return new BatchDeleteResponse { Deleted = deleted, NotFound = notFound };
    }

    // --- Bài 1: Statistics ---

    public StatsResponse GetStats()
    {
        var (total, byType, byStatus) = storage.Stats();
        return new StatsResponse { Total = total, ByType = byType, ByStatus = byStatus };
    }

    public CountResponse CountByFilter(string? type, string? status)
    {
        var count = storage.CountByFilter(type, status);
        return new CountResponse
        {
            Count = count,
            Filters = new CountFilters { Type = type, Status = status }
        };
    }

    // --- Bài 6: Pagination ---

    public PaginatedResponse ListFiltered(string? type, string? status, int page, int limit)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 20;
        if (limit > 100) limit = 100;

        var (items, total) = storage.ListFiltered(type, status, page, limit);
        int totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / limit);

        return new PaginatedResponse
        {
            Data = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                Limit = limit,
                Total = total,
                TotalPages = totalPages
            }
        };
    }

    // --- Bài 7: Search ---

    public List<Asset> SearchByName(string query) =>
        storage.SearchByName(query, MaxSearchResults);

    public int Count() => storage.Count();
}
