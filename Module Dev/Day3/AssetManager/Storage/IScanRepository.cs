using AssetManager.Models;

namespace AssetManager.Storage;

public interface IScanRepository
{
    Task<ScanJob> CreateJobAsync(string assetId, string scanType);
    Task<ScanJob?> GetJobAsync(string id);
    Task UpdateJobStatusAsync(string id, string status, string? error = null, int resultCount = 0);
    Task SaveResultAsync(string jobId, string assetId, string scanType, object data);
    Task<List<ScanResult>> GetJobResultsAsync(string jobId);
    Task<List<ScanJob>> GetAssetJobsAsync(string assetId);
    Task<List<ScanResult>> GetAssetResultsAsync(string assetId, string? scanType = null);
}
