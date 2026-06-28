using AssetManager.DTOs;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

[ApiController]
[Route("assets")]
public class AssetsController(IAssetService service) : ControllerBase
{
    // ===== CRUD cơ bản =====

    // POST /assets
    [HttpPost]
    public IActionResult Create([FromBody] CreateAssetRequest req)
    {
        var (asset, error) = service.Create(req);
        if (error != null) return BadRequest(new ErrorResponse { Error = error });
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    // GET /assets/{id}
    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var (asset, error) = service.GetById(id);
        if (error != null) return NotFound(new ErrorResponse { Error = error });
        return Ok(asset);
    }

    // PUT /assets/{id}
    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateAssetRequest req)
    {
        var (asset, error) = service.Update(id, req);
        if (error != null)
        {
            if (error == "asset not found") return NotFound(new ErrorResponse { Error = error });
            return BadRequest(new ErrorResponse { Error = error });
        }
        return Ok(asset);
    }

    // DELETE /assets/{id}
    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        if (!service.Delete(id))
            return NotFound(new ErrorResponse { Error = "asset not found" });
        return NoContent();
    }

    // ===== Bài 1: Statistics =====

    // GET /assets/stats
    [HttpGet("stats")]
    public IActionResult GetStats() => Ok(service.GetStats());

    // GET /assets/count
    [HttpGet("count")]
    public IActionResult Count(
        [FromQuery] string? type,
        [FromQuery] string? status)
    {
        return Ok(service.CountByFilter(type, status));
    }

    // ===== Bài 2: Batch Create =====

    // POST /assets/batch
    [HttpPost("batch")]
    public IActionResult BatchCreate([FromBody] BatchCreateRequest req)
    {
        var (response, error) = service.BatchCreate(req);
        if (error != null) return BadRequest(new ErrorResponse { Error = error });
        return StatusCode(201, response);
    }

    // ===== Bài 3: Batch Delete =====

    // DELETE /assets/batch?ids=uuid1,uuid2,uuid3
    [HttpDelete("batch")]
    public IActionResult BatchDelete([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return BadRequest(new ErrorResponse { Error = "ids parameter is required" });

        var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        return Ok(service.BatchDelete(idList));
    }

    // ===== Bài 6: Pagination & Filtering (BONUS) =====

    // GET /assets?page=1&limit=20&type=domain&status=active
    [HttpGet]
    public IActionResult List(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null)
    {
        return Ok(service.ListFiltered(type, status, page, limit));
    }

    // ===== Bài 7: Search by Name (BONUS) =====

    // GET /assets/search?q=example
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new ErrorResponse { Error = "q parameter is required" });
        return Ok(service.SearchByName(q));
    }
}
