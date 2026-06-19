using AssetManager.DTOs;
using AssetManager.Models;

namespace AssetManager.Services;

public interface IAssetService
{
    (Asset asset, string? error) Create(CreateAssetRequest req);
    (Asset? asset, string? error) GetById(string id);
    (Asset? asset, string? error) Update(string id, UpdateAssetRequest req);
    bool Delete(string id);
    (BatchCreateResponse? response, string? error) BatchCreate(BatchCreateRequest req);
    BatchDeleteResponse BatchDelete(List<string> ids);
    StatsResponse GetStats();
    CountResponse CountByFilter(string? type, string? status);
    PaginatedResponse ListFiltered(string? type, string? status, int page, int limit);
    List<Asset> SearchByName(string query);
    int Count();
}
