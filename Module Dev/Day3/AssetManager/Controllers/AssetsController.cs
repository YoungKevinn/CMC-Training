using AssetManager.DTOs;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

[ApiController]
[Route("assets")]
public class AssetsController(IAssetService service, IScanService scanService) : ControllerBase
{
    // ===== CRUD =====

    [HttpPost]
    public IActionResult Create([FromBody] CreateAssetRequest req)
    {
        var (asset, error) = service.Create(req);
        if (error != null) return BadRequest(new ErrorResponse { Error = error });
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        return Ok(asset);
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateAssetRequest req)
    {
        var (asset, error) = service.Update(id, req);
        if (error == null) return Ok(asset);
        return error == "asset not found"
            ? NotFound(new ErrorResponse { Error = error })
            : BadRequest(new ErrorResponse { Error = error });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        if (!service.Delete(id)) return NotFound(new ErrorResponse { Error = "asset not found" });
        return NoContent();
    }

    // ===== Stats / Count / List / Search (from Day 1) =====

    [HttpGet("stats")]
    public IActionResult GetStats() => Ok(service.GetStats());

    [HttpGet("count")]
    public IActionResult Count([FromQuery] string? type, [FromQuery] string? status) =>
        Ok(service.CountByFilter(type, status));

    [HttpGet]
    public IActionResult List(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null)
        => Ok(service.ListFiltered(type, status, page, limit));

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new ErrorResponse { Error = "q parameter is required" });
        return Ok(service.SearchByName(q));
    }

    [HttpPost("batch")]
    public IActionResult BatchCreate([FromBody] BatchCreateRequest req)
    {
        var (response, error) = service.BatchCreate(req);
        if (error != null) return BadRequest(new ErrorResponse { Error = error });
        return StatusCode(201, response);
    }

    [HttpDelete("batch")]
    public IActionResult BatchDelete([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return BadRequest(new ErrorResponse { Error = "ids parameter is required" });
        var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        return Ok(service.BatchDelete(idList));
    }

    // ===== Scan endpoints (Day 3) =====

    // POST /assets/{id}/scan
    [HttpPost("{id}/scan")]
    public async Task<IActionResult> StartScan(string id, [FromBody] StartScanRequest req)
    {
        var (job, error) = await scanService.StartScanAsync(id, req.ScanType);
        if (error != null)
        {
            if (error == "asset not found") return NotFound(new ErrorResponse { Error = error });
            return BadRequest(new ErrorResponse { Error = error });
        }
        return StatusCode(202, ScanJobResponse.From(job!));
    }

    // GET /assets/{id}/scans
    [HttpGet("{id}/scans")]
    public async Task<IActionResult> GetAssetScans(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        var jobs = await scanService.GetAssetScansAsync(id);
        return Ok(jobs.Select(ScanJobResponse.From));
    }

    // GET /assets/{id}/results
    [HttpGet("{id}/results")]
    public async Task<IActionResult> GetAssetResults(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        var results = await scanService.GetAssetResultsAsync(id);
        return Ok(results);
    }

    // GET /assets/{id}/dns
    [HttpGet("{id}/dns")]
    public async Task<IActionResult> GetAssetDns(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        var results = await scanService.GetAssetResultsAsync(id, "dns");
        return Ok(results);
    }

    // GET /assets/{id}/whois
    [HttpGet("{id}/whois")]
    public async Task<IActionResult> GetAssetWhois(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        var results = await scanService.GetAssetResultsAsync(id, "whois");
        return Ok(results);
    }

    // GET /assets/{id}/subdomains
    [HttpGet("{id}/subdomains")]
    public async Task<IActionResult> GetAssetSubdomains(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        var results = await scanService.GetAssetResultsAsync(id, "subdomain");
        return Ok(results);
    }
}
