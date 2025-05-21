namespace BackupHelper.Tests;

public class TestSettings
{
    public TestSettings(string zipperTestsDirectory)
    {
        ZipperTestsDirectory = zipperTestsDirectory;
    }

    public string ZipperTestsDirectory { get; }
}