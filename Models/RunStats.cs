namespace RemittanceMatcherApp.Models;

public sealed class RunStats
{
    public int TotalTransactions { get; set; }
    public int MatchedTransactions { get; set; }
    public int UnmatchedTransactions { get; set; }
    public int Checked { get; set; }
    public int PdfFound { get; set; }
    public int Saved { get; set; }
    public int Rejected { get; set; }
    public int PdfSaveFail { get; set; }
    public int PdfOpenFail { get; set; }
    public int OverflowSkips { get; set; }
    public int RuntimeErrors { get; set; }
    public int CacheSkips { get; set; }

    public int ProcessedMails => Checked;
}
