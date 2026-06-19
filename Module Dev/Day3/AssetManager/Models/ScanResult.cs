namespace AssetManager.Models;

public class ScanResult
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    // Stored as JSON string for flexibility across different scanner result schemas
    public string DataJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
