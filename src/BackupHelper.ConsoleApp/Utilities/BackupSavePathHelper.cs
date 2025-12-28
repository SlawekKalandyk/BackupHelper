namespace BackupHelper.ConsoleApp.Utilities;

public class BackupSavePathHelper
{
    public static string GetBackupSavePath(string argPath, string? zipFilenameSuffix = null)
    {
        if (File.Exists(argPath))
        {
            return argPath;
        }

        if (Directory.Exists(argPath))
        {
            return Path.Combine(argPath, CreateDateTimeBasedZipFileName(zipFilenameSuffix));
        }

        if (Path.HasExtension(argPath))
        {
            var directories = Path.GetDirectoryName(argPath);

            if (string.IsNullOrEmpty(directories))
            {
                return argPath;
            }

            Directory.CreateDirectory(directories);

            return argPath;
        }

        Directory.CreateDirectory(argPath);

        return Path.Combine(argPath, CreateDateTimeBasedZipFileName(zipFilenameSuffix));
    }

    public static string CreateDateTimeBasedZipFileName(string? zipFilenameSuffix)
    {
        var suffix = string.IsNullOrWhiteSpace(zipFilenameSuffix) ? "backup" : zipFilenameSuffix;
        var time = DateTime.Now;
        var baseFileName = $"{time:yyyy-MM-dd_HH-mm-ss}_{suffix}";
        var fileName = $"{baseFileName}.zip";
        var i = 1;

        while (File.Exists(fileName))
        {
            fileName = baseFileName + $".{i}.zip";
            ++i;
        }

        return fileName;
    }
}