namespace BackupHelper.Core.Tests;

public class TestSettings
{
    public TestSettings(string testsDirectory)
    {
        TestsDirectory = testsDirectory;
    }

    public string TestsDirectory { get; }
}