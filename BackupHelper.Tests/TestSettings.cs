namespace BackupHelper.Tests;

public class TestSettings
{
    public TestSettings(string fileZipperTestsDirectory)
    {
        FileZipperTestsDirectory = fileZipperTestsDirectory;
    }

    public string FileZipperTestsDirectory { get; }
}