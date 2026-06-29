using System.Text.Json;

namespace AssetManager.Scanners;

// Certificate Transparency log lookup via crt.sh
public class CertTransScanner : IScanner
{
    public string ScanType => "cert_trans";

    private readonly IHttpClientFactory _factory;

    public CertTransScanner(IHttpClientFactory factory) => _factory = factory;

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var http = _factory.CreateClient("scanner");
            var url = $"https://crt.sh/?q={Uri.EscapeDataString(target)}&output=json";
            var json = await http.GetStringAsync(url, cancellationToken);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var entries = JsonSerializer.Deserialize<List<CrtShEntry>>(json, opts) ?? [];

            // Take latest 50 certificates
            var certs = entries
                .OrderByDescending(e => e.NotBefore)
                .Take(50)
                .Select(e => new
                {
                    id = e.Id,
                    common_name = e.CommonName,
                    issuer = e.IssuerName,
                    names = e.NameValue.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                    not_before = e.NotBefore,
                    not_after = e.NotAfter
                })
                .ToList();

            return new ScanResultData
            {
                Success = true,
                Data = new { domain = target, certificates = certs, total = entries.Count, scanned_at = DateTime.UtcNow }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }
}
