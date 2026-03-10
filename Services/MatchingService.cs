using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RemittanceMatcherApp.Models;

namespace RemittanceMatcherApp.Services;

public sealed class MatchingService
{
    private const decimal MaxAmount = 50_000_000m;

    public MatchResult? FindBestValidatedMatch(string text, IReadOnlyDictionary<string, FebanRecord> feban, AppSettings settings, ref int overflowSkips)
    {
        if (string.IsNullOrWhiteSpace(text) || feban.Count == 0)
        {
            return null;
        }

        var normalized = NormalizeExtractedText(text);
        var scores = new Dictionary<string, int>(StringComparer.Ordinal);
        var signals = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var kv in feban)
        {
            var key = kv.Key;
            if (!CandidateAmountAppearsAnywhere(normalized, kv.Value.DisplayAmount, settings))
            {
                continue;
            }

            var score = 0;

            if (CandidateInStrongTotalContext(normalized, kv.Value.DisplayAmount, settings))
            {
                score += settings.ScoreStrongTotalWeight;
                AddSignal(signals, key, "HARD");
            }

            if (CandidateInTotalContext(normalized, kv.Value.DisplayAmount, settings))
            {
                score += settings.ScoreTotalContextWeight;
                AddSignal(signals, key, "TOTAL_CTX");
            }

            if (CandidateInKeywordWindow(normalized, kv.Value.DisplayAmount, settings))
            {
                score += settings.ScoreKeywordWindowWeight;
                AddSignal(signals, key, "KW_WIN");
            }

            if (CandidateNearInvoiceNumber(normalized, kv.Value.DisplayAmount, settings))
            {
                score += settings.ScoreLineItemPenaltyWeight;
                AddSignal(signals, key, "INV_NEAR");
            }

            if (score != 0)
            {
                scores[key] = score;
            }
        }

        if (scores.Count == 0)
        {
            return null;
        }

        var ordered = scores.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();
        var top = ordered[0];
        var secondScore = ordered.Count > 1 ? ordered[1].Value : 0;
        var margin = top.Value - secondScore;
        var signalCount = signals.TryGetValue(top.Key, out var set) ? set.Count : 0;
        var hasHard = signals.TryGetValue(top.Key, out var topSignals) && topSignals.Contains("HARD");

        var reason = string.Empty;

        if (top.Value < settings.ScoreMinToSave)
        {
            reason = $"LOW_SCORE: {top.Value}";
            return new MatchResult { Key = string.Empty, Reason = reason, Score = top.Value, Margin = margin };
        }

        if (margin < settings.ScoreMarginToSave)
        {
            reason = $"LOW_MARGIN: {margin}";
            return new MatchResult { Key = string.Empty, Reason = reason, Score = top.Value, Margin = margin };
        }

        if (settings.RequireHardSignalOrTwoSignals)
        {
            if (!hasHard && signalCount < 2)
            {
                reason = "NOT_ENOUGH_EVIDENCE";
                return new MatchResult { Key = string.Empty, Reason = reason, Score = top.Value, Margin = margin };
            }
        }

        var topRecord = feban[top.Key];
        var topStrongTotal = CandidateInStrongTotalContext(normalized, topRecord.DisplayAmount, settings);

        if (settings.PreferHardTotalOverHigherScore && !topStrongTotal && ExistsAnyHardSignalOtherThan(signals, top.Key))
        {
            reason = "PREFER_HARD_TOTAL_CANDIDATE";
            return new MatchResult { Key = string.Empty, Reason = reason, Score = top.Value, Margin = margin };
        }

        var topNearInvoice = CandidateNearInvoiceNumber(normalized, topRecord.DisplayAmount, settings);
        if (settings.BlockInvoiceContextWithoutStrongTotal && topNearInvoice && !topStrongTotal)
        {
            var singleAllowed = IsSingleInvoiceExceptionAllowed(normalized, top.Key, top.Value, margin, feban, settings);
            if (!singleAllowed)
            {
                reason = "INVOICE_CONTEXT_BLOCK";
                return new MatchResult { Key = string.Empty, Reason = reason, Score = top.Value, Margin = margin };
            }
        }

