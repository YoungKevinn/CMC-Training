namespace AssetManager.Scanners;

public class ScannerFactory
{
    private readonly Dictionary<string, IScanner> _scanners;

    public ScannerFactory(IEnumerable<IScanner> scanners)
    {
        _scanners = scanners.ToDictionary(s => s.ScanType, s => s);
    }

    public IScanner? GetScanner(string scanType) => _scanners.GetValueOrDefault(scanType);
}
