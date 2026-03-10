namespace RemittanceMatcherApp.Models;

public sealed class CacheEntry
{
    public required string Fingerprint { get; init; }
    public required DateTime ReceivedTime { get; init; }
    public required DateTime LastChecked { get; init; }
    public required string Status { get; init; }
    public required string Reason { get; init; }
}
