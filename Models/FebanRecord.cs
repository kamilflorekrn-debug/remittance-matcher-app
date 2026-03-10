namespace RemittanceMatcherApp.Models;

public sealed class FebanRecord
{
    public required string Key { get; init; }
    public required string DisplayAmount { get; init; }
    public required string Partner { get; init; }
    public required DateTime StatementDate { get; init; }
}
