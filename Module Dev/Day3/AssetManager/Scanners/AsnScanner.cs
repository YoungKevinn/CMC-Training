using System.Text.Json;

namespace AssetManager.Scanners;

// ASN lookup for IP assets using ip-api.com (free, no auth required)
public class AsnScanner : IScanner
{
    public string ScanType => "asn";

    private readonly IHttpClientFactory _factory;

    public AsnScanner(IHttpClientFactory factory) => _factory = factory;

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var http = _factory.CreateClient("scanner");
            var url = $"http://ip-api.com/json/{target}?fields=status,message,as,org";
            var json = await http.GetStringAsync(url, cancellationToken);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var info = JsonSerializer.Deserialize<IpApiResponse>(json, opts);

            if (info?.Status == "fail")
                return new ScanResultData { Success = false, Error = info.Message ?? "ip-api lookup failed" };

            var (number, name) = ParseAsn(info?.AsNumber);

            return new ScanResultData
            {
                Success = true,
                Data = new
                {
                    ip = target,
                    asn = new { number, name, description = info?.Org ?? name },
                    scanned_at = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }

    internal static (int number, string name) ParseAsn(string? asString)
    {
        if (string.IsNullOrEmpty(asString)) return (0, string.Empty);
        // Format from ip-api: "AS13335 Cloudflare, Inc."
        var parts = asString.Split(' ', 2);
        var number = int.TryParse(parts[0].Replace("AS", ""), out var n) ? n : 0;
        var name = parts.Length > 1 ? parts[1] : string.Empty;
        return (number, name);
    }
}
