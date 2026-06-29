using AssetManager.Data;
using AssetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Storage;

public class EfAssetStorage : IAssetStorage
{
    private readonly AppDbContext _db;

    public EfAssetStorage(AppDbContext db) => _db = db;

    public string Create(Asset asset)
    {
        asset.Id = Guid.NewGuid().ToString();
        asset.CreatedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        _db.Assets.Add(asset);
        _db.SaveChanges();
        return asset.Id;
    }

    public List<string> BatchCreate(List<Asset> assets)
    {
        var now = DateTime.UtcNow;
        var ids = new List<string>();
        foreach (var asset in assets)
        {
            asset.Id = Guid.NewGuid().ToString();
            asset.CreatedAt = now;
            asset.UpdatedAt = now;
            _db.Assets.Add(asset);
            ids.Add(asset.Id);
        }
        _db.SaveChanges();
        return ids;
    }

    public Asset? GetById(string id) => _db.Assets.Find(id);

    public bool Update(Asset asset)
    {
        var existing = _db.Assets.Find(asset.Id);
        if (existing == null) return false;
        existing.Name = asset.Name;
        existing.Type = asset.Type;
        existing.Status = asset.Status;
        existing.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return true;
    }

    public bool Delete(string id)
    {
        var asset = _db.Assets.Find(id);
        if (asset == null) return false;
        _db.Assets.Remove(asset);
        _db.SaveChanges();
        return true;
    }

    public (int deleted, int notFound) BatchDelete(List<string> ids)
    {
        int deleted = 0, notFound = 0;
        foreach (var id in ids)
        {
            var asset = _db.Assets.Find(id);
            if (asset != null) { _db.Assets.Remove(asset); deleted++; }
            else notFound++;
        }
        _db.SaveChanges();
        return (deleted, notFound);
    }

    public int Count() => _db.Assets.Count();

    public (int total, Dictionary<string, int> byType, Dictionary<string, int> byStatus) Stats()
    {
        var assets = _db.Assets.ToList();
        return (
            assets.Count,
            assets.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count()),
            assets.GroupBy(a => a.Status).ToDictionary(g => g.Key, g => g.Count())
        );
    }

    public int CountByFilter(string? type, string? status) =>
        _db.Assets.Count(a =>
            (type == null || a.Type == type) &&
            (status == null || a.Status == status));

    public (List<Asset> items, int total) ListFiltered(string? type, string? status, int page, int limit)
    {
        var query = _db.Assets
            .Where(a => (type == null || a.Type == type) && (status == null || a.Status == status))
            .OrderByDescending(a => a.CreatedAt);

        var total = query.Count();
        var items = query.Skip((page - 1) * limit).Take(limit).ToList();
        return (items, total);
    }

    public List<Asset> SearchByName(string query, int maxResults) =>
        _db.Assets
            .Where(a => EF.Functions.Like(a.Name, $"%{query}%"))
            .Take(maxResults)
            .ToList();
}
