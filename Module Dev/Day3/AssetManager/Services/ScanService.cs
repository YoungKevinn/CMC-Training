using System.Threading.Channels;
using AssetManager.Models;
using AssetManager.Scanners;
using AssetManager.Storage;
using Microsoft.Extensions.Logging;

namespace AssetManager.Services;

public class ScanService : IScanService
{
    private readonly IAssetStorage _assets;
    private readonly IScanRepository _scanRepo;
    private readonly ScannerFactory _factory;
    private readonly Channel<string> _jobChannel;
    private readonly ILogger<ScanService> _logger;

    // Scan types to run when scan_type == "all"
    private static readonly string[] AllPassiveScans = ["dns", "whois", "subdomain", "cert_trans"];

    public ScanService(
        IAssetStorage assets,
        IScanRepository scanRepo,
        ScannerFactory factory,
        Channel<string> jobChannel,
        ILogger<ScanService> logger)
    {
        _assets = assets;
        _scanRepo = scanRepo;
        _factory = factory;
        _jobChannel = jobChannel;
        _logger = logger;
    }

    public async Task<(ScanJob? job, string? error)> StartScanAsync(string assetId, string scanType)
    {
        var asset = _assets.GetById(assetId);
        if (asset == null) return (null, "asset not found");

        if (!ScanJob.ValidScanTypes.Contains(scanType))
            return (null, $"scan_type must be one of: {string.Join(", ", ScanJob.ValidScanTypes)}");

        // Validate scan type is compatible with asset type
        if (ScanJob.AllowedTypesPerAsset.TryGetValue(asset.Type, out var allowed) && !allowed.Contains(scanType))
            return (null, $"scan_type '{scanType}' is not valid for asset type '{asset.Type}'");

        var job = await _scanRepo.CreateJobAsync(assetId, scanType);

        // Enqueue for background processing
        await _jobChannel.Writer.WriteAsync(job.Id);

        _logger.LogInformation("Scan job {JobId} ({ScanType}) queued for asset {AssetId}", job.Id, scanType, assetId);
        return (job, null);
    }

    public async Task<ScanJob?> GetJobAsync(string id) => await _scanRepo.GetJobAsync(id);

    public async Task<(ScanJob? job, List<ScanResult> results)> GetJobResultsAsync(string id)
    {
        var job = await _scanRepo.GetJobAsync(id);
        if (job == null) return (null, []);
        var results = await _scanRepo.GetJobResultsAsync(id);
        return (job, results);
    }

    public async Task<List<ScanJob>> GetAssetScansAsync(string assetId) =>
        await _scanRepo.GetAssetJobsAsync(assetId);

    public async Task<List<ScanResult>> GetAssetResultsAsync(string assetId, string? scanType = null) =>
        await _scanRepo.GetAssetResultsAsync(assetId, scanType);

    // Called by ScanWorker in the background
    public async Task ExecuteJobAsync(string jobId, CancellationToken ct)
    {
        var job = await _scanRepo.GetJobAsync(jobId);
        if (job == null) return;

        var asset = _assets.GetById(job.AssetId);
        if (asset == null)
        {
            await _scanRepo.UpdateJobStatusAsync(jobId, "failed", "Asset not found");
            return;
        }

        await _scanRepo.UpdateJobStatusAsync(jobId, "running");
        _logger.LogInformation("Executing scan job {JobId} ({ScanType}) on {Target}", jobId, job.ScanType, asset.Name);

        var scanTypes = job.ScanType == "all" ? AllPassiveScans : [job.ScanType];
        int resultCount = 0;
        string? lastError = null;

        foreach (var scanType in scanTypes)
        {
            if (ct.IsCancellationRequested) break;

            var scanner = _factory.GetScanner(scanType);
            if (scanner == null)
            {
                lastError = $"No scanner for type: {scanType}";
                _logger.LogWarning("No scanner found for type {ScanType}", scanType);
                continue;
            }

            try
            {
                var result = await scanner.ScanAsync(asset.Name, ct);
                if (result.Success && result.Data != null)
                {
                    await _scanRepo.SaveResultAsync(jobId, asset.Id, scanType, result.Data);
                    resultCount++;
                    _logger.LogInformation("Scan {ScanType} completed for job {JobId}", scanType, jobId);
                }
                else
                {
                    lastError = result.Error;
                    _logger.LogWarning("Scan {ScanType} failed for job {JobId}: {Error}", scanType, jobId, result.Error);
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogError(ex, "Scanner {ScanType} threw exception for job {JobId}", scanType, jobId);
            }
        }

        // Determine final status
        string finalStatus = (resultCount == 0 && lastError != null)
            ? "failed"
            : (lastError != null ? "partial" : "completed");

        await _scanRepo.UpdateJobStatusAsync(jobId, finalStatus, lastError, resultCount);
        _logger.LogInformation("Job {JobId} finished with status {Status} ({Count} results)", jobId, finalStatus, resultCount);
    }
}
