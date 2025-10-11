using ICSharpCode.SharpZipLib.Zip;

namespace BackupHelper.Tests.Shared;

public class Unzipper
{
    public Unzipper(string zipFilePath, string unzipDirectoryPath)
    {
        ZipFilePath = zipFilePath;
        UnzipDirectoryPath = unzipDirectoryPath;
    }

    public string ZipFilePath { get; }
    public string UnzipDirectoryPath { get; }

    public void UnzipFile(string? password = null)
    {
        var fastZip = new FastZip() { CreateEmptyDirectories = true };

        if (!string.IsNullOrEmpty(password))
        {
            fastZip.Password = password;
        }
        fastZip.ExtractZip(ZipFilePath, UnzipDirectoryPath, null);
    }
}
