using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

[ApiController]
[Route("health")]
public class HealthController(IAssetService service) : ControllerBase
{
    [HttpGet]
    public IActionResult Health() => Ok(new
    {
        status = "ok",
        asset_count = service.Count(),
        timestamp = DateTime.UtcNow
    });
}
