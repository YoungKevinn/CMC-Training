namespace AssetManager.DTOs;

// POST /assets
public class CreateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Status { get; set; }
}

// POST /assets/batch (Bài 2)
public class BatchCreateRequest
{
    public List<CreateAssetRequest> Assets { get; set; } = [];
}

// PUT /assets/{id}
public class UpdateAssetRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
}
