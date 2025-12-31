namespace BackupHelper.Core.Utilities;

public class BackupSavePathHelper
{
    public static string GetBackupSaveFilePath(
        string? outputDirectory,
        string? zipFilenameSuffix = null
    )
    {
        outputDirectory ??= Path.Join(Path.GetTempPath(), "BackupHelper");
        return Path.Join(outputDirectory, CreateDateTimeBasedZipFileName(zipFilenameSuffix));
    }

    private static string CreateDateTimeBasedZipFileName(string? zipFilenameSuffix)
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