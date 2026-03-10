namespace RemittanceMatcherApp.Models;

public sealed class ProgressUpdate
{
    public int Current { get; init; }
    public int Total { get; init; }
    public required string Status { get; init; }
}
