using System.Text.Json.Serialization;

namespace AssetManager.Scanners;

// Response schema from crt.sh Certificate Transparency log search
public record CrtShEntry
{
    public long Id { get; init; }

    [JsonPropertyName("name_value")]
    public string NameValue { get; init; } = string.Empty;

    [JsonPropertyName("issuer_name")]
    public string IssuerName { get; init; } = string.Empty;

    [JsonPropertyName("common_name")]
    public string CommonName { get; init; } = string.Empty;

    [JsonPropertyName("not_before")]
    public string NotBefore { get; init; } = string.Empty;

    [JsonPropertyName("not_after")]
    public string NotAfter { get; init; } = string.Empty;
}
