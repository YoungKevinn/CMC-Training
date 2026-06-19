using AssetManager.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssetManager.DTOs;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

public class StatsResponse
{
    public int Total { get; set; }
    public Dictionary<string, int> ByType { get; set; } = [];
    public Dictionary<string, int> ByStatus { get; set; } = [];
}

public class CountResponse
{
    public int Count { get; set; }
}

public class PaginatedResponse
{
    public List<Asset> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
}

public class BatchCreateResponse
{
    public List<string> Ids { get; set; } = [];
    public int Created { get; set; }
}

public class BatchDeleteResponse
{
    public int Deleted { get; set; }
    public int NotFound { get; set; }
}

public class ScanJobResponse
{
    public string Id { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Error { get; set; } = string.Empty;
    public int Results { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ScanJobResponse From(ScanJob job) => new()
    {
        Id = job.Id,
        AssetId = job.AssetId,
        ScanType = job.ScanType,
        Status = job.Status,
        StartedAt = job.StartedAt,
        EndedAt = job.EndedAt,
        Error = job.Error,
        Results = job.ResultCount,
        CreatedAt = job.CreatedAt
    };
}

public class ScanJobResultsResponse
{
    public string JobId { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public List<JsonNode?> Results { get; set; } = [];

    public static ScanJobResultsResponse From(ScanJob job, IEnumerable<ScanResult> results) => new()
    {
        JobId = job.Id,
        ScanType = job.ScanType,
        Results = results
            .Select(r => JsonNode.Parse(r.DataJson))
            .ToList()
    };
}
