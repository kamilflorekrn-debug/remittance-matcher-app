using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms;

internal static class Program
{
    private const string AppDisplayName = "Getinge Remittance Matcher";
    private const string AppTitle = "GETINGE - REMITTANCE ADVICE MATCHER";

    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();

        var info = FindInstallInfo() ?? FindFromLocalUnins();
        if (info is null)
        {
            MessageBox.Show(
                "Nie znaleziono zainstalowanej aplikacji.\n\nSprawdzono rejestr systemu oraz lokalny katalog programu.",
                AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 1;
        }

        var locationText = string.IsNullOrWhiteSpace(info.InstallLocation) ? "(brak danych)" : info.InstallLocation;
        var confirm = MessageBox.Show(
            $"Wykryty katalog instalacji:\n{locationText}\n\nCzy uruchomic odinstalowanie?",
            AppTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return 0;
        }

        var command = NormalizeUninstallCommand(info);
        if (command is null)
        {
            MessageBox.Show(
                "Nie mozna odnalezc programu odinstalowujacego (unins*.exe).",
                AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 2;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command.Value.ExePath,
                Arguments = command.Value.Arguments,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(command.Value.ExePath) ?? Environment.CurrentDirectory
            };

            Process.Start(psi);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie udalo sie uruchomic odinstalowania.\n\n{ex.Message}",
                AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 3;
        }
    }

    private static InstallInfo? FindInstallInfo()
    {
        foreach (var root in EnumerateUninstallRoots())
        {
            using var uninstallRoot = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallRoot is null)
            {
                continue;
            }

            foreach (var subName in uninstallRoot.GetSubKeyNames())
            {
                using var sub = uninstallRoot.OpenSubKey(subName);
                if (sub is null)
                {
                    continue;
                }

                var displayName = (sub.GetValue("DisplayName") as string)?.Trim() ?? string.Empty;
                if (!displayName.Contains(AppDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var installLocation = (sub.GetValue("InstallLocation") as string)?.Trim() ?? string.Empty;
                var uninstallString = (sub.GetValue("UninstallString") as string)?.Trim() ?? string.Empty;

                return new InstallInfo(displayName, installLocation, uninstallString);
            }
        }

        return null;
    }

    private static InstallInfo? FindFromLocalUnins()
    {
        var local = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            AppDisplayName);

        if (!Directory.Exists(local))
        {
            return null;
        }

        var unins = Directory.GetFiles(local, "unins*.exe", SearchOption.TopDirectoryOnly)
            .OrderByDescending(Path.GetFileName)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(unins))
        {
            return null;
        }

        return new InstallInfo(AppDisplayName, local, $"\"{unins}\"");
    }

    private static IEnumerable<RegistryKey> EnumerateUninstallRoots()
    {
        yield return Registry.CurrentUser;
        yield return Registry.LocalMachine;
    }

    private static (string ExePath, string Arguments)? NormalizeUninstallCommand(InstallInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.UninstallString))
        {
            var split = SplitCommandLine(info.UninstallString);
            if (!string.IsNullOrWhiteSpace(split.ExePath) && File.Exists(split.ExePath))
            {
                return split;
            }
        }

        if (!string.IsNullOrWhiteSpace(info.InstallLocation) && Directory.Exists(info.InstallLocation))
        {
            var unins = Directory.GetFiles(info.InstallLocation, "unins*.exe", SearchOption.TopDirectoryOnly)
                .OrderByDescending(Path.GetFileName)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(unins))
            {
                return (unins, string.Empty);
            }
        }

        return null;
    }

    private static (string ExePath, string Arguments) SplitCommandLine(string command)
    {
        var text = command.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return (string.Empty, string.Empty);
        }

        if (text.StartsWith("\"", StringComparison.Ordinal))
        {
            var end = text.IndexOf('"', 1);
            if (end > 1)
            {
                var exe = text.Substring(1, end - 1);
                var args = text[(end + 1)..].Trim();
                return (exe, args);
            }
        }

        var exeIndex = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIndex > 0)
        {
            var exe = text[..(exeIndex + 4)].Trim();
            var args = text[(exeIndex + 4)..].Trim();
            return (exe, args);
        }

        return (text, string.Empty);
    }

    private sealed record InstallInfo(string DisplayName, string InstallLocation, string UninstallString);
}
