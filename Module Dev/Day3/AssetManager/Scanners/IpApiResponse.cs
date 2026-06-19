using System.Text.Json.Serialization;

namespace AssetManager.Scanners;

// Response schema from ip-api.com
public record IpApiResponse
{
    public string? Status { get; init; }
    public string? Message { get; init; }
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public double? Lat { get; init; }
    public double? Lon { get; init; }
    public string? Isp { get; init; }
    public string? Org { get; init; }
    [JsonPropertyName("as")]
    public string? AsNumber { get; init; }
    public string? Reverse { get; init; }
}
