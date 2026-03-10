using RemittanceMatcherApp.Models;

namespace RemittanceMatcherApp.Services;

public sealed class CsvService
{
    public Dictionary<string, FebanRecord> LoadFebanCsvWithDate(string path)
    {
        var output = new Dictionary<string, FebanRecord>(StringComparer.Ordinal);

        if (!File.Exists(path))
        {
            return output;
        }

        using var sr = new StreamReader(path);

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
            if (parts.Length < 4)
            {
                continue;
            }

            var stmtS = parts[0].Trim();
            var key = parts[1].Trim();
            var disp = parts[2].Trim();
            var partner = parts[3].Trim();

            if (string.IsNullOrWhiteSpace(key) || output.ContainsKey(key))
            {
                continue;
            }

            output[key] = new FebanRecord
            {
                Key = key,
                DisplayAmount = disp,
                Partner = partner,
                StatementDate = ParseStatementDate(stmtS)
            };
        }

        return output;
    }

    private DateTime ParseStatementDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DateTime.Today;
        }

        var t = input.Trim().Replace('/', '.').Replace('-', '.');
        var parts = t.Split('.');
        if (parts.Length < 3)
        {
            return DateTime.Today;
        }

        if (!int.TryParse(parts[0], out var dd)) return DateTime.Today;
        if (!int.TryParse(parts[1], out var mm)) return DateTime.Today;
        if (!int.TryParse(parts[2], out var yy)) return DateTime.Today;

        if (yy < 100)
        {
            yy += 2000;
        }

        try
        {
            return new DateTime(yy, mm, dd);
        }
        catch
        {
            return DateTime.Today;
        }
    }
}
