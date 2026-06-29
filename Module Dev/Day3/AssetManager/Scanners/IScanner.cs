namespace AssetManager.Scanners;

public interface IScanner
{
    string ScanType { get; }
    Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default);
}

public class ScanResultData
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public object? Data { get; set; }
}
