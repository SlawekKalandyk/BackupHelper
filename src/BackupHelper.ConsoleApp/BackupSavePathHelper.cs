namespace BackupHelper.ConsoleApp;

public class BackupSavePathHelper
{
    public static string GetBackupSavePath(string argPath)
    {
        if (File.Exists(argPath))
        {
            return argPath;
        }

        if (Directory.Exists(argPath))
        {
            return Path.Combine(argPath, CreateDateTimeBasedZipFileName());
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

        return Path.Combine(argPath, CreateDateTimeBasedZipFileName());
    }

    public static string CreateDateTimeBasedZipFileName()
    {
        var time = DateTime.Now;
        var baseFileName = $"{time:yyyy-MM-dd_HH-mm-ss}_backup";
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