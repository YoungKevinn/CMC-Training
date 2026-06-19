namespace AssetManager.Models;

public class ScanJob
{
    public string Id { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Error { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Valid scan types: existing (Day 1-2) + new (Day 3)
    public static readonly HashSet<string> ValidScanTypes =
        ["dns", "whois", "subdomain", "cert_trans", "asn", "all", "ip", "port", "ssl", "tech"];

    // Which scan types are valid per asset type
    public static readonly Dictionary<string, HashSet<string>> AllowedTypesPerAsset = new()
    {
        ["domain"] = ["dns", "whois", "subdomain", "cert_trans", "all", "ssl", "tech"],
        ["ip"]     = ["asn", "ip", "port"],
        ["service"] = ["ssl", "tech"]
    };
}
