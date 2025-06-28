namespace BackupHelper.Core.Utilities;

public static class PathHelper
{
    public static string GetName(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetFileName(path) ?? string.Empty;
    }
}