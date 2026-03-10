using System.Windows;
using RemittanceMatcherApp.Localization;
using RemittanceMatcherApp.Models;

namespace RemittanceMatcherApp;

public partial class AdvancedSettingsWindow : Window
{
    private readonly AppSettings _initial;

    public AppSettings? ResultSettings { get; private set; }

    public AdvancedSettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _initial = CloneSettings(settings);
        ApplyLanguageUi();
        ApplyToUi(_initial);
        Loaded += AdvancedSettingsWindow_Loaded;
    }

    private void AdvancedSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Math.Max(Width, 860), area.Width * 0.9);
        Height = Math.Min(Math.Max(Height, 620), area.Height * 0.9);
    }

    private void ApplyLanguageUi()
    {
        CenterHeaderText.Text = L("header_main");
        Title = L("adv_title");
        AdvancedHeaderText.Text = L("adv_header");
        ApplyButton.Content = L("adv_btn_apply");
        CancelButton.Content = L("adv_btn_cancel");

        LblScoreMin.Text = L("adv_lbl_score_min");
        LblScoreMargin.Text = L("adv_lbl_score_margin");
        LblRequireHard.Text = L("adv_lbl_require_hard");
        LblRequireTotalContext.Text = L("adv_lbl_require_total_context");
        LblInvoiceRegex.Text = L("adv_lbl_invoice_regex");
        LblInvoiceWindowLines.Text = L("adv_lbl_invoice_window_lines");
        LblSingleInvoiceScoreBonus.Text = L("adv_lbl_single_invoice_score_bonus");
        LblSingleInvoiceMarginBonus.Text = L("adv_lbl_single_invoice_margin_bonus");
        LblTotalContextWindowChars.Text = L("adv_lbl_total_context_window_chars");
        LblStrongNeighborLines.Text = L("adv_lbl_strong_neighbor_lines");
        LblTotalNeighborLines.Text = L("adv_lbl_total_neighbor_lines");
        LblAllowLooseDigits.Text = L("adv_lbl_allow_loose_digits");
        LblMinLooseDigits.Text = L("adv_lbl_min_loose_digits");
        LblStrongScoreWeight.Text = L("adv_lbl_strong_score_weight");
        LblTotalScoreWeight.Text = L("adv_lbl_total_score_weight");
        LblKeywordScoreWeight.Text = L("adv_lbl_keyword_score_weight");
        LblInvoicePenaltyWeight.Text = L("adv_lbl_invoice_penalty_weight");
        LblPreferHardTotal.Text = L("adv_lbl_prefer_hard_total");
        LblBlockInvoiceContext.Text = L("adv_lbl_block_invoice_context");
        LblStrongKeywords.Text = L("adv_lbl_strong_keywords");
        LblTotalKeywords.Text = L("adv_lbl_total_keywords");

        TtScoreMin.Text = L("adv_tt_score_min");
        TtScoreMargin.Text = L("adv_tt_score_margin");
        TtRequireHard.Text = L("adv_tt_require_hard");
        TtRequireTotalContext.Text = L("adv_tt_require_total_context");
        TtInvoiceRegex.Text = L("adv_tt_invoice_regex");
        TtInvoiceWindowLines.Text = L("adv_tt_invoice_window_lines");
        TtSingleInvoiceScoreBonus.Text = L("adv_tt_single_invoice_score_bonus");
        TtSingleInvoiceMarginBonus.Text = L("adv_tt_single_invoice_margin_bonus");
        TtTotalContextWindowChars.Text = L("adv_tt_total_context_window_chars");
        TtStrongNeighborLines.Text = L("adv_tt_strong_neighbor_lines");
        TtTotalNeighborLines.Text = L("adv_tt_total_neighbor_lines");
        TtAllowLooseDigits.Text = L("adv_tt_allow_loose_digits");
        TtMinLooseDigits.Text = L("adv_tt_min_loose_digits");
        TtStrongScoreWeight.Text = L("adv_tt_strong_score_weight");
        TtTotalScoreWeight.Text = L("adv_tt_total_score_weight");
        TtKeywordScoreWeight.Text = L("adv_tt_keyword_score_weight");
        TtInvoicePenaltyWeight.Text = L("adv_tt_invoice_penalty_weight");
        TtPreferHardTotal.Text = L("adv_tt_prefer_hard_total");
        TtBlockInvoiceContext.Text = L("adv_tt_block_invoice_context");
        TtStrongKeywords.Text = L("adv_tt_strong_keywords");
        TtTotalKeywords.Text = L("adv_tt_total_keywords");
    }

    private void ApplyToUi(AppSettings s)
    {
        ScoreMinBox.Text = s.ScoreMinToSave.ToString();
        ScoreMarginBox.Text = s.ScoreMarginToSave.ToString();
        RequireHardBox.IsChecked = s.RequireHardSignalOrTwoSignals;
        RequireTotalContextBox.IsChecked = s.RequireCandidateInTotalContext;

        InvoiceRegexBox.Text = s.InvoiceNumberRegex;
        InvoiceWindowLinesBox.Text = s.InvoiceBlockWindowLines.ToString();
        SingleInvoiceScoreBonusBox.Text = s.SingleInvoiceScoreBonus.ToString();
        SingleInvoiceMarginBonusBox.Text = s.SingleInvoiceMarginBonus.ToString();
        TotalContextWindowCharsBox.Text = s.TotalContextWindowChars.ToString();

        StrongNeighborLinesBox.Text = s.StrongTotalNeighborLines.ToString();
        TotalNeighborLinesBox.Text = s.TotalKeywordNeighborLines.ToString();
        AllowLooseDigitsBox.IsChecked = s.AllowLooseDigitsAmountMatch;
        MinLooseDigitsBox.Text = s.MinDigitsForLooseMatch.ToString();

        StrongScoreWeightBox.Text = s.ScoreStrongTotalWeight.ToString();
        TotalScoreWeightBox.Text = s.ScoreTotalContextWeight.ToString();
        KeywordScoreWeightBox.Text = s.ScoreKeywordWindowWeight.ToString();
        InvoicePenaltyWeightBox.Text = s.ScoreLineItemPenaltyWeight.ToString();

        PreferHardTotalBox.IsChecked = s.PreferHardTotalOverHigherScore;
        BlockInvoiceContextBox.IsChecked = s.BlockInvoiceContextWithoutStrongTotal;

        StrongKeywordsBox.Text = string.Join(Environment.NewLine, s.StrongTotalKeywords);
        TotalKeywordsBox.Text = string.Join(Environment.NewLine, s.TotalKeywords);
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var s = BuildFromUi();
            ResultSettings = s;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private AppSettings BuildFromUi()
    {
        var s = CloneSettings(_initial);

        s.ScoreMinToSave = ParseInt(ScoreMinBox.Text, s.ScoreMinToSave, 1, 1000, L("adv_lbl_score_min"));
        s.ScoreMarginToSave = ParseInt(ScoreMarginBox.Text, s.ScoreMarginToSave, 0, 1000, L("adv_lbl_score_margin"));
        s.RequireHardSignalOrTwoSignals = RequireHardBox.IsChecked == true;
        s.RequireCandidateInTotalContext = RequireTotalContextBox.IsChecked == true;

        var regexText = InvoiceRegexBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(regexText))
        {
            throw new InvalidOperationException(L("adv_msg_regex_required"));
        }

        s.InvoiceNumberRegex = regexText;
        s.InvoiceBlockWindowLines = ParseInt(InvoiceWindowLinesBox.Text, s.InvoiceBlockWindowLines, 0, 20, L("adv_lbl_invoice_window_lines"));
        s.SingleInvoiceScoreBonus = ParseInt(SingleInvoiceScoreBonusBox.Text, s.SingleInvoiceScoreBonus, 0, 500, L("adv_lbl_single_invoice_score_bonus"));
        s.SingleInvoiceMarginBonus = ParseInt(SingleInvoiceMarginBonusBox.Text, s.SingleInvoiceMarginBonus, 0, 500, L("adv_lbl_single_invoice_margin_bonus"));
        s.TotalContextWindowChars = ParseInt(TotalContextWindowCharsBox.Text, s.TotalContextWindowChars, 30, 5000, L("adv_lbl_total_context_window_chars"));

        s.StrongTotalNeighborLines = ParseInt(StrongNeighborLinesBox.Text, s.StrongTotalNeighborLines, 0, 10, L("adv_lbl_strong_neighbor_lines"));
        s.TotalKeywordNeighborLines = ParseInt(TotalNeighborLinesBox.Text, s.TotalKeywordNeighborLines, 0, 10, L("adv_lbl_total_neighbor_lines"));
        s.AllowLooseDigitsAmountMatch = AllowLooseDigitsBox.IsChecked == true;
        s.MinDigitsForLooseMatch = ParseInt(MinLooseDigitsBox.Text, s.MinDigitsForLooseMatch, 3, 12, L("adv_lbl_min_loose_digits"));

        s.ScoreStrongTotalWeight = ParseInt(StrongScoreWeightBox.Text, s.ScoreStrongTotalWeight, 0, 500, L("adv_lbl_strong_score_weight"));
        s.ScoreTotalContextWeight = ParseInt(TotalScoreWeightBox.Text, s.ScoreTotalContextWeight, 0, 500, L("adv_lbl_total_score_weight"));
        s.ScoreKeywordWindowWeight = ParseInt(KeywordScoreWeightBox.Text, s.ScoreKeywordWindowWeight, 0, 500, L("adv_lbl_keyword_score_weight"));
        s.ScoreLineItemPenaltyWeight = ParseInt(InvoicePenaltyWeightBox.Text, s.ScoreLineItemPenaltyWeight, -500, 0, L("adv_lbl_invoice_penalty_weight"));

        s.PreferHardTotalOverHigherScore = PreferHardTotalBox.IsChecked == true;
        s.BlockInvoiceContextWithoutStrongTotal = BlockInvoiceContextBox.IsChecked == true;

        s.StrongTotalKeywords = ParseLines(StrongKeywordsBox.Text, s.StrongTotalKeywords);
        s.TotalKeywords = ParseLines(TotalKeywordsBox.Text, s.TotalKeywords);

        return s;
    }

    private static string[] ParseLines(string text, string[] fallback)
    {
        var lines = text
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return lines.Length > 0 ? lines : fallback;
    }

    private string L(string key) => AppLocalizer.T(_initial.LanguageCode, key);

    private int ParseInt(string text, int fallback, int min, int max, string fieldName)
    {
        if (!int.TryParse(text.Trim(), out var value))
        {
            value = fallback;
        }

        if (value < min || value > max)
        {
            throw new InvalidOperationException(string.Format(L("msg_out_of_range"), fieldName, min, max));
        }

        return value;
    }

    private static AppSettings CloneSettings(AppSettings s)
    {
        return new AppSettings
        {
            LanguageCode = s.LanguageCode,
            LookbackDays = s.LookbackDays,
            MailboxRootName = s.MailboxRootName,
            TargetSubfolderName = s.TargetSubfolderName,
            SecondMailboxRootName = s.SecondMailboxRootName,
            FebanCsvRelativeFolder = s.FebanCsvRelativeFolder,
            FebanCsvFilename = s.FebanCsvFilename,
            RemitBaseRelativeFolder = s.RemitBaseRelativeFolder,
            EnablePass2SubjectBodyFallback = s.EnablePass2SubjectBodyFallback,
            EnablePass3InlineSaveMsg = s.EnablePass3InlineSaveMsg,
            EnableProcessedPdfCache = s.EnableProcessedPdfCache,
            MaxPdfSizeMb = s.MaxPdfSizeMb,
            ScoreMinToSave = s.ScoreMinToSave,
            ScoreMarginToSave = s.ScoreMarginToSave,
            RequireHardSignalOrTwoSignals = s.RequireHardSignalOrTwoSignals,
            RequireCandidateInTotalContext = s.RequireCandidateInTotalContext,
            DoEventsEveryNItems = s.DoEventsEveryNItems,
            InvoiceNumberRegex = s.InvoiceNumberRegex,
            InvoiceBlockWindowLines = s.InvoiceBlockWindowLines,
            SingleInvoiceScoreBonus = s.SingleInvoiceScoreBonus,
            SingleInvoiceMarginBonus = s.SingleInvoiceMarginBonus,
            TotalContextWindowChars = s.TotalContextWindowChars,
            StrongTotalNeighborLines = s.StrongTotalNeighborLines,
            TotalKeywordNeighborLines = s.TotalKeywordNeighborLines,
            AllowLooseDigitsAmountMatch = s.AllowLooseDigitsAmountMatch,
            MinDigitsForLooseMatch = s.MinDigitsForLooseMatch,
            ScoreStrongTotalWeight = s.ScoreStrongTotalWeight,
            ScoreTotalContextWeight = s.ScoreTotalContextWeight,
            ScoreKeywordWindowWeight = s.ScoreKeywordWindowWeight,
            ScoreLineItemPenaltyWeight = s.ScoreLineItemPenaltyWeight,
            PreferHardTotalOverHigherScore = s.PreferHardTotalOverHigherScore,
            BlockInvoiceContextWithoutStrongTotal = s.BlockInvoiceContextWithoutStrongTotal,
            StrongTotalKeywords = s.StrongTotalKeywords.ToArray(),
            TotalKeywords = s.TotalKeywords.ToArray(),
            Mailboxes = s.Mailboxes.Select(m => new MailboxSearchScope
            {
                RootName = m.RootName,
                InboxSubfolders = m.InboxSubfolders.ToList()
            }).ToList()
        };
    }
}
