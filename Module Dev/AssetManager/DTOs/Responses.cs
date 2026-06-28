using System.Text.Json.Serialization;

namespace AssetManager.DTOs;

// GET /assets/stats (Bài 1.1)
public class StatsResponse
{
    public int Total { get; set; }

    [JsonPropertyName("by_type")]
    public Dictionary<string, int> ByType { get; set; } = [];

    [JsonPropertyName("by_status")]
    public Dictionary<string, int> ByStatus { get; set; } = [];
}

// GET /assets/count (Bài 1.2)
public class CountResponse
{
    public int Count { get; set; }
    public CountFilters Filters { get; set; } = new();
}

public class CountFilters
{
    public string? Type { get; set; }
    public string? Status { get; set; }
}

// POST /assets/batch (Bài 2)
public class BatchCreateResponse
{
    public int Created { get; set; }
    public List<string> Ids { get; set; } = [];
}

// DELETE /assets/batch (Bài 3)
public class BatchDeleteResponse
{
    public int Deleted { get; set; }

    [JsonPropertyName("not_found")]
    public int NotFound { get; set; }
}

// GET /health (Bài 5)
public class HealthResponse
{
    public string Status { get; set; } = "ok";
    public StorageInfo Storage { get; set; } = new();

    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; set; }

    public string Timestamp { get; set; } = string.Empty;
}

public class StorageInfo
{
    public string Type { get; set; } = "in-memory";

    [JsonPropertyName("asset_count")]
    public int AssetCount { get; set; }
}

// GET /assets with pagination (Bài 6)
public class PaginatedResponse
{
    public List<Models.Asset> Data { get; set; } = [];
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

// Error response
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
