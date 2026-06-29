using System.Net;
using System.Text.Json;

namespace AssetManager.Scanners;

// Full IP geolocation + ASN + reverse DNS scan (new in Day 3)
public class IpScanner : IScanner
{
    public string ScanType => "ip";

    private readonly IHttpClientFactory _factory;

    public IpScanner(IHttpClientFactory factory) => _factory = factory;

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var http = _factory.CreateClient("scanner");
            var url = $"http://ip-api.com/json/{target}?fields=status,message,country,countryCode,region,city,lat,lon,isp,org,as,reverse";
            var json = await http.GetStringAsync(url, cancellationToken);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var info = JsonSerializer.Deserialize<IpApiResponse>(json, opts);

            if (info?.Status == "fail")
                return new ScanResultData { Success = false, Error = info.Message ?? "ip-api lookup failed" };

            // Reverse DNS lookup as fallback
            var reverseDns = info?.Reverse ?? string.Empty;
            if (string.IsNullOrEmpty(reverseDns))
            {
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(target, cancellationToken);
                    reverseDns = hostEntry.HostName;
                }
                catch { /* best-effort */ }
            }

            var (asnNumber, asnName) = AsnScanner.ParseAsn(info?.AsNumber);

            return new ScanResultData
            {
                Success = true,
                Data = new
                {
                    ip_address = target,
                    geolocation = new
                    {
                        country = info?.Country,
                        country_code = info?.CountryCode,
                        city = info?.City,
                        region = info?.Region,
                        latitude = info?.Lat,
                        longitude = info?.Lon,
                        isp = info?.Isp,
                        org = info?.Org
                    },
                    asn = new
                    {
                        number = asnNumber,
                        name = asnName,
                        description = info?.Org ?? asnName
                    },
                    reverse_dns = reverseDns,
                    created_at = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }
}
