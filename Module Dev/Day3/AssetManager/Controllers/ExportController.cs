using System.Text;
using System.Text.Json;
using AssetManager.Storage;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

[ApiController]
[Route("export")]
public class ExportController(IAssetStorage assetStorage, IScanRepository scanRepo) : ControllerBase
{
    // GET /export/assets.csv — all assets as CSV
    [HttpGet("assets.csv")]
    public IActionResult ExportAssetsCsv()
    {
        var (items, _) = assetStorage.ListFiltered(null, null, 1, 10000);

        var sb = new StringBuilder();
        sb.AppendLine("id,name,type,status,created_at,updated_at");
        foreach (var a in items)
            sb.AppendLine($"{Csv(a.Id)},{Csv(a.Name)},{Csv(a.Type)},{Csv(a.Status)},{a.CreatedAt:O},{a.UpdatedAt:O}");

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "assets.csv");
    }

    // GET /export/assets.json — all assets as JSON
    [HttpGet("assets.json")]
    public IActionResult ExportAssetsJson()
    {
        var (items, total) = assetStorage.ListFiltered(null, null, 1, 10000);
        return Ok(new { exported_at = DateTime.UtcNow, total, assets = items });
    }

    // GET /assets/{id}/export/results.json
    [HttpGet("/assets/{id}/export/results.json")]
    public async Task<IActionResult> ExportResultsJson(string id)
    {
        var asset = assetStorage.GetById(id);
        if (asset is null) return NotFound(new { error = "Asset not found" });

        var results = await scanRepo.GetAssetResultsAsync(id);
        var jobs    = await scanRepo.GetAssetJobsAsync(id);

        var payload = new
        {
            exported_at = DateTime.UtcNow,
            asset,
            scan_summary = new
            {
                total_jobs    = jobs.Count,
                completed     = jobs.Count(j => j.Status == "completed"),
                failed        = jobs.Count(j => j.Status == "failed"),
                total_results = results.Count
            },
            results = results.Select(r => new
            {
                r.Id,
                r.ScanType,
                data = JsonSerializer.Deserialize<object>(r.DataJson ?? "{}"),
                r.CreatedAt
            })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json",
            $"asset_{id}_results.json");
    }

    // GET /assets/{id}/export/results.csv
    [HttpGet("/assets/{id}/export/results.csv")]
    public async Task<IActionResult> ExportResultsCsv(string id)
    {
        var asset = assetStorage.GetById(id);
        if (asset is null) return NotFound(new { error = "Asset not found" });

        var results = await scanRepo.GetAssetResultsAsync(id);

        var sb = new StringBuilder();
        sb.AppendLine("id,job_id,asset_id,scan_type,created_at,data_summary");
        foreach (var r in results)
        {
            // Build short summary from DataJson
            string summary = BuildSummary(r.ScanType, r.DataJson);
            sb.AppendLine($"{r.Id},{r.JobId},{r.AssetId},{Csv(r.ScanType)},{r.CreatedAt:O},{Csv(summary)}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"asset_{id}_results.csv");
    }

    // GET /scan-jobs/{id}/export/results.json
    [HttpGet("/scan-jobs/{id}/export/results.json")]
    public async Task<IActionResult> ExportJobResultsJson(string id)
    {
        var job = await scanRepo.GetJobAsync(id);
        if (job is null) return NotFound(new { error = "Job not found" });

        var results = await scanRepo.GetJobResultsAsync(id);

        var payload = new
        {
            exported_at = DateTime.UtcNow,
            job,
            results = results.Select(r => new
            {
                r.Id,
                r.ScanType,
                data = JsonSerializer.Deserialize<object>(r.DataJson ?? "{}"),
                r.CreatedAt
            })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json",
            $"job_{id}_results.json");
    }

    private static string BuildSummary(string scanType, string? dataJson)
    {
        if (string.IsNullOrEmpty(dataJson)) return "";
        try
        {
            using var doc = JsonDocument.Parse(dataJson);
            var root = doc.RootElement;
            return scanType switch
            {
                "dns"        => $"a:{GetNestedArrayLen(root,"records","a")} mx:{GetNestedArrayLen(root,"records","mx")} ns:{GetNestedArrayLen(root,"records","ns")}",
                "ssl"        => $"grade:{GetStr(root,"grade")} tls:{GetNestedStr(root,"connection","tls_version")}",
                "port"       => $"open:{GetArrayLen(root,"open_ports")} scanned:{GetInt(root,"total_scanned")}",
                "ip"         => $"country:{GetNestedStr(root,"geolocation","country")} asn:{GetNestedStr(root,"asn","number")}",
                "subdomain"  => $"count:{GetArrayLen(root,"subdomains")}",
                "tech"       => $"count:{GetArrayLen(root,"technologies")}",
                _            => ""
            };
        }
        catch { return ""; }
    }

    private static int GetArrayLen(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array)
            return el.GetArrayLength();
        return 0;
    }

    private static int GetNestedArrayLen(JsonElement root, string key1, string key2)
    {
        if (root.TryGetProperty(key1, out var el1) && el1.TryGetProperty(key2, out var el2)
            && el2.ValueKind == JsonValueKind.Array)
            return el2.GetArrayLength();
        return 0;
    }

    private static string GetStr(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var el)) return el.ToString();
        return "";
    }

    private static string GetNestedStr(JsonElement root, string key1, string key2)
    {
        if (root.TryGetProperty(key1, out var el1) && el1.TryGetProperty(key2, out var el2))
            return el2.ToString();
        return "";
    }

    private static int GetInt(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var el) && el.TryGetInt32(out int v)) return v;
        return 0;
    }

    // RFC 4180 CSV escaping
    private static string Csv(string? s)
    {
        if (s is null) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
