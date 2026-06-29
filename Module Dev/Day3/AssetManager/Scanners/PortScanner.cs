using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AssetManager.Scanners;

// Active TCP port scanner — ONLY allowed on localhost and private IP ranges
public class PortScanner : IScanner
{
    public string ScanType => "port";

    private static readonly int[] CommonPorts =
        [21, 22, 23, 25, 53, 80, 110, 111, 135, 139, 143, 443, 445, 993, 995,
         1723, 3306, 3389, 5432, 5900, 6379, 8080, 8443, 8888, 27017];

    private static readonly Dictionary<int, string> ServiceNames = new()
    {
        { 21, "ftp" }, { 22, "ssh" }, { 23, "telnet" }, { 25, "smtp" },
        { 53, "dns" }, { 80, "http" }, { 110, "pop3" }, { 111, "rpcbind" },
        { 135, "msrpc" }, { 139, "netbios-ssn" }, { 143, "imap" }, { 443, "https" },
        { 445, "smb" }, { 993, "imaps" }, { 995, "pop3s" }, { 1723, "pptp" },
        { 3306, "mysql" }, { 3389, "rdp" }, { 5432, "postgresql" }, { 5900, "vnc" },
        { 6379, "redis" }, { 8080, "http-alt" }, { 8443, "https-alt" },
        { 8888, "http-alt" }, { 27017, "mongodb" }
    };

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        // Safety: only scan localhost and private IP ranges
        if (!IsPrivateOrLocalhost(target))
        {
            return new ScanResultData
            {
                Success = false,
                Error = "Port scan is only allowed on localhost and private IP ranges (127.x, 10.x, 172.16-31.x, 192.168.x)"
            };
        }

        var sw = Stopwatch.StartNew();
        var scanTasks = CommonPorts.Select(port => ScanPortAsync(target, port, cancellationToken));
        var results = await Task.WhenAll(scanTasks);
        sw.Stop();

        var openPorts = results
            .Where(r => r.isOpen)
            .Select(r => new
            {
                port = r.port,
                protocol = "tcp",
                state = "open",
                service = ServiceNames.GetValueOrDefault(r.port, "unknown"),
                version = string.Empty
            })
            .OrderBy(p => p.port)
            .ToList<object>();

        return new ScanResultData
        {
            Success = true,
            Data = new
            {
                ip_address = target,
                open_ports = openPorts,
                closed_ports = CommonPorts.Length - openPorts.Count,
                total_scanned = CommonPorts.Length,
                scan_duration_ms = sw.ElapsedMilliseconds,
                created_at = DateTime.UtcNow
            }
        };
    }

    private static async Task<(int port, bool isOpen)> ScanPortAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            // 800ms timeout per port to keep scan fast
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(800);
            await client.ConnectAsync(host, port, cts.Token);
            return (port, true);
        }
        catch
        {
            return (port, false);
        }
    }

    // Safety check: only allow private/loopback ranges
    internal static bool IsPrivateOrLocalhost(string ip)
    {
        if (ip.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (!IPAddress.TryParse(ip, out var addr)) return false;

        var bytes = addr.GetAddressBytes();
        if (bytes.Length != 4) return addr.IsIPv6LinkLocal;

        return bytes[0] == 127 ||
               bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}
