namespace RemittanceMatcherApp.Models;

public sealed class MatchResult
{
    public required string Key { get; init; }
    public required string Reason { get; init; }
    public int Score { get; init; }
    public int Margin { get; init; }
}
