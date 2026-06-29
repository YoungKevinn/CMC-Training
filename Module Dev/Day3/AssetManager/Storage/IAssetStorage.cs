using AssetManager.Models;

namespace AssetManager.Storage;

public interface IAssetStorage
{
    string Create(Asset asset);
    List<string> BatchCreate(List<Asset> assets);
    Asset? GetById(string id);
    bool Update(Asset asset);
    bool Delete(string id);
    (int deleted, int notFound) BatchDelete(List<string> ids);
    int Count();
    (int total, Dictionary<string, int> byType, Dictionary<string, int> byStatus) Stats();
    int CountByFilter(string? type, string? status);
    (List<Asset> items, int total) ListFiltered(string? type, string? status, int page, int limit);
    List<Asset> SearchByName(string query, int maxResults);
}
