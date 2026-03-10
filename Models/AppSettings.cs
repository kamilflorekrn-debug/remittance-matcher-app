namespace RemittanceMatcherApp.Models;

public sealed class AppSettings
{
    public string LanguageCode { get; set; } = "pl";

    public int LookbackDays { get; set; } = 7;

    // Backward compatibility (stary format ustawien)
    public string MailboxRootName { get; set; } = "ukremittance";
    public string TargetSubfolderName { get; set; } = "Remittance advices";
    public string SecondMailboxRootName { get; set; } = "AR.UKIE";

    // Nowy model: dowolna liczba skrzynek root i podfolderow inbox.
    public List<MailboxSearchScope> Mailboxes { get; set; } =
    [
        new MailboxSearchScope
        {
            RootName = "ukremittance",
            InboxSubfolders = ["Remittance advices"]
        },
        new MailboxSearchScope
        {
            RootName = "AR.UKIE"
        }
    ];

    public string FebanCsvRelativeFolder { get; set; } = "\\!NORTH_DACH\\02_OPERATIONS\\GB02_Getinge_Ltd\\OTC\\Remittance advices\\02. SEB Remittance advices\\Robot\\";
    public string FebanCsvFilename { get; set; } = "transactions.csv";
    public string RemitBaseRelativeFolder { get; set; } = "\\!NORTH_DACH\\02_OPERATIONS\\GB02_Getinge_Ltd\\OTC\\Remittance advices\\02. SEB Remittance advices\\";

    public bool EnablePass2SubjectBodyFallback { get; set; } = true;
    public bool EnablePass3InlineSaveMsg { get; set; } = true;
    public bool EnableProcessedPdfCache { get; set; } = true;

    public int MaxPdfSizeMb { get; set; } = 15;

    public int ScoreMinToSave { get; set; } = 120;
    public int ScoreMarginToSave { get; set; } = 45;
    public bool RequireHardSignalOrTwoSignals { get; set; } = true;
    public bool RequireCandidateInTotalContext { get; set; } = true;

    public int DoEventsEveryNItems { get; set; } = 20;

    public string InvoiceNumberRegex { get; set; } = @"\b3129\d{5}\b";
    public int InvoiceBlockWindowLines { get; set; } = 1;
    public int SingleInvoiceScoreBonus { get; set; } = 20;
    public int SingleInvoiceMarginBonus { get; set; } = 10;

    public int TotalContextWindowChars { get; set; } = 280;

    public int StrongTotalNeighborLines { get; set; } = 1;
    public int TotalKeywordNeighborLines { get; set; } = 1;
    public bool AllowLooseDigitsAmountMatch { get; set; } = true;
    public int MinDigitsForLooseMatch { get; set; } = 5;

    public int ScoreStrongTotalWeight { get; set; } = 130;
    public int ScoreTotalContextWeight { get; set; } = 85;
    public int ScoreKeywordWindowWeight { get; set; } = 60;
    public int ScoreLineItemPenaltyWeight { get; set; } = -90;

    public bool PreferHardTotalOverHigherScore { get; set; } = true;
    public bool BlockInvoiceContextWithoutStrongTotal { get; set; } = true;

    public string[] StrongTotalKeywords { get; set; } =
    [
        "grand total",
        "amount paid to account",
        "amount paid",
        "payment by bacs",
        "paid by bacs",
        "bank transfer total"
    ];

    public string[] TotalKeywords { get; set; } =
    [
        "total",
        "amount paid",
        "payment amount",
        "amount payable",
        "payment by bacs",
        "amount paid to account"
    ];
}
