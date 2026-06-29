using System.Text.RegularExpressions;

namespace AssetManager.Scanners;

// Technology detection via HTTP response headers and body (new in Day 3)
public class TechScanner : IScanner
{
    public string ScanType => "tech";

    private readonly IHttpClientFactory _factory;

    private static readonly HashSet<string> InterestingHeaders =
        ["server", "x-powered-by", "content-type", "x-frame-options",
         "x-xss-protection", "strict-transport-security", "x-content-type-options",
         "cf-ray", "x-aspnet-version", "x-generator"];

    public TechScanner(IHttpClientFactory factory) => _factory = factory;

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        // Try HTTPS first, fall back to HTTP
        foreach (var scheme in new[] { "https", "http" })
        {
            try
            {
                return await ScanUrl($"{scheme}://{target}", target, cancellationToken);
            }
            catch when (scheme == "https") { /* try http */ }
        }
        return new ScanResultData { Success = false, Error = "Failed to connect to target" };
    }

    private async Task<ScanResultData> ScanUrl(string url, string target, CancellationToken ct)
    {
        var http = _factory.CreateClient("scanner");
        var response = await http.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Merge response headers + content headers, lowercased
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key.ToLower(), h => string.Join(", ", h.Value));

        var technologies = DetectTechnologies(headers, body);
        var metaTags = ExtractMetaTags(body);
        var filteredHeaders = headers
            .Where(h => InterestingHeaders.Contains(h.Key))
            .ToDictionary(h => h.Key, h => h.Value);

        return new ScanResultData
        {
            Success = true,
            Data = new
            {
                domain = target,
                technologies,
                headers = filteredHeaders,
                meta_tags = metaTags,
                status_code = (int)response.StatusCode,
                created_at = DateTime.UtcNow
            }
        };
    }

    private static List<object> DetectTechnologies(Dictionary<string, string> headers, string body)
    {
        var techs = new List<object>();

        if (headers.TryGetValue("server", out var server))
        {
            var (name, version) = SplitNameVersion(server);
            techs.Add(new { name, category = "Web Server", version, confidence = 100 });
        }

        if (headers.TryGetValue("x-powered-by", out var poweredBy))
            techs.Add(new { name = poweredBy, category = "Framework", version = (string?)null, confidence = 90 });

        if (headers.ContainsKey("cf-ray") || headers.ContainsKey("cf-cache-status"))
            techs.Add(new { name = "Cloudflare", category = "CDN", version = (string?)null, confidence = 100 });

        if (headers.TryGetValue("x-aspnet-version", out var aspVer))
            techs.Add(new { name = "ASP.NET", category = "Framework", version = aspVer, confidence = 100 });

        if (body.Contains("wp-content/") || body.Contains("wp-includes/"))
            techs.Add(new { name = "WordPress", category = "CMS", version = (string?)null, confidence = 95 });

        if (body.Contains("Drupal.settings") || body.Contains("/sites/default/"))
            techs.Add(new { name = "Drupal", category = "CMS", version = (string?)null, confidence = 90 });

        if (body.Contains("_next/") || body.Contains("__NEXT_DATA__"))
            techs.Add(new { name = "Next.js", category = "JavaScript Framework", version = (string?)null, confidence = 95 });

        if (Regex.IsMatch(body, @"react[\.\-]dom", RegexOptions.IgnoreCase))
            techs.Add(new { name = "React", category = "JavaScript Framework", version = (string?)null, confidence = 85 });

        if (Regex.IsMatch(body, @"ng-version|angular\.js", RegexOptions.IgnoreCase))
            techs.Add(new { name = "Angular", category = "JavaScript Framework", version = (string?)null, confidence = 85 });

        if (Regex.IsMatch(body, @"vue\.min\.js|__vue__", RegexOptions.IgnoreCase))
            techs.Add(new { name = "Vue.js", category = "JavaScript Framework", version = (string?)null, confidence = 85 });

        if (Regex.IsMatch(body, @"bootstrap\.min\.(css|js)", RegexOptions.IgnoreCase))
            techs.Add(new { name = "Bootstrap", category = "CSS Framework", version = (string?)null, confidence = 80 });

        if (body.Contains("jquery") || body.Contains("jQuery"))
            techs.Add(new { name = "jQuery", category = "JavaScript Library", version = (string?)null, confidence = 75 });

        return techs;
    }

    private static (string name, string? version) SplitNameVersion(string header)
    {
        var slash = header.IndexOf('/');
        if (slash < 0) return (header.Trim(), null);
        return (header[..slash].Trim(), header[(slash + 1)..].Trim());
    }

    private static Dictionary<string, string> ExtractMetaTags(string html)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(
            html,
            @"<meta\s[^>]*?name=[""']([^""']+)[""'][^>]*?content=[""']([^""']+)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match m in matches)
            result.TryAdd(m.Groups[1].Value.ToLower(), m.Groups[2].Value);

        // Also try reversed attribute order
        var reversed = Regex.Matches(
            html,
            @"<meta\s[^>]*?content=[""']([^""']+)[""'][^>]*?name=[""']([^""']+)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match m in reversed)
            result.TryAdd(m.Groups[2].Value.ToLower(), m.Groups[1].Value);

        return result;
    }
}
