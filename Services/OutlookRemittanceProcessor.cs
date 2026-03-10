using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using RemittanceMatcherApp.Logging;
using RemittanceMatcherApp.Models;

namespace RemittanceMatcherApp.Services;

public sealed class OutlookRemittanceProcessor
{
    private const string BuildMarker = "OUTLOOK_DIAG_2026-03-10";

    private readonly PathResolverService _pathResolver;
    private readonly CsvService _csvService;
    private readonly CacheService _cacheService;
    private readonly PdfTextExtractorService _pdfExtractor;
    private readonly MatchingService _matchingService;

    public OutlookRemittanceProcessor(
        PathResolverService pathResolver,
        CsvService csvService,
        CacheService cacheService,
        PdfTextExtractorService pdfExtractor,
        MatchingService matchingService)
    {
        _pathResolver = pathResolver;
        _csvService = csvService;
        _cacheService = cacheService;
        _pdfExtractor = pdfExtractor;
        _matchingService = matchingService;
    }

    public RunStats Run(AppSettings settings, IProgress<ProgressUpdate>? progress, IAppLogger logger, CancellationToken cancellationToken)
    {
        var stats = new RunStats();
        var processStart = DateTime.Now;

        logger.Info($"Rozpoczynam proces. Wersja diagnostyczna: {BuildMarker}");

        var csvPath = _pathResolver.ResolveNetworkPathToFile(settings.FebanCsvRelativeFolder, settings.FebanCsvFilename);
        if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        {
            throw new InvalidOperationException("Nie moge znalezc transactions.csv.");
        }

        var remitBasePath = _pathResolver.ResolveNetworkPathToFolder(settings.RemitBaseRelativeFolder);
        if (string.IsNullOrWhiteSpace(remitBasePath))
        {
            throw new InvalidOperationException("Nie moge znalezc katalogu bazowego remitek.");
        }

        var feban = _csvService.LoadFebanCsvWithDate(csvPath);
        if (feban.Count == 0)
        {
            throw new InvalidOperationException("Brak danych w CSV / nie wczytano transakcji.");
        }

        stats.TotalTransactions = feban.Count;
        logger.Info($"Wczytano transakcje FEBAN: {feban.Count}");

        var cachePath = _cacheService.BuildCachePathFromCsvPath(csvPath);
        var cache = settings.EnableProcessedPdfCache
            ? _cacheService.Load(cachePath, settings.LookbackDays)
            : new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        logger.Info($"Cache PDF: {cache.Count} rekordow ({cachePath})");

        object? outlookApp = null;
        object? ns = null;

        try
        {
            outlookApp = GetOutlookApplication();
            dynamic app = outlookApp;
            ns = app.Session;

            var targets = ResolveSearchFolders(ns, settings, logger);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("Nie znaleziono zadnego poprawnego folderu do skanowania. Sprawdz ustawienia skrzynek.");
            }

            var dtFrom = DateTime.Today.AddDays(-settings.LookbackDays);
            var restrictFilter = BuildReceivedTimeRestrict(dtFrom);

            var mailsPdfNotSaved = new List<object>();
            var mailsNoPdfCandidates = new List<object>();
            var savedPdfKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var savedAnyPdfInMail = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var savedMsgMail = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var totalItems = targets.Sum(t => CountMailItems(t.Folder, restrictFilter));
            var progressCurrent = 0;
            progress?.Report(new ProgressUpdate { Current = 0, Total = Math.Max(1, totalItems), Status = "Skanowanie maili..." });

            var folderIndex = 0;
            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (feban.Count == 0)
                {
                    break;
                }

                folderIndex++;
                logger.Info($"Skan folderu: {target.Label}");

                ProcessFolderItems(
                    target.Folder,
                    target.Label,
                    $"F{folderIndex}",
                    restrictFilter,
                    feban,
                    remitBasePath,
                    settings,
                    stats,
                    mailsPdfNotSaved,
                    mailsNoPdfCandidates,
                    savedPdfKeys,
                    savedAnyPdfInMail,
                    cache,
                    ref progressCurrent,
                    totalItems,
                    progress,
                    logger,
                    cancellationToken);
            }

            if (settings.EnablePass2SubjectBodyFallback && feban.Count > 0)
            {
                Pass2SubjectBodyFallbackSavePdf(
                    mailsPdfNotSaved,
                    feban,
                    savedPdfKeys,
                    savedAnyPdfInMail,
                    remitBasePath,
                    settings,
                    stats,
                    cache,
                    logger,
                    cancellationToken);
            }

