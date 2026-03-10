using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using RemittanceMatcherApp.Helpers;
using RemittanceMatcherApp.Localization;
using RemittanceMatcherApp.Logging;
using RemittanceMatcherApp.Models;
using RemittanceMatcherApp.Services;

namespace RemittanceMatcherApp;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly OutlookRemittanceProcessor _processor;

    private AppSettings _settings;
    private CancellationTokenSource? _cts;
    private bool _languageSelectorReady;

    public MainWindow()
    {
        InitializeComponent();

        _processor = new OutlookRemittanceProcessor(
            new PathResolverService(),
            new CsvService(),
            new CacheService(),
            new PdfTextExtractorService(),
            new MatchingService());

        _settings = _settingsService.Load();
        _settings.LanguageCode = AppLocalizer.NormalizeLanguageCode(_settings.LanguageCode);

        InitializeLanguageSelector();
        ApplyLanguageUi();
        ApplySettingsToUi(_settings);

        Loaded += MainWindow_Loaded;

        TryLoadAppIcon();
        AppendLog(L("log_app_started"));
        AppendLog(L("log_ready"));
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Max(MinWidth, Math.Min(area.Width * 0.60, area.Width * 0.92));
        Height = Math.Max(MinHeight, Math.Min(area.Height * 0.65, area.Height * 0.92));
    }

    private void InitializeLanguageSelector()
    {
        LanguageComboBox.ItemsSource = AppLocalizer.SupportedLanguages;
        LanguageComboBox.SelectedValue = _settings.LanguageCode;
        _languageSelectorReady = true;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_languageSelectorReady)
        {
            return;
        }

        var code = LanguageComboBox.SelectedValue?.ToString();
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var normalized = AppLocalizer.NormalizeLanguageCode(code);
        if (string.Equals(normalized, _settings.LanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _settings.LanguageCode = normalized;
        _settingsService.Save(_settings);
        ApplyLanguageUi();
        AppendLog($"{L("log_language_changed_prefix")} {normalized}");
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cts is not null)
        {
            return;
        }

        try
        {
            _settings.LookbackDays = ParseInt(LookbackDaysBox.Text, 7, 1, 60, L("label_lookback"));
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{L("msg_bad_settings_prefix")} {ex.Message}", L("msg_error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ToggleRunState(isRunning: true);
        RunProgressBar.Value = 0;
        ProgressPercentText.Text = "0%";
        StatusTextBlock.Text = L("status_starting");
        AppendLogSafe(L("log_search_started"));

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var logger = new UiLogger(AppendLogSafe);
        var progress = new Progress<ProgressUpdate>(u =>
        {
            var total = Math.Max(1, u.Total);
            var pct = Math.Clamp((double)u.Current / total * 100.0, 0.0, 100.0);
            RunProgressBar.Value = pct;
            ProgressPercentText.Text = $"{pct:0}%";
            StatusTextBlock.Text = $"{u.Status} ({pct:0}%)";
        });

        try
        {
            var stats = await StaTaskRunner.RunAsync(() => _processor.Run(_settings, progress, logger, token), token);
            AppendLogSafe(L("log_search_finished"));
            AppendLogSafe(string.Format(
                L("log_process_summary"),
                stats.TotalTransactions,
                stats.MatchedTransactions,
                stats.UnmatchedTransactions));
            StatusTextBlock.Text = L("status_done");
            RunProgressBar.Value = 100;
            ProgressPercentText.Text = "100%";
        }
        catch (OperationCanceledException)
        {
            AppendLogSafe(L("log_canceled"));
            StatusTextBlock.Text = L("status_canceled");
        }
        catch (Exception ex)
        {
            AppendLogSafe($"BŁĄD: {ex.Message}");
            MessageBox.Show(this, ex.Message, L("msg_process_error_title"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = L("status_error");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            ToggleRunState(isRunning: false);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void FetchTransactionsButton_Click(object sender, RoutedEventArgs e)
    {
        AppendLog(L("log_fetch_dev_clicked"));
        MessageBox.Show(this, L("msg_fetch_dev_body"), L("msg_fetch_dev_title"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = FindAboutPath(_settings.LanguageCode);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show(this, L("msg_about_missing"), L("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{L("msg_about_open_fail")} {ex.Message}", L("msg_error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var before = CloneSettings(_settings);

        try
        {
            _settings.LookbackDays = ParseInt(LookbackDaysBox.Text, 7, 1, 60, L("label_lookback"));
        }
        catch
        {
            _settings.LookbackDays = 7;
            LookbackDaysBox.Text = "7";
        }

        var dlg = new SettingsWindow(_settings)
        {
            Owner = this,
            Icon = Icon
        };

        if (dlg.ShowDialog() == true && dlg.ResultSettings is not null)
        {
            _settings = dlg.ResultSettings;
            _settings.LanguageCode = AppLocalizer.NormalizeLanguageCode(_settings.LanguageCode);
            ApplyLanguageUi();
            ApplySettingsToUi(_settings);
            _settingsService.Save(_settings);
            AppendLog(L("log_settings_applied"));
            AppendSettingsChangeLogs(before, _settings);
        }
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.LookbackDays = ParseInt(LookbackDaysBox.Text, _settings.LookbackDays, 1, 60, L("label_lookback"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, L("msg_error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _settingsService.Save(_settings);
            AppendLog(L("log_settings_saved_default"));
            MessageBox.Show(this, L("msg_settings_saved"), L("msg_success"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{L("msg_save_fail_prefix")} {ex.Message}", L("msg_error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var before = CloneSettings(_settings);

        var dlg = new OpenFileDialog
        {
            Title = L("dlg_load_settings"),
            Filter = "Plik JSON (*.json)|*.json|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _settings = _settingsService.LoadFromFile(dlg.FileName);
            _settings.LanguageCode = AppLocalizer.NormalizeLanguageCode(_settings.LanguageCode);
            ApplyLanguageUi();
            ApplySettingsToUi(_settings);
            AppendLog($"{L("log_settings_loaded_prefix")} {dlg.FileName}");
            AppendSettingsChangeLogs(before, _settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{L("msg_load_fail_prefix")} {ex.Message}", L("msg_error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ToggleRunState(bool isRunning)
    {
        StartButton.IsEnabled = !isRunning;
        StopButton.IsEnabled = isRunning;
        SettingsButton.IsEnabled = !isRunning;
        SaveSettingsButton.IsEnabled = !isRunning;
        LoadSettingsButton.IsEnabled = !isRunning;
        FetchTransactionsButton.IsEnabled = !isRunning;
        AboutButton.IsEnabled = !isRunning;
        LanguageComboBox.IsEnabled = !isRunning;
    }

    private void ApplySettingsToUi(AppSettings settings)
    {
        LookbackDaysBox.Text = settings.LookbackDays.ToString();

        _languageSelectorReady = false;
        LanguageComboBox.SelectedValue = settings.LanguageCode;
        _languageSelectorReady = true;
    }

    private void ApplyLanguageUi()
    {
        var header = L("header_main");

        Title = L("app_title");
        CenterHeaderText.Text = header;

        LookbackLabelText.Text = L("label_lookback");
        AboutButton.Content = L("btn_about");

        if (LookbackInfoButton.ToolTip is TextBlock tooltip)
        {
            tooltip.Text = L("tooltip_lookback");
        }

        FetchTransactionsButton.Content = L("btn_fetch_transactions");
        StartButton.Content = L("btn_start");
        StopButton.Content = L("btn_stop");
        SettingsButton.Content = L("btn_settings");
        SaveSettingsButton.Content = L("btn_save_settings");
        LoadSettingsButton.Content = L("btn_load_settings");

        LiveLogLabelText.Text = L("label_live_log");

        if (_cts is null)
        {
            StatusTextBlock.Text = L("status_ready");
            ProgressPercentText.Text = $"{RunProgressBar.Value:0}%";
        }
    }

    private string L(string key) => AppLocalizer.T(_settings.LanguageCode, key);

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

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.AppendText(line + Environment.NewLine);
    }

    private void AppendLogSafe(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogTextBox.AppendText(line + Environment.NewLine);
        });
    }

    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LogTextBox.ScrollToEnd();
    }

    private void TryLoadAppIcon()
    {
        var iconPath = FindIconPath();
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
        {
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            AppIconImage.Source = bitmap;
            Icon = bitmap;
        }
        catch
        {
            // Brak logowania - użytkownik nie potrzebuje logu o ikonie.
        }
    }

    private static string? FindAboutPath(string languageCode)
    {
        var lang = string.IsNullOrWhiteSpace(languageCode) ? "pl" : languageCode.Trim().ToLowerInvariant();

        foreach (var dir in BuildSearchDirs())
        {
            var candidates = new[]
            {
                Path.Combine(dir, $"Opis.{lang}.txt"),
                Path.Combine(dir, "Opis.pl.txt"),
                Path.Combine(dir, "Opis.txt")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string? FindIconPath()
    {
        foreach (var dir in BuildSearchDirs())
        {
            var candidates = new[]
            {
                Path.Combine(dir, "getingeremittanceikomn.ico"),
                Path.Combine(dir, "getingeremittanceikomn.png")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> BuildSearchDirs()
    {
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddWithCommonChildren(string baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                return;
            }

            dirs.Add(baseDir);
            dirs.Add(Path.Combine(baseDir, "support"));
            dirs.Add(Path.Combine(baseDir, "data"));
            dirs.Add(Path.Combine(baseDir, "app"));
            dirs.Add(Path.Combine(baseDir, "app", "support"));
            dirs.Add(Path.Combine(baseDir, "app", "data"));
        }

        AddWithCommonChildren(Environment.CurrentDirectory);
        AddWithCommonChildren(AppContext.BaseDirectory);

        var d = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && d is not null; i++)
        {
            AddWithCommonChildren(d.FullName);
            d = d.Parent;
        }

        return dirs
            .Where(Directory.Exists)
            .ToList();
    }

    private void AppendSettingsChangeLogs(AppSettings before, AppSettings after)
    {
        var changes = new List<string>();

        foreach (var prop in typeof(AppSettings).GetProperties())
        {
            var left = prop.GetValue(before);
            var right = prop.GetValue(after);

            var leftJson = JsonSerializer.Serialize(left);
            var rightJson = JsonSerializer.Serialize(right);

            if (!string.Equals(leftJson, rightJson, StringComparison.Ordinal))
            {
                changes.Add(prop.Name);
            }
        }

        if (changes.Count > 0)
        {
            AppendLog($"{L("log_settings_changed_prefix")} {string.Join(", ", changes)}");
        }
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
