namespace AssetManager.Models;

public class Asset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static readonly HashSet<string> ValidTypes = ["domain", "ip", "service"];
    public static readonly HashSet<string> ValidStatuses = ["active", "inactive"];
}
