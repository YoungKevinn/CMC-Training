using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace AssetManager.Scanners;

// SSL/TLS certificate inspection (new in Day 3)
public class SslScanner : IScanner
{
    public string ScanType => "ssl";

    public async Task<ScanResultData> ScanAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            await tcpClient.ConnectAsync(target, 443, cts.Token);

            // Accept all certs so we can inspect even self-signed
            using var sslStream = new SslStream(
                tcpClient.GetStream(),
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (_, _, _, _) => true);

            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = target,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            }, cts.Token);

            var rawCert = sslStream.RemoteCertificate
                ?? throw new InvalidOperationException("No certificate received");

            var cert = new X509Certificate2(rawCert.Export(X509ContentType.Cert));

            var now = DateTime.UtcNow;
            var daysToExpiry = (int)(cert.NotAfter.ToUniversalTime() - now).TotalDays;
            var isSelfSigned = cert.Subject == cert.Issuer;

            // Extract SAN (Subject Alternative Names)
            var san = new List<string>();
            foreach (var ext in cert.Extensions)
            {
                if (ext.Oid?.Value == "2.5.29.17" && ext is X509SubjectAlternativeNameExtension sanExt)
                    san.AddRange(sanExt.EnumerateDnsNames());
            }

            var tlsVersion = sslStream.SslProtocol switch
            {
                SslProtocols.Tls12 => "TLS 1.2",
                SslProtocols.Tls13 => "TLS 1.3",
                _                  => sslStream.SslProtocol.ToString()
            };

            var issues = BuildIssues(cert, sslStream.SslProtocol);
            var grade = DetermineGrade(sslStream.SslProtocol, isSelfSigned, daysToExpiry, issues);

            return new ScanResultData
            {
                Success = true,
                Data = new
                {
                    domain = target,
                    certificate = new
                    {
                        subject = cert.Subject,
                        issuer = cert.Issuer,
                        serial_number = cert.SerialNumber,
                        valid_from = cert.NotBefore.ToUniversalTime(),
                        valid_until = cert.NotAfter.ToUniversalTime(),
                        days_until_expiry = daysToExpiry,
                        is_expired = daysToExpiry < 0,
                        is_self_signed = isSelfSigned,
                        san
                    },
                    connection = new
                    {
                        tls_version = tlsVersion,
                        cipher_suite = sslStream.NegotiatedCipherSuite.ToString()
                    },
                    grade,
                    issues,
                    created_at = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ScanResultData { Success = false, Error = ex.Message };
        }
    }

    private static string DetermineGrade(SslProtocols protocol, bool selfSigned, int daysToExpiry, List<string> issues)
    {
        if (selfSigned || daysToExpiry < 0) return "F";
        if (issues.Count > 0) return "C";
        if (protocol == SslProtocols.Tls13) return "A+";
        if (protocol == SslProtocols.Tls12) return "A";
        return "B";
    }

    private static List<string> BuildIssues(X509Certificate2 cert, SslProtocols protocol)
    {
        var issues = new List<string>();
        if (cert.Subject == cert.Issuer) issues.Add("Self-signed certificate");
        if (cert.NotAfter < DateTime.UtcNow) issues.Add("Certificate is expired");
        else if (cert.NotAfter < DateTime.UtcNow.AddDays(30)) issues.Add("Certificate expires within 30 days");
        if ((int)protocol < (int)SslProtocols.Tls12) issues.Add("TLS 1.1 or lower is deprecated");
        return issues;
    }
}
