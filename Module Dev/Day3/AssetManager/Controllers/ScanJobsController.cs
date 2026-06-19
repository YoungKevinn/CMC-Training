using AssetManager.DTOs;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

[ApiController]
[Route("scan-jobs")]
public class ScanJobsController(IScanService scanService) : ControllerBase
{
    // GET /scan-jobs/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(string id)
    {
        var job = await scanService.GetJobAsync(id);
        if (job == null) return NotFound(new ErrorResponse { Error = "scan job not found" });
        return Ok(ScanJobResponse.From(job));
    }

    // GET /scan-jobs/{id}/results
    [HttpGet("{id}/results")]
    public async Task<IActionResult> GetJobResults(string id)
    {
        var (job, results) = await scanService.GetJobResultsAsync(id);
        if (job == null) return NotFound(new ErrorResponse { Error = "scan job not found" });
        return Ok(ScanJobResultsResponse.From(job, results));
    }
}
