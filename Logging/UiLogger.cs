using System.Globalization;

namespace RemittanceMatcherApp.Logging;

public sealed class UiLogger : IAppLogger
{
    private readonly Action<string> _sink;

    public UiLogger(Action<string> sink)
    {
        _sink = sink;
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}] [{level}] {message}";
        _sink(line);
    }
}
