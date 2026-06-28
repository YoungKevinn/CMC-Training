using AssetManager.DTOs;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

[ApiController]
[Route("health")]
public class HealthController(IAssetService service) : ControllerBase
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    // GET /health (Bài 5)
    [HttpGet]
    public IActionResult GetHealth()
    {
        var uptime = (long)(DateTime.UtcNow - StartTime).TotalSeconds;

        return Ok(new HealthResponse
        {
            Status = "ok",
            Storage = new StorageInfo
            {
                Type = "in-memory",
                AssetCount = service.Count()
            },
            UptimeSeconds = uptime,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }
}
