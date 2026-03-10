using RemittanceMatcherApp.Models;

namespace RemittanceMatcherApp.Services;

public sealed class CacheService
{
    public const string StatusMatched = "MATCHED";
    public const string StatusRejectHard = "REJECT_HARD";
    public const string StatusRejectSoft = "REJECT_SOFT";

    public string BuildCachePathFromCsvPath(string csvPath)
    {
        var dir = Path.GetDirectoryName(csvPath) ?? string.Empty;
        return Path.Combine(dir, "processed_pdf_cache.csv");
    }

    public Dictionary<string, CacheEntry> Load(string cachePath, int keepDays)
    {
        var cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(cachePath) || !File.Exists(cachePath))
        {
            return cache;
        }

        using var sr = new StreamReader(cachePath);
        if (!sr.EndOfStream)
        {
            _ = sr.ReadLine();
        }

        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(';');
            if (parts.Length < 5)
            {
                continue;
            }

            var fp = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(fp))
            {
                continue;
            }

            var received = ParseDate(parts[1], DateTime.Today);
            if ((DateTime.Today - received.Date).TotalDays > keepDays)
            {
                continue;
            }

            var lastChecked = ParseDate(parts[2], received);
            var status = parts[3].Trim();
            var reason = parts[4];

            cache[fp] = new CacheEntry
            {
                Fingerprint = fp,
                ReceivedTime = received,
                LastChecked = lastChecked,
                Status = status,
                Reason = reason
            };
        }

        return cache;
    }

    public void Save(string cachePath, Dictionary<string, CacheEntry> cache, int keepDays)
    {
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cachePath) ?? string.Empty);

        using var sw = new StreamWriter(cachePath, false);
        sw.WriteLine("fingerprint;received_time;last_checked;status;reason");

        foreach (var kv in cache)
        {
            var e = kv.Value;
            if ((DateTime.Today - e.ReceivedTime.Date).TotalDays > keepDays)
            {
                continue;
            }

            sw.WriteLine($"{CsvSafe(e.Fingerprint)};{e.ReceivedTime:yyyy-MM-dd HH:mm:ss};{e.LastChecked:yyyy-MM-dd HH:mm:ss};{CsvSafe(e.Status)};{CsvSafe(e.Reason)}");
        }
    }

    public bool ShouldSkip(Dictionary<string, CacheEntry> cache, string fingerprint, DateTime receivedTime, int lookbackDays)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return false;
        }

        if (!cache.TryGetValue(fingerprint, out var existing))
        {
            return false;
        }

        if ((DateTime.Today - existing.ReceivedTime.Date).TotalDays > lookbackDays)
        {
            return false;
        }

        return string.Equals(existing.Status, StatusMatched, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(existing.Status, StatusRejectHard, StringComparison.OrdinalIgnoreCase);
    }

    public void Update(Dictionary<string, CacheEntry> cache, string fingerprint, DateTime receivedTime, string status, string reason)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return;
        }

        cache[fingerprint] = new CacheEntry
        {
            Fingerprint = fingerprint,
            ReceivedTime = receivedTime,
            LastChecked = DateTime.Now,
            Status = status,
            Reason = reason.Length > 200 ? reason[..200] : reason
        };
    }

    public string MapStatusFromReason(string reason)
    {
        var r = (reason ?? string.Empty).Trim().ToUpperInvariant();

        if (r.StartsWith("OK", StringComparison.Ordinal))
        {
            return StatusMatched;
        }

        if (r.Contains("INVOICE_CONTEXT_BLOCK", StringComparison.Ordinal) ||
            r.Contains("PREFER_HARD_TOTAL_CANDIDATE", StringComparison.Ordinal) ||
            r.Contains("NO_TOTAL_CONTEXT_FOR_CANDIDATE", StringComparison.Ordinal))
        {
            return StatusRejectHard;
        }

        return StatusRejectSoft;
    }

    private static DateTime ParseDate(string s, DateTime fallback)
    {
        return DateTime.TryParse(s, out var dt) ? dt : fallback;
    }

    private static string CsvSafe(string s)
    {
        return (s ?? string.Empty)
            .Replace(';', ',')
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }
}
