using System.Windows;
using RemittanceMatcherApp.Localization;
using RemittanceMatcherApp.Models;
using RemittanceMatcherApp.Services;

namespace RemittanceMatcherApp;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _working;
    private readonly List<MailboxSearchScope> _mailboxes = [];

    public AppSettings? ResultSettings { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _working = CloneSettings(settings);

        foreach (var mb in _working.Mailboxes)
        {
            _mailboxes.Add(new MailboxSearchScope
            {
                RootName = mb.RootName,
                InboxSubfolders = mb.InboxSubfolders.ToList()
            });
        }

        ApplyLanguageUi();
        ApplyToUi(_working);
        RefreshMailboxRoots();

        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Math.Max(Width, 900), area.Width * 0.9);
        Height = Math.Min(Math.Max(Height, 600), area.Height * 0.9);
    }

    private void ApplyLanguageUi()
    {
        Title = L("settings_title");
        CenterHeaderText.Text = L("header_main");
        PathsHeaderText.Text = L("settings_paths_header");
        FebanFolderLabel.Text = L("settings_label_feban_folder");
        FebanFileLabel.Text = L("settings_label_feban_file");
        RemitBaseLabel.Text = L("settings_label_remit_folder");
        FebanFolderInfoText.Text = L("settings_tt_feban_folder");
        FebanFileInfoText.Text = L("settings_tt_feban_file");
        RemitBaseInfoText.Text = L("settings_tt_remit_folder");

        AdvancedSettingsButton.Content = L("settings_btn_advanced");
        ApplyButton.Content = L("settings_btn_apply");
        CancelButton.Content = L("settings_btn_cancel");
        AddRootButton.Content = L("settings_btn_add_root");
        RemoveRootButton.Content = L("settings_btn_remove_root");
        AddSubfolderButton.Content = L("settings_btn_add_subfolder");
        RemoveSubfolderButton.Content = L("settings_btn_remove_subfolder");
        RootsGroupBox.Header = L("settings_group_roots");
        SubfoldersGroupBox.Header = L("settings_group_subfolders");

        EnablePass2Box.Content = L("settings_mode_pass2");
        EnablePass3Box.Content = L("settings_mode_pass3");
        EnableCacheBox.Content = L("settings_mode_cache");
        ModeHeaderText.Text = L("settings_mode_header");
        Pass2InfoText.Text = L("settings_tt_pass2");
        Pass3InfoText.Text = L("settings_tt_pass3");
        CacheInfoText.Text = L("settings_tt_cache");
        MaxPdfLabel.Text = L("settings_label_max_pdf");
        MaxPdfInfoText.Text = L("settings_tt_max_pdf");
        UiRefreshLabel.Text = L("settings_label_ui_refresh");
        UiRefreshInfoText.Text = L("settings_tt_ui_refresh");

        MailboxesHeaderText.Text = L("settings_mailbox_header");
        MailboxesInfoText.Text = L("settings_tt_mailboxes");
        RootNameBox.ToolTip = L("settings_root_placeholder");
        SubfolderNameBox.ToolTip = L("settings_subfolder_placeholder");
    }

    private void ApplyToUi(AppSettings s)
    {
        FebanFolderBox.Text = s.FebanCsvRelativeFolder;
        FebanFileBox.Text = s.FebanCsvFilename;
        RemitBaseFolderBox.Text = s.RemitBaseRelativeFolder;

        EnablePass2Box.IsChecked = s.EnablePass2SubjectBodyFallback;
        EnablePass3Box.IsChecked = s.EnablePass3InlineSaveMsg;
        EnableCacheBox.IsChecked = s.EnableProcessedPdfCache;

        MaxPdfSizeBox.Text = s.MaxPdfSizeMb.ToString();
        DoEventsEveryBox.Text = s.DoEventsEveryNItems.ToString();
    }

    private void RefreshMailboxRoots()
    {
        MailboxRootsListBox.ItemsSource = null;
        MailboxRootsListBox.ItemsSource = _mailboxes;
        MailboxRootsListBox.DisplayMemberPath = nameof(MailboxSearchScope.RootName);

        if (_mailboxes.Count > 0 && MailboxRootsListBox.SelectedIndex < 0)
        {
            MailboxRootsListBox.SelectedIndex = 0;
        }

        RefreshSubfolders();
    }

    private void RefreshSubfolders()
    {
        SubfoldersListBox.ItemsSource = null;

        var selected = MailboxRootsListBox.SelectedItem as MailboxSearchScope;
        if (selected is null)
        {
            return;
        }

        SubfoldersListBox.ItemsSource = selected.InboxSubfolders.ToList();
    }

    private void AddRootButton_Click(object sender, RoutedEventArgs e)
    {
        var root = RootNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(root))
        {
            MessageBox.Show(this, L("settings_msg_enter_root"), L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_mailboxes.Any(x => string.Equals(x.RootName, root, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, L("settings_msg_root_exists"), L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _mailboxes.Add(new MailboxSearchScope { RootName = root });
        RootNameBox.Clear();
        RefreshMailboxRoots();
        MailboxRootsListBox.SelectedIndex = _mailboxes.Count - 1;
    }

    private void RemoveRootButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = MailboxRootsListBox.SelectedItem as MailboxSearchScope;
        if (selected is null)
        {
            return;
        }

        _mailboxes.Remove(selected);
        RefreshMailboxRoots();
    }

    private void AddSubfolderButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = MailboxRootsListBox.SelectedItem as MailboxSearchScope;
        if (selected is null)
        {
            MessageBox.Show(this, L("settings_msg_select_root"), L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sub = SubfolderNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(sub))
        {
            MessageBox.Show(this, L("settings_msg_enter_subfolder"), L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selected.InboxSubfolders.Any(x => string.Equals(x, sub, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, L("settings_msg_subfolder_exists"), L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        selected.InboxSubfolders.Add(sub);
        SubfolderNameBox.Clear();
        RefreshSubfolders();
    }

    private void RemoveSubfolderButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedRoot = MailboxRootsListBox.SelectedItem as MailboxSearchScope;
        var selectedSubfolder = SubfoldersListBox.SelectedItem as string;
        if (selectedRoot is null || string.IsNullOrWhiteSpace(selectedSubfolder))
        {
            return;
        }

        selectedRoot.InboxSubfolders.RemoveAll(x => string.Equals(x, selectedSubfolder, StringComparison.OrdinalIgnoreCase));
        RefreshSubfolders();
    }

    private void MailboxRootsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshSubfolders();
    }

    private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        AppSettings baseSettings;
        try
        {
            baseSettings = BuildSettingsFromUi(requireMailbox: false);
        }
        catch
        {
            baseSettings = CloneSettings(_working);
        }

        var dlg = new AdvancedSettingsWindow(baseSettings)
        {
            Owner = this,
            Icon = Icon
        };

        if (dlg.ShowDialog() == true && dlg.ResultSettings is not null)
        {
            _working = CloneSettings(dlg.ResultSettings);
            ApplyLanguageUi();
            ApplyToUi(_working);
            _settingsService.Save(_working);
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = BuildSettingsFromUi(requireMailbox: true);
            _working = CloneSettings(result);
            ResultSettings = result;
            _settingsService.Save(result);
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

    private AppSettings BuildSettingsFromUi(bool requireMailbox)
    {
        if (requireMailbox && _mailboxes.Count == 0)
        {
            throw new InvalidOperationException(L("settings_msg_need_root"));
        }

        var maxPdf = ParseInt(MaxPdfSizeBox.Text, 15, 1, 200, L("settings_label_max_pdf"));
        var doEvents = ParseInt(DoEventsEveryBox.Text, 20, 1, 500, L("settings_label_ui_refresh"));

        var s = CloneSettings(_working);
        s.FebanCsvRelativeFolder = FebanFolderBox.Text.Trim();
        s.FebanCsvFilename = FebanFileBox.Text.Trim();
        s.RemitBaseRelativeFolder = RemitBaseFolderBox.Text.Trim();

        s.EnablePass2SubjectBodyFallback = EnablePass2Box.IsChecked == true;
        s.EnablePass3InlineSaveMsg = EnablePass3Box.IsChecked == true;
        s.EnableProcessedPdfCache = EnableCacheBox.IsChecked == true;

        s.MaxPdfSizeMb = maxPdf;
        s.DoEventsEveryNItems = doEvents;

        s.Mailboxes = _mailboxes.Select(x => new MailboxSearchScope
        {
            RootName = x.RootName.Trim(),
            InboxSubfolders = x.InboxSubfolders
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        }).ToList();

        if (!requireMailbox && s.Mailboxes.Count == 0)
        {
            s.Mailboxes = _working.Mailboxes.Select(x => new MailboxSearchScope
            {
                RootName = x.RootName,
                InboxSubfolders = x.InboxSubfolders.ToList()
            }).ToList();
        }

        if (s.Mailboxes.Count > 0)
        {
            s.MailboxRootName = s.Mailboxes[0].RootName;
            s.TargetSubfolderName = s.Mailboxes[0].InboxSubfolders.FirstOrDefault() ?? string.Empty;
            s.SecondMailboxRootName = s.Mailboxes.Count > 1 ? s.Mailboxes[1].RootName : string.Empty;
        }

        return s;
    }

    private string L(string key) => AppLocalizer.T(_working.LanguageCode, key);

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
}
