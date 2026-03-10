namespace RemittanceMatcherApp.Services;

public sealed class PathResolverService
{
    public string ResolveNetworkPathToFile(string relativeFolder, string fileName)
    {
        var folder = ResolveNetworkPathToFolder(relativeFolder);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        return Path.Combine(folder, fileName);
    }

    public string ResolveNetworkPathToFolder(string relativeFolder)
    {
        var driveRoot = FindMappedDriveForRelativeFolder(relativeFolder);
        if (string.IsNullOrWhiteSpace(driveRoot))
        {
            return string.Empty;
        }

        var trimmed = relativeFolder.TrimStart('\\');
        var full = Path.Combine(driveRoot, trimmed);
        return EnsureTrailingSlash(full);
    }

    private string FindMappedDriveForRelativeFolder(string relativeFolder)
    {
        var trimmed = relativeFolder.TrimStart('\\');

        for (var d = 'A'; d <= 'Z'; d++)
        {
            var root = $"{d}:\\";
            var candidate = Path.Combine(root, trimmed);
            if (Directory.Exists(candidate))
            {
                return root;
            }
        }

        return string.Empty;
    }

    public string EnsureTrailingSlash(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.EndsWith("\\", StringComparison.Ordinal) ? path : path + "\\";
    }

    public void EnsureFolderTreeExists(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        Directory.CreateDirectory(folderPath);
    }

    public string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = fileName.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public string GetUniquePath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return fullPath;
        }

        var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        var ext = Path.GetExtension(fullPath);

        var n = 2;
        while (true)
        {
            var candidate = Path.Combine(directory, $"{fileName} ({n}){ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            n++;
        }
    }

    public string GetSaveFolderForDate(string basePath, DateTime date)
    {
        var folder = Path.Combine(basePath, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd.MM"));
        return folder;
    }
}
