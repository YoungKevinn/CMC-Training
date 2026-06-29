using System.Net.Sockets;
using System.Text;

namespace AssetManager.Scanners;

public class WhoisScanner : IScanner
{
    public string ScanType => "whois";

    // Common WHOIS servers per TLD
    private static readonly Dictionary<string, string> WhoisServers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["com"]  = "whois.verisign-grs.com",
        ["net"]  = "whois.verisign-grs.com",
        ["org"]  = "whois.pir.org",
        ["io"]   = "whois.nic.io",
        ["dev"]  = "whois.nic.google",
        ["app"]  = "whois.nic.google",
        ["vn"]   = "whois.vnnic.vn",
        ["uk"]   = "whois.nic.uk",
        ["de"]   = "whois.denic.de",
        ["jp"]   = "whois.jprs.jp",
        ["cn"]   = "whois.cnnic.cn",
        ["au"]   = "whois.audns.net.au",
        ["info"] = "whois.afilias.net",
        ["biz"]  = "whois.biz",
        ["co"]   = "whois.nic.co",
    };

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var tld = target.Split('.').Last().ToLower();
            var server = WhoisServers.GetValueOrDefault(tld, "whois.iana.org");

            var rawText = await QueryWhoisServer(server, target, cancellationToken);

            // Parse key fields from raw text
            var parsed = ParseWhoisFields(rawText);

            return new ScanResultData
            {
                Success = true,
                Data = new
                {
                    domain = target,
                    server = server,
                    registrar = parsed.GetValueOrDefault("Registrar"),
                    created_date = parsed.GetValueOrDefault("Creation Date") ?? parsed.GetValueOrDefault("Created Date"),
                    expiry_date = parsed.GetValueOrDefault("Registry Expiry Date") ?? parsed.GetValueOrDefault("Expiry Date"),
                    updated_date = parsed.GetValueOrDefault("Updated Date"),
                    name_servers = parsed.Where(p => p.Key.StartsWith("Name Server", StringComparison.OrdinalIgnoreCase))
                                        .Select(p => p.Value).Distinct().ToList(),
                    status = parsed.Where(p => p.Key.Equals("Domain Status", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => p.Value).ToList(),
                    raw = rawText,
                    scanned_at = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }

    private static async Task<string> QueryWhoisServer(string server, string query, CancellationToken ct)
    {
        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        await client.ConnectAsync(server, 43, cts.Token);

        using var stream = client.GetStream();
        var bytes = Encoding.ASCII.GetBytes(query + "\r\n");
        await stream.WriteAsync(bytes, cts.Token);

        using var reader = new StreamReader(stream, Encoding.ASCII);
        return await reader.ReadToEndAsync(cts.Token);
    }

    private static Dictionary<string, string> ParseWhoisFields(string raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in raw.Split('\n'))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx <= 0) continue;
            var key = line[..colonIdx].Trim();
            var value = line[(colonIdx + 1)..].Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result.TryAdd(key, value);
        }
        return result;
    }
}
