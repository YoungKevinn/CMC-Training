using AssetManager.Models;

namespace AssetManager.Services;

public interface IScanService
{
    Task<(ScanJob? job, string? error)> StartScanAsync(string assetId, string scanType);
    Task<ScanJob?> GetJobAsync(string id);
    Task<(ScanJob? job, List<ScanResult> results)> GetJobResultsAsync(string id);
    Task<List<ScanJob>> GetAssetScansAsync(string assetId);
    Task<List<ScanResult>> GetAssetResultsAsync(string assetId, string? scanType = null);
    Task ExecuteJobAsync(string jobId, CancellationToken ct);
}
