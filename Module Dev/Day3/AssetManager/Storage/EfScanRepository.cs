using System.Text.Json;
using AssetManager.Data;
using AssetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Storage;

public class EfScanRepository : IScanRepository
{
    private readonly AppDbContext _db;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public EfScanRepository(AppDbContext db) => _db = db;

    public async Task<ScanJob> CreateJobAsync(string assetId, string scanType)
    {
        var job = new ScanJob
        {
            Id = Guid.NewGuid().ToString(),
            AssetId = assetId,
            ScanType = scanType,
            Status = "pending",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.ScanJobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task<ScanJob?> GetJobAsync(string id) =>
        await _db.ScanJobs.FindAsync(id);

    public async Task UpdateJobStatusAsync(string id, string status, string? error = null, int resultCount = 0)
    {
        var job = await _db.ScanJobs.FindAsync(id);
        if (job == null) return;
        job.Status = status;
        job.Error = error ?? string.Empty;
        job.ResultCount = resultCount;
        if (status is "completed" or "failed" or "partial")
            job.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SaveResultAsync(string jobId, string assetId, string scanType, object data)
    {
        var result = new ScanResult
        {
            Id = Guid.NewGuid().ToString(),
            JobId = jobId,
            AssetId = assetId,
            ScanType = scanType,
            DataJson = JsonSerializer.Serialize(data, _json),
            CreatedAt = DateTime.UtcNow
        };
        _db.ScanResults.Add(result);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ScanResult>> GetJobResultsAsync(string jobId) =>
        await _db.ScanResults
            .Where(r => r.JobId == jobId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

    public async Task<List<ScanJob>> GetAssetJobsAsync(string assetId) =>
        await _db.ScanJobs
            .Where(j => j.AssetId == assetId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

    public async Task<List<ScanResult>> GetAssetResultsAsync(string assetId, string? scanType = null) =>
        await _db.ScanResults
            .Where(r => r.AssetId == assetId && (scanType == null || r.ScanType == scanType))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}
