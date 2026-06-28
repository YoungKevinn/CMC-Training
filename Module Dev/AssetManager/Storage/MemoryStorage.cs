using AssetManager.Models;

namespace AssetManager.Storage;

/// <summary>
/// Thread-safe in-memory storage using ReaderWriterLockSlim
/// (C# equivalent of Go's sync.RWMutex — Bài 4).
/// </summary>
public class MemoryStorage : IAssetStorage, IDisposable
{
    private readonly Dictionary<string, Asset> _assets = [];
    private readonly ReaderWriterLockSlim _lock = new();

    // --- Create (Bài 4: concurrent-safe) ---

    public string Create(Asset asset)
    {
        _lock.EnterWriteLock();
        try
        {
            asset.Id = Guid.NewGuid().ToString();
            asset.CreatedAt = DateTime.UtcNow;
            asset.UpdatedAt = DateTime.UtcNow;
            _assets[asset.Id] = asset;
            return asset.Id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // --- Batch Create (Bài 2) ---

    public List<string> BatchCreate(List<Asset> assets)
    {
        _lock.EnterWriteLock();
        try
        {
            var ids = new List<string>(assets.Count);
            var now = DateTime.UtcNow;

            foreach (var asset in assets)
            {
                asset.Id = Guid.NewGuid().ToString();
                asset.CreatedAt = now;
                asset.UpdatedAt = now;
                _assets[asset.Id] = asset;
                ids.Add(asset.Id);
            }

            return ids;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // --- Read ---

    public Asset? GetById(string id)
    {
        _lock.EnterReadLock();
        try
        {
            return _assets.GetValueOrDefault(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // --- Update ---

    public bool Update(Asset asset)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_assets.ContainsKey(asset.Id)) return false;
            asset.UpdatedAt = DateTime.UtcNow;
            _assets[asset.Id] = asset;
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // --- Delete ---

    public bool Delete(string id)
    {
        _lock.EnterWriteLock();
        try
        {
            return _assets.Remove(id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // --- Batch Delete (Bài 3) ---

    public (int deleted, int notFound) BatchDelete(List<string> ids)
    {
        _lock.EnterWriteLock();
        try
        {
            int deleted = 0, notFound = 0;

            foreach (var id in ids)
            {
                if (_assets.Remove(id))
                    deleted++;
                else
                    notFound++;
            }

            return (deleted, notFound);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // --- Stats (Bài 1) ---

    public int Count()
    {
        _lock.EnterReadLock();
        try
        {
            return _assets.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public (int total, Dictionary<string, int> byType, Dictionary<string, int> byStatus) Stats()
    {
        _lock.EnterReadLock();
        try
        {
            var byType = new Dictionary<string, int>();
            var byStatus = new Dictionary<string, int>();

            foreach (var a in _assets.Values)
            {
                byType[a.Type] = byType.GetValueOrDefault(a.Type) + 1;
                byStatus[a.Status] = byStatus.GetValueOrDefault(a.Status) + 1;
            }

            return (_assets.Count, byType, byStatus);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int CountByFilter(string? type, string? status)
    {
        _lock.EnterReadLock();
        try
        {
            return _assets.Values.Count(a =>
                (type == null || a.Type == type) &&
                (status == null || a.Status == status));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // --- Pagination & Filtering (Bài 6) ---

    public (List<Asset> items, int total) ListFiltered(string? type, string? status, int page, int limit)
    {
        _lock.EnterReadLock();
        try
        {
            var filtered = _assets.Values
                .Where(a =>
                    (type == null || a.Type == type) &&
                    (status == null || a.Status == status))
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            int total = filtered.Count;
            var items = filtered
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return (items, total);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // --- Search (Bài 7) ---

    public List<Asset> SearchByName(string query, int maxResults)
    {
        _lock.EnterReadLock();
        try
        {
            return _assets.Values
                .Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