        if (settings.RequireCandidateInTotalContext)
        {
            if (!CandidateInTotalContext(normalized, topRecord.DisplayAmount, settings))
            {
                reason = "NO_TOTAL_CONTEXT_FOR_CANDIDATE";
                return new MatchResult { Key = string.Empty, Reason = reason, Score = top.Value, Margin = margin };
            }
        }

        reason = $"OK SCORE={top.Value} MARGIN={margin}";
        return new MatchResult { Key = top.Key, Reason = reason, Score = top.Value, Margin = margin };
    }

    public string NormalizeExtractedText(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var t = s.Replace('\u00A0', ' ')
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "\r\n");

        t = Regex.Replace(t, "([,\\.])\\s+(\\d{2})\\b", "$1$2", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, "(\\d(?:[\\d,\\s]{0,20}))\\s*(?:\\r\\n)+\\s*([,\\.])\\s*(\\d{2})\\b", "$1$2$3", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, "([,\\.]\\d)\\s+(\\d)\\b", "$1$2", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, "[ \\t]{2,}", " ", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, "(\\r\\n){3,}", "\r\n\r\n", RegexOptions.IgnoreCase);
        return t;
    }

    private static void AddSignal(Dictionary<string, HashSet<string>> signalByKey, string key, string signal)
    {
        if (!signalByKey.TryGetValue(key, out var s))
        {
            s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            signalByKey[key] = s;
        }

        s.Add(signal);
    }

    private static bool ExistsAnyHardSignalOtherThan(Dictionary<string, HashSet<string>> signalByKey, string topKey)
    {
        foreach (var kv in signalByKey)
        {
            if (string.Equals(kv.Key, topKey, StringComparison.Ordinal))
            {
                continue;
            }

            if (kv.Value.Contains("HARD"))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSingleInvoiceExceptionAllowed(
        string text,
        string candidateKey,
        int score,
        int margin,
        IReadOnlyDictionary<string, FebanRecord> feban,
        AppSettings settings)
    {
        if (CountUniqueInvoiceNumbers(text, settings.InvoiceNumberRegex) != 1)
        {
            return false;
        }

        if (score < settings.ScoreMinToSave + settings.SingleInvoiceScoreBonus)
        {
            return false;
        }

        if (margin < settings.ScoreMarginToSave + settings.SingleInvoiceMarginBonus)
        {
            return false;
        }

        var foundCandidates = 0;
        foreach (var kv in feban)
        {
            if (CandidateAmountAppearsAnywhere(text, kv.Value.DisplayAmount, settings))
            {
                foundCandidates++;
                if (foundCandidates > 1)
                {
                    return false;
                }
            }
        }

        return foundCandidates == 1 && feban.ContainsKey(candidateKey);
    }

    private static int CountUniqueInvoiceNumbers(string text, string invoiceRegex)
    {
        var matches = Regex.Matches(text, invoiceRegex, RegexOptions.IgnoreCase);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in matches)
        {
            if (!string.IsNullOrWhiteSpace(m.Value))
            {
                set.Add(m.Value);
            }
        }

        return set.Count;
    }

    private bool CandidateInStrongTotalContext(string text, string displayAmount, AppSettings settings)
    {
        var lines = NormalizeLineBreaks(text).Split("\r\n");

        for (var i = 0; i < lines.Length; i++)
        {
            if (!ContainsAnyKeyword(lines[i], settings.StrongTotalKeywords))
            {
                continue;
            }

            var from = Math.Max(0, i - settings.StrongTotalNeighborLines);
            var to = Math.Min(lines.Length - 1, i + settings.StrongTotalNeighborLines);
            for (var j = from; j <= to; j++)
            {
                if (LineContainsCandidateAmount(lines[j], displayAmount, settings.AllowLooseDigitsAmountMatch, settings.MinDigitsForLooseMatch))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CandidateInTotalContext(string text, string displayAmount, AppSettings settings)
    {
        var lines = NormalizeLineBreaks(text).Split("\r\n");

        for (var i = 0; i < lines.Length; i++)
        {
            if (!ContainsAnyKeyword(lines[i], settings.TotalKeywords))
            {
                continue;
            }

            var from = Math.Max(0, i - settings.TotalKeywordNeighborLines);
            var to = Math.Min(lines.Length - 1, i + settings.TotalKeywordNeighborLines);
            for (var j = from; j <= to; j++)
            {
                if (LineContainsCandidateAmount(lines[j], displayAmount, settings.AllowLooseDigitsAmountMatch, settings.MinDigitsForLooseMatch))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CandidateInKeywordWindow(string text, string displayAmount, AppSettings settings)
    {
        var t = text.ToLowerInvariant();
        foreach (var keyword in settings.TotalKeywords)
        {
            var p = t.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            while (p >= 0)
            {
                var len = Math.Min(settings.TotalContextWindowChars, text.Length - p);
                var win = text.Substring(p, len);
                if (LineContainsCandidateAmount(win, displayAmount, settings.AllowLooseDigitsAmountMatch, settings.MinDigitsForLooseMatch))
                {
                    return true;
                }

                p = t.IndexOf(keyword, p + 1, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    private static bool CandidateNearInvoiceNumber(string text, string displayAmount, AppSettings settings)
    {
        var lines = NormalizeLineBreaks(text).Split("\r\n");
        var re = new Regex(settings.InvoiceNumberRegex, RegexOptions.IgnoreCase);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!LineContainsCandidateAmount(lines[i], displayAmount, false, settings.MinDigitsForLooseMatch))
            {
                continue;
            }

            var from = Math.Max(0, i - settings.InvoiceBlockWindowLines);
            var to = Math.Min(lines.Length - 1, i + settings.InvoiceBlockWindowLines);
            for (var j = from; j <= to; j++)
            {
                if (re.IsMatch(lines[j]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CandidateAmountAppearsAnywhere(string text, string displayAmount, AppSettings settings)
    {
        var lines = NormalizeLineBreaks(text).Split("\r\n");
        foreach (var ln in lines)
        {
            if (LineContainsCandidateAmount(ln, displayAmount, settings.AllowLooseDigitsAmountMatch, settings.MinDigitsForLooseMatch))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAnyKeyword(string line, IEnumerable<string> keywords)
    {
        var t = line.ToLowerInvariant();
        foreach (var keyword in keywords)
        {
            if (t.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeLineBreaks(string s)
    {
        return s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
    }

    private static bool LineContainsCandidateAmount(string lineText, string displayAmount, bool allowDigitsOnly, int minDigitsForLooseMatch)
    {
        var tokens = GetAmountSearchTokens(displayAmount);
        if (string.IsNullOrWhiteSpace(tokens.Dot))
        {
            return false;
        }

        var t = lineText.ToLowerInvariant().Replace('\u00A0', ' ');

        if (ContainsAmountToken(t, tokens.Dot) ||
            ContainsAmountToken(t, tokens.Comma) ||
            ContainsAmountToken(t, tokens.DotThousands) ||
            ContainsAmountToken(t, tokens.CommaThousands))
        {
            return true;
        }

        if (allowDigitsOnly && tokens.DigitsWanted.Length >= minDigitsForLooseMatch)
        {
            var digitsLine = TrimLeadingZeros(DigitsOnly(t));
            if (digitsLine.Contains(tokens.DigitsWanted, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAmountToken(string input, string token)
    {
        return !string.IsNullOrWhiteSpace(token) && input.Contains(token.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    private static (string Dot, string Comma, string DotThousands, string CommaThousands, string DigitsWanted) GetAmountSearchTokens(string displayAmount)
    {
        var dot = NormalizeAmountDisplayForSearch(displayAmount);
        if (string.IsNullOrWhiteSpace(dot))
        {
            return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var comma = dot.Replace('.', ',');
        var dotThousands = AddThousandsCommasToAmount(dot);
        var commaThousands = dotThousands.Replace('.', ',');
        var digits = TrimLeadingZeros(DigitsOnly(dot));

        return (dot, comma, dotThousands, commaThousands, digits);
    }

    private static string NormalizeAmountDisplayForSearch(string disp)
    {
        var t = (disp ?? string.Empty).Trim().Replace("\u00A0", "").Replace(" ", "");
        if (t.Length == 0)
        {
            return string.Empty;
        }

        var pDot = t.LastIndexOf('.');
        var pCom = t.LastIndexOf(',');

        if (pDot >= 0 && pCom >= 0)
        {
            if (pDot > pCom)
            {
                t = t.Replace(",", "");
            }
            else
            {
                t = t.Replace(".", "").Replace(',', '.');
            }
        }
        else if (pCom >= 0 && pDot < 0)
        {
            if (t.Length - pCom - 1 == 2)
            {
                t = t.Replace(',', '.');
            }
            else
            {
                t = t.Replace(",", "");
            }
        }

        return t;
    }

    private static string AddThousandsCommasToAmount(string amountDot)
    {
        var s = amountDot.Trim();
        if (s.Length == 0)
        {
            return string.Empty;
        }

        var p = s.IndexOf('.');
        var intPart = p >= 0 ? s[..p] : s;
        var decPart = p >= 0 ? s[(p + 1)..] : string.Empty;

        intPart = intPart.Replace(",", "");
        if (intPart.Length <= 3)
        {
            return decPart.Length > 0 ? $"{intPart}.{decPart}" : intPart;
        }

        var sb = new StringBuilder();
        var cnt = 0;
        for (var i = intPart.Length - 1; i >= 0; i--)
        {
            sb.Insert(0, intPart[i]);
            cnt++;
            if (cnt % 3 == 0 && i > 0)
            {
                sb.Insert(0, ',');
            }
        }

        return decPart.Length > 0 ? $"{sb}.{decPart}" : sb.ToString();
    }

    private static string DigitsOnly(string s)
    {
        return Regex.Replace(s, "[^\\d]", string.Empty);
    }

    private static string TrimLeadingZeros(string s)
    {
        var t = s;
        while (t.Length > 1 && t[0] == '0')
        {
            t = t[1..];
        }

        return t;
    }

    public bool LooksLikeInlineRemittance(string bodyText, string subject)
    {
        var t = (bodyText + " " + subject).ToLowerInvariant();
        return t.Contains("remittance", StringComparison.Ordinal) ||
               t.Contains("remittance advice", StringComparison.Ordinal) ||
               t.Contains("payment remittance", StringComparison.Ordinal) ||
               t.Contains("total payment", StringComparison.Ordinal) ||
               t.Contains("payment amount", StringComparison.Ordinal) ||
               t.Contains("amount payable", StringComparison.Ordinal) ||
               t.Contains("amount paid", StringComparison.Ordinal) ||
               t.Contains("paid by bacs", StringComparison.Ordinal) ||
               t.Contains("bank transfer", StringComparison.Ordinal);
    }

    public decimal ParseAmountToDecimalSafe(string raw, ref int overflowSkips)
    {
        try
        {
            var cleaned = new string(raw.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',' || ch == ' ' || ch == '\u00A0').ToArray());
            var t = cleaned.Replace("\u00A0", "").Replace(" ", "");

            var pDot = t.LastIndexOf('.');
            var pCom = t.LastIndexOf(',');

            if (pDot >= 0 && pCom >= 0)
            {
                if (pDot > pCom)
                {
                    t = t.Replace(",", "");
                }
                else
                {
                    t = t.Replace(".", "").Replace(',', '.');
                }
            }
            else if (pCom >= 0 && pDot < 0)
            {
                t = t.Replace(',', '.');
            }

            if (!decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
            {
                return 0m;
            }

            if (val <= 0m || val > MaxAmount)
            {
                if (val > MaxAmount) overflowSkips++;
                return 0m;
            }

            return Math.Round(val, 2);
        }
        catch
        {
            overflowSkips++;
            return 0m;
        }
    }
}
