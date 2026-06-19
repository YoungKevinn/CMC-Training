namespace AssetManager.DTOs;

public class CreateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class UpdateAssetRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
}

public class BatchCreateRequest
{
    public List<CreateAssetRequest> Assets { get; set; } = [];
}

public class StartScanRequest
{
    public string ScanType { get; set; } = string.Empty;
}