            if (settings.EnablePass3InlineSaveMsg && feban.Count > 0)
            {
                Pass3InlineBodySaveMsg(
                    mailsNoPdfCandidates,
                    feban,
                    savedMsgMail,
                    remitBasePath,
                    settings,
                    stats,
                    logger,
                    cancellationToken);
            }

            if (settings.EnableProcessedPdfCache)
            {
                _cacheService.Save(cachePath, cache, settings.LookbackDays);
            }

            var elapsed = DateTime.Now - processStart;
            stats.UnmatchedTransactions = feban.Count;
            stats.MatchedTransactions = Math.Max(0, stats.TotalTransactions - stats.UnmatchedTransactions);
            logger.Info($"Koniec. Zapisane: {stats.Saved}, nieodnalezione: {feban.Count}, pominiete z cache: {stats.CacheSkips}, czas: {elapsed.TotalMinutes:F2} min");
            progress?.Report(new ProgressUpdate { Current = Math.Max(1, totalItems), Total = Math.Max(1, totalItems), Status = "Zakonczono" });

            return stats;
        }
        catch (OperationCanceledException)
        {
            logger.Warn("Proces zatrzymany przez uzytkownika.");
            throw;
        }
        catch (Exception ex)
        {
            stats.RuntimeErrors++;
            logger.Error(ex.Message);
            throw;
        }
        finally
        {
            if (ns is not null)
            {
                Marshal.FinalReleaseComObject(ns);
            }

            if (outlookApp is not null)
            {
                Marshal.FinalReleaseComObject(outlookApp);
            }
        }
    }

    private void ProcessFolderItems(
        dynamic srcFolder,
        string folderLabel,
        string tmpTag,
        string restrictFilter,
        Dictionary<string, FebanRecord> feban,
        string remitBasePath,
        AppSettings settings,
        RunStats stats,
        List<object> mailsPdfNotSaved,
        List<object> mailsNoPdfCandidates,
        HashSet<string> savedPdfKeys,
        Dictionary<string, bool> savedAnyPdfInMail,
        Dictionary<string, CacheEntry> cache,
        ref int progressCurrent,
        int progressTotal,
        IProgress<ProgressUpdate>? progress,
        IAppLogger logger,
        CancellationToken cancellationToken)
    {
        if (feban.Count == 0)
        {
            return;
        }

        dynamic itemsAll = srcFolder.Items;
        dynamic items = itemsAll.Restrict(restrictFilter);
        items.Sort("[ReceivedTime]", true);

        var itemCount = (int)items.Count;
        logger.Info($"{folderLabel}: elementow po filtrze czasu: {itemCount}");

        for (var i = itemCount; i >= 1; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (feban.Count == 0)
            {
                break;
            }

            dynamic it = items[i];
            if (!IsMailItem(it))
            {
                progressCurrent++;
                continue;
            }

            dynamic mi = it;
            stats.Checked++;
            progressCurrent++;
            progress?.Report(new ProgressUpdate
            {
                Current = Math.Min(progressCurrent, Math.Max(1, progressTotal)),
                Total = Math.Max(1, progressTotal),
                Status = $"{folderLabel}: {stats.Checked} maili sprawdzonych"
            });

            var hasPdf = false;
            var mailSavedAnyPdf = false;

            var attCount = (int)mi.Attachments.Count;
            for (var a = 1; a <= attCount; a++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic att = mi.Attachments.Item(a);
                var fn = ((string)att.FileName).Trim().ToLowerInvariant();
                if (!fn.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                hasPdf = true;
                stats.PdfFound++;

                var pdfKey = $"{(string)mi.EntryID}|{(string)att.FileName}";
                if (savedPdfKeys.Contains(pdfKey))
                {
                    continue;
                }

                var fingerprint = BuildPdfFingerprint(mi, att);
                if (settings.EnableProcessedPdfCache && _cacheService.ShouldSkip(cache, fingerprint, (DateTime)mi.ReceivedTime, settings.LookbackDays))
                {
                    stats.CacheSkips++;
                    continue;
                }

                var tmpPdf = Path.Combine(Path.GetTempPath(), $"RA_{DateTime.Now:yyyyMMdd_HHmmss}_{tmpTag}_{i}_{a}.pdf");

                try
                {
                    att.SaveAsFile(tmpPdf);
                }
                catch
                {
                    stats.PdfSaveFail++;
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusRejectSoft, "PDF_SAVE_FAIL");
                    }

                    continue;
                }

                var maxPdfBytes = settings.MaxPdfSizeMb * 1024L * 1024L;
                if (new FileInfo(tmpPdf).Length > maxPdfBytes)
                {
                    stats.Rejected++;
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusRejectSoft, "PDF_TOO_LARGE");
                    }

                    SafeDeleteFile(tmpPdf);
                    continue;
                }

                var pdfText = _pdfExtractor.ExtractPdfTextViaWord(tmpPdf);
                if (pdfText.Length == 0)
                {
                    stats.PdfOpenFail++;
                    stats.Rejected++;
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusRejectSoft, "PDF_TEXT_EMPTY");
                    }

                    SafeDeleteFile(tmpPdf);
                    continue;
                }

                var ovf = stats.OverflowSkips;
                var match = _matchingService.FindBestValidatedMatch(pdfText, feban, settings, ref ovf);
                stats.OverflowSkips = ovf;

                if (match is not null && !string.IsNullOrWhiteSpace(match.Key) && !mailSavedAnyPdf)
                {
                    SaveMatchedPdf(tmpPdf, match.Key, feban, remitBasePath);
                    stats.Saved++;
                    savedPdfKeys.Add(pdfKey);
                    savedAnyPdfInMail[(string)mi.EntryID] = true;
                    mailSavedAnyPdf = true;
                    feban.Remove(match.Key);

                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusMatched, match.Reason);
                    }

                    logger.Info($"PDF zapisany: {(string)att.FileName} => {match.Key}");
                }
                else
                {
                    stats.Rejected++;
                    var reason = match?.Reason ?? "NO_CANDIDATE";
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, _cacheService.MapStatusFromReason(reason), reason);
                    }
                }

                SafeDeleteFile(tmpPdf);
            }

            if (hasPdf)
            {
                if (!mailSavedAnyPdf)
                {
                    mailsPdfNotSaved.Add(mi);
                    if (!savedAnyPdfInMail.ContainsKey((string)mi.EntryID))
                    {
                        savedAnyPdfInMail[(string)mi.EntryID] = false;
                    }
                }
            }
            else
            {
                mailsNoPdfCandidates.Add(mi);
            }

            if (settings.DoEventsEveryNItems > 0 && stats.Checked % settings.DoEventsEveryNItems == 0)
            {
                Thread.Sleep(1);
            }
        }
    }

    private void Pass2SubjectBodyFallbackSavePdf(
        List<object> mailsPdfNotSaved,
        Dictionary<string, FebanRecord> feban,
        HashSet<string> savedPdfKeys,
        Dictionary<string, bool> savedAnyPdfInMail,
        string remitBasePath,
        AppSettings settings,
        RunStats stats,
        Dictionary<string, CacheEntry> cache,
        IAppLogger logger,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < mailsPdfNotSaved.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (feban.Count == 0)
            {
                return;
            }

            dynamic mi = mailsPdfNotSaved[i];
            var entryId = (string)mi.EntryID;
            if (savedAnyPdfInMail.TryGetValue(entryId, out var alreadySaved) && alreadySaved)
            {
                continue;
            }

            var mailText = Nz((string?)mi.Subject) + "\r\n" + GetMailTextPlain(mi);

            var attCount = (int)mi.Attachments.Count;
            for (var a = 1; a <= attCount; a++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic att = mi.Attachments.Item(a);
                var fn = ((string)att.FileName).Trim().ToLowerInvariant();
                if (!fn.Contains(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var pdfKey = $"{entryId}|{(string)att.FileName}";
                if (savedPdfKeys.Contains(pdfKey))
                {
                    continue;
                }

                var fingerprint = BuildPdfFingerprint(mi, att);
                if (settings.EnableProcessedPdfCache && _cacheService.ShouldSkip(cache, fingerprint, (DateTime)mi.ReceivedTime, settings.LookbackDays))
                {
                    stats.CacheSkips++;
                    continue;
                }

                var tmpPdf = Path.Combine(Path.GetTempPath(), $"RA_FALLBACK_{DateTime.Now:yyyyMMdd_HHmmss}_{i}_{a}.pdf");

                try
                {
                    att.SaveAsFile(tmpPdf);
                }
                catch
                {
                    stats.PdfSaveFail++;
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusRejectSoft, "PDF_SAVE_FAIL_P2");
                    }

                    continue;
                }

                var pdfText = _pdfExtractor.ExtractPdfTextViaWord(tmpPdf);
                if (pdfText.Length == 0)
                {
                    stats.PdfOpenFail++;
                    stats.Rejected++;
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusRejectSoft, "PDF_TEXT_EMPTY_P2");
                    }

                    SafeDeleteFile(tmpPdf);
                    continue;
                }

                var combined = mailText + "\r\n" + pdfText;
                var ovf = stats.OverflowSkips;
                var match = _matchingService.FindBestValidatedMatch(combined, feban, settings, ref ovf);
                stats.OverflowSkips = ovf;

                if (match is null || string.IsNullOrWhiteSpace(match.Key))
                {
                    stats.Rejected++;
                    var reason = match?.Reason ?? "NO_CANDIDATE_P2";
                    if (settings.EnableProcessedPdfCache)
                    {
                        _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, _cacheService.MapStatusFromReason(reason), reason);
                    }

                    SafeDeleteFile(tmpPdf);
                    continue;
                }

                SaveMatchedPdf(tmpPdf, match.Key, feban, remitBasePath);
                stats.Saved++;
                savedPdfKeys.Add(pdfKey);
                savedAnyPdfInMail[entryId] = true;
                feban.Remove(match.Key);
                if (settings.EnableProcessedPdfCache)
                {
                    _cacheService.Update(cache, fingerprint, (DateTime)mi.ReceivedTime, CacheService.StatusMatched, match.Reason);
                }

                SafeDeleteFile(tmpPdf);
                logger.Info($"Pass2 zapisany: {(string)att.FileName} => {match.Key}");
                break;
            }
        }
    }

    private void Pass3InlineBodySaveMsg(
        List<object> mailsNoPdfCandidates,
        Dictionary<string, FebanRecord> feban,
        HashSet<string> savedMsgMail,
        string remitBasePath,
        AppSettings settings,
        RunStats stats,
        IAppLogger logger,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < mailsNoPdfCandidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (feban.Count == 0)
            {
                return;
            }

            dynamic mi = mailsNoPdfCandidates[i];
            var entryId = (string)mi.EntryID;
            if (savedMsgMail.Contains(entryId))
            {
                continue;
            }

            var subj = Nz((string?)mi.Subject);
            var bodyText = GetMailTextPlain(mi);

            if (!_matchingService.LooksLikeInlineRemittance(bodyText, subj))
            {
                continue;
            }

            var text = subj + "\r\n" + bodyText;
            var ovf = stats.OverflowSkips;
            var match = _matchingService.FindBestValidatedMatch(text, feban, settings, ref ovf);
            stats.OverflowSkips = ovf;

            if (match is null || string.IsNullOrWhiteSpace(match.Key))
            {
                continue;
            }

            var rec = feban[match.Key];
            var outName = $"{rec.DisplayAmount} {_pathResolver.SanitizeFileName(rec.Partner)}.msg";
            var saveFolder = _pathResolver.GetSaveFolderForDate(remitBasePath, rec.StatementDate);
            _pathResolver.EnsureFolderTreeExists(saveFolder);

            var full = _pathResolver.GetUniquePath(Path.Combine(saveFolder, outName));
            mi.SaveAs(full, 3);

            stats.Saved++;
            savedMsgMail.Add(entryId);
            feban.Remove(match.Key);
            logger.Info($"Pass3 zapisany MSG => {match.Key}");
        }
    }

    private void SaveMatchedPdf(string tmpPdf, string key, Dictionary<string, FebanRecord> feban, string remitBasePath)
    {
        var rec = feban[key];
        var outName = $"{rec.DisplayAmount} {_pathResolver.SanitizeFileName(rec.Partner)}.pdf";
        var saveFolder = _pathResolver.GetSaveFolderForDate(remitBasePath, rec.StatementDate);
        _pathResolver.EnsureFolderTreeExists(saveFolder);

        var fullPath = _pathResolver.GetUniquePath(Path.Combine(saveFolder, outName));
        File.Copy(tmpPdf, fullPath, overwrite: false);
    }

    private static object GetOutlookApplication()
    {
        var t = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook.Application COM jest niedostepny.");
        return Activator.CreateInstance(t) ?? throw new InvalidOperationException("Nie mozna uruchomic Outlook.Application.");
    }

    private sealed class SearchFolderTarget
    {
        public required string Label { get; init; }
        public required object Folder { get; init; }
    }

    private static List<SearchFolderTarget> ResolveSearchFolders(dynamic ns, AppSettings settings, IAppLogger logger)
    {
        var output = new List<SearchFolderTarget>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mailbox in settings.Mailboxes)
        {
            if (string.IsNullOrWhiteSpace(mailbox.RootName))
            {
                continue;
            }

            var inbox = GetInboxFolderOnly(ns, mailbox.RootName);
            if (inbox is null)
            {
                logger.Warn($"Nie znaleziono skrzynki lub Inbox: {mailbox.RootName}");
                continue;
            }

            if (mailbox.InboxSubfolders.Count == 0)
            {
                var label = $"{mailbox.RootName}\\Inbox";
                if (dedupe.Add(label))
                {
                    output.Add(new SearchFolderTarget { Label = label, Folder = inbox });
                }

                continue;
            }

            foreach (var subfolderPath in mailbox.InboxSubfolders)
            {
                var target = GetInboxSubfolderByPath(inbox, subfolderPath);
                if (target is null)
                {
                    logger.Warn($"Nie znaleziono podfolderu: {mailbox.RootName}\\Inbox\\{subfolderPath}");
                    continue;
                }

                var label = $"{mailbox.RootName}\\Inbox\\{subfolderPath}";
                if (dedupe.Add(label))
                {
                    output.Add(new SearchFolderTarget { Label = label, Folder = target });
                }
            }
        }

        return output;
    }

    private static dynamic? GetInboxFolderOnly(dynamic ns, string mailboxRoot)
    {
        try
        {
            dynamic root = ns.Folders[mailboxRoot];

            // Prefer domyslny Inbox dla store (dziala niezaleznie od jezyka Outlooka).
            try
            {
                const int olFolderInbox = 6;
                return root.Store.GetDefaultFolder(olFolderInbox);
            }
            catch
            {
                return root.Folders["Inbox"];
            }
        }
        catch
        {
            return null;
        }
    }

    private static dynamic? GetInboxSubfolderByPath(dynamic inboxFolder, string subfolderPath)
    {
        try
        {
            var parts = subfolderPath
                .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            dynamic current = inboxFolder;
            foreach (var part in parts)
            {
                current = current.Folders[part];
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    private static int CountMailItems(dynamic folder, string restrictFilter)
    {
        try
        {
            dynamic items = folder.Items.Restrict(restrictFilter);
            return (int)items.Count;
        }
        catch
        {
            return 0;
        }
    }

    private static string BuildReceivedTimeRestrict(DateTime dtFrom)
    {
        // Outlook Restrict oczekuje formatu US daty/czasu niezaleznie od lokalizacji systemu.
        var filterDate = dtFrom.ToString("MM/dd/yyyy hh:mm tt", CultureInfo.GetCultureInfo("en-US"));
        return $"[ReceivedTime] >= '{filterDate}'";
    }

    private static string BuildPdfFingerprint(dynamic mailItem, dynamic att)
    {
        string storeId;
        try
        {
            storeId = (string)mailItem.Parent.StoreID;
        }
        catch
        {
            storeId = string.Empty;
        }

        var fileName = (string)att.FileName;
        var size = (int)att.Size;
        var received = (DateTime)mailItem.ReceivedTime;
        return $"{storeId}|{(string)mailItem.EntryID}|{fileName}|{size}|{received:yyyyMMddHHmmss}".ToLowerInvariant();
    }

    private static bool IsMailItem(dynamic item)
    {
        try
        {
            const int olMail = 43;
            return (int)item.Class == olMail;
        }
        catch
        {
        }

        try
        {
            var messageClass = (string?)item.MessageClass;
            return !string.IsNullOrWhiteSpace(messageClass) &&
                   messageClass.StartsWith("IPM.Note", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string GetMailTextPlain(dynamic mailItem)
    {
        try
        {
            var html = Nz((string?)mailItem.HTMLBody);
            if (!string.IsNullOrWhiteSpace(html))
            {
                return HtmlToText(html);
            }
        }
        catch
        {
        }

        try
        {
            return Nz((string?)mailItem.Body);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string HtmlToText(string html)
    {
        var s = html;
        s = Regex.Replace(s, "<br\\s*/?>", "\r\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "</(tr|p|div|li)>", "\r\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "</td>", " ", RegexOptions.IgnoreCase);
        s = s.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase).Replace('\u00A0', ' ');
        s = Regex.Replace(s, "<[^>]+>", " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "[ \\t]{2,}", " ");
        s = Regex.Replace(s, "(\\r\\n){3,}", "\r\n\r\n");
        return s.Trim();
    }

    private static string Nz(string? value) => value ?? string.Empty;

    private static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
        }
    }
}
