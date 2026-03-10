using System.Text.Json;
using RemittanceMatcherApp.Models;

namespace RemittanceMatcherApp.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "RemittanceMatcherApp");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        var targetPath = GetDefaultSettingsPath();
        if (File.Exists(targetPath))
        {
            return LoadFromFile(targetPath);
        }

        var defaults = LoadDefaultTemplateFromAppFolder() ?? Normalize(new AppSettings());
        SaveToFile(defaults, targetPath);
        return defaults;
    }

    public AppSettings LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return Normalize(new AppSettings());
        }

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return Normalize(data);
        }
        catch
        {
            return Normalize(new AppSettings());
        }
    }

    public void Save(AppSettings settings)
    {
        SaveToFile(settings, GetDefaultSettingsPath());
    }

    public void SaveToFile(AppSettings settings, string path)
    {
        var normalized = Normalize(settings);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, json);
    }

    private AppSettings? LoadDefaultTemplateFromAppFolder()
    {
        foreach (var dir in BuildSearchDirs())
        {
            var candidate = Path.Combine(dir, "settings.default.json");
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                return LoadFromFile(candidate);
            }
            catch
            {
                // Ignoruj i sprawdz kolejna lokalizacje.
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

    private static AppSettings Normalize(AppSettings s)
    {
        s.LanguageCode = NormalizeLanguageCode(s.LanguageCode);
        s.LookbackDays = Clamp(s.LookbackDays, 1, 60, 7);
        s.MaxPdfSizeMb = Clamp(s.MaxPdfSizeMb, 1, 200, 15);
        s.DoEventsEveryNItems = Clamp(s.DoEventsEveryNItems, 1, 500, 20);
        s.ScoreMinToSave = Clamp(s.ScoreMinToSave, 1, 1000, 120);
        s.ScoreMarginToSave = Clamp(s.ScoreMarginToSave, 0, 1000, 45);
        s.InvoiceBlockWindowLines = Clamp(s.InvoiceBlockWindowLines, 0, 20, 1);
        s.SingleInvoiceScoreBonus = Clamp(s.SingleInvoiceScoreBonus, 0, 500, 20);
        s.SingleInvoiceMarginBonus = Clamp(s.SingleInvoiceMarginBonus, 0, 500, 10);
        s.TotalContextWindowChars = Clamp(s.TotalContextWindowChars, 30, 5000, 280);
        s.StrongTotalNeighborLines = Clamp(s.StrongTotalNeighborLines, 0, 10, 1);
        s.TotalKeywordNeighborLines = Clamp(s.TotalKeywordNeighborLines, 0, 10, 1);
        s.MinDigitsForLooseMatch = Clamp(s.MinDigitsForLooseMatch, 3, 12, 5);
        s.ScoreStrongTotalWeight = Clamp(s.ScoreStrongTotalWeight, 0, 500, 130);
        s.ScoreTotalContextWeight = Clamp(s.ScoreTotalContextWeight, 0, 500, 85);
        s.ScoreKeywordWindowWeight = Clamp(s.ScoreKeywordWindowWeight, 0, 500, 60);
        s.ScoreLineItemPenaltyWeight = Clamp(s.ScoreLineItemPenaltyWeight, -500, 0, -90);

        s.FebanCsvFilename = string.IsNullOrWhiteSpace(s.FebanCsvFilename) ? "transactions.csv" : s.FebanCsvFilename.Trim();

        if (s.Mailboxes is null)
        {
            s.Mailboxes = [];
        }

        // Backward compatibility: stare pola -> lista skrzynek
        if (s.Mailboxes.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(s.MailboxRootName))
            {
                var scope = new MailboxSearchScope { RootName = s.MailboxRootName.Trim() };
                if (!string.IsNullOrWhiteSpace(s.TargetSubfolderName))
                {
                    scope.InboxSubfolders.Add(s.TargetSubfolderName.Trim());
                }

                s.Mailboxes.Add(scope);
            }

            if (!string.IsNullOrWhiteSpace(s.SecondMailboxRootName))
            {
                s.Mailboxes.Add(new MailboxSearchScope { RootName = s.SecondMailboxRootName.Trim() });
            }
        }

        s.Mailboxes = s.Mailboxes
            .Where(m => !string.IsNullOrWhiteSpace(m.RootName))
            .Select(m => new MailboxSearchScope
            {
                RootName = m.RootName.Trim(),
                InboxSubfolders = m.InboxSubfolders
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .ToList();

        if (s.Mailboxes.Count == 0)
        {
            s.Mailboxes.Add(new MailboxSearchScope { RootName = "ukremittance", InboxSubfolders = ["Remittance advices"] });
        }

        return s;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        var code = (languageCode ?? "pl").Trim().ToLowerInvariant();
        return code is "pl" or "en" or "es" or "fr" or "de" ? code : "pl";
    }

    private static int Clamp(int value, int min, int max, int fallback)
    {
        if (value == 0)
        {
            return fallback;
        }

        return Math.Max(min, Math.Min(max, value));
    }
}
