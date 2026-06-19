using System.Text.Json;

namespace AssetManager.Scanners;

// Passive subdomain enumeration via Certificate Transparency logs (crt.sh)
public class SubdomainScanner : IScanner
{
    public string ScanType => "subdomain";

    private readonly IHttpClientFactory _factory;

    public SubdomainScanner(IHttpClientFactory factory) => _factory = factory;

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var http = _factory.CreateClient("scanner");
            // Search for wildcard subdomain certs
            var url = $"https://crt.sh/?q=%25.{Uri.EscapeDataString(target)}&output=json";
            var json = await http.GetStringAsync(url, cancellationToken);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var entries = JsonSerializer.Deserialize<List<CrtShEntry>>(json, opts) ?? [];

            var subdomains = entries
                .SelectMany(e => e.NameValue.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.TrimStart('*').TrimStart('.').ToLower())
                .Where(s => s.EndsWith(target) && s != target && !s.StartsWith("*"))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return new ScanResultData
            {
                Success = true,
                Data = new { domain = target, subdomains, count = subdomains.Count, scanned_at = DateTime.UtcNow }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }
}
