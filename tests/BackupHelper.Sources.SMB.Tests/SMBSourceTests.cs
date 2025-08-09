using System.Text;
using BackupHelper.Sources.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Sources.SMB.Tests;

[TestFixture]
public class SMBSourceTests : SMBTestsBase
{
    private ISource GetSMBSource()
    {
        return ServiceScope.ServiceProvider.GetServices<ISource>().Single(source => source.GetScheme() == SMBSource.Scheme);
    }

    private void CreateTestFileStructure()
    {
        SMBTestConfigurationProvider.CreateTestDirectory("SubDir1");
        SMBTestConfigurationProvider.CreateTestDirectory("SubDir2");
        SMBTestConfigurationProvider.CreateTestFile("file1.txt", "Content of file 1");
        SMBTestConfigurationProvider.CreateTestFile("file2.txt", "Content of file 2");
        SMBTestConfigurationProvider.CreateTestFile("SubDir1\\file3.txt", "Content of file 3 in SubDir1");
        SMBTestConfigurationProvider.CreateTestFile("SubDir2\\file4.txt", "Content of file 4 in SubDir2");
        SMBTestConfigurationProvider.CreateTestFile("SubDir2\\file5.txt", "Content of file 5 in SubDir2");
    }

    [Test]
    public void GivenSMBShare_WhenReadingFileWithContent_ThenExpectedContentIsReturned()
    {
        var testFileName = "testFile.txt";
        var expectedText = "This is a test file.";
        SMBTestConfigurationProvider.CreateTestFile(testFileName, expectedText);

        var smbSource = GetSMBSource();
        using var stream = smbSource.GetStream($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\{testFileName}");
        using var streamReader = new StreamReader(stream);
        var actualText = streamReader.ReadToEnd();

        Assert.That(actualText, Is.EqualTo(expectedText));
    }

    [Test]
    public void GivenSMBShare_WhenListingSubDirectories_ThenCorrectSubDirectoriesAreReturned()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var subDirectories = smbSource.GetSubDirectories($"{SMBTestConfigurationProvider.TestsDirectoryPath}").ToList();
        Assert.That(subDirectories.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\SubDir1"));
        Assert.That(subDirectories.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\SubDir2"));
    }

    [Test]
    public void GivenSMBShare_WhenListingFiles_ThenCorrectFilesAreReturned()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var files = smbSource.GetFiles($"{SMBTestConfigurationProvider.TestsDirectoryPath}").ToList();
        Assert.That(files.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\file1.txt"));
        Assert.That(files.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\file2.txt"));
    }

    [Test]
    public void GivenExistingSMBFile_WhenCheckingIfFileExists_ThenReturnsTrue()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        Assert.That(smbSource.FileExists($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\file1.txt"));
    }

    [Test]
    public void GivenNonExistingSMBFile_WhenCheckingIfFileExists_ThenReturnsFalse()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        Assert.That(smbSource.FileExists($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\nonexistent.txt"), Is.False);
    }

    [Test]
    public void GivenExistingSMBDirectory_WhenCheckingIfDirectoryExists_ThenReturnsTrue()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        Assert.That(smbSource.DirectoryExists($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\SubDir1"));
    }

    [Test]
    public void GivenNonExistingSMBDirectory_WhenCheckingIfDirectoryExists_ThenReturnsFalse()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        Assert.That(smbSource.DirectoryExists($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\NonExistentDirectory"), Is.False);
    }
}