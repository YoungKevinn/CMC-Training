using DnsClient;

namespace AssetManager.Scanners;

public class DnsScanner : IScanner
{
    public string ScanType => "dns";

    private readonly LookupClient _client = new(new LookupClientOptions
    {
        Timeout = TimeSpan.FromSeconds(10),
        Retries = 2,
        UseCache = false
    });

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var aTask    = _client.QueryAsync(target, QueryType.A,   cancellationToken: cancellationToken);
            var mxTask   = _client.QueryAsync(target, QueryType.MX,  cancellationToken: cancellationToken);
            var nsTask   = _client.QueryAsync(target, QueryType.NS,  cancellationToken: cancellationToken);
            var txtTask  = _client.QueryAsync(target, QueryType.TXT, cancellationToken: cancellationToken);
            var aaaaTask = _client.QueryAsync(target, QueryType.AAAA, cancellationToken: cancellationToken);

            await Task.WhenAll(aTask, mxTask, nsTask, txtTask, aaaaTask);

            var records = new
            {
                A    = (await aTask).Answers.ARecords().Select(r => r.Address.ToString()).ToList(),
                AAAA = (await aaaaTask).Answers.AaaaRecords().Select(r => r.Address.ToString()).ToList(),
                MX   = (await mxTask).Answers.MxRecords()
                           .Select(r => new { exchange = r.Exchange.Value, preference = r.Preference }).ToList(),
                NS   = (await nsTask).Answers.NsRecords().Select(r => r.NSDName.Value).ToList(),
                TXT  = (await txtTask).Answers.TxtRecords()
                           .Select(r => string.Join("", r.Text)).ToList()
            };

            return new ScanResultData
            {
                Success = true,
                Data = new { domain = target, records, scanned_at = DateTime.UtcNow }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }
}
