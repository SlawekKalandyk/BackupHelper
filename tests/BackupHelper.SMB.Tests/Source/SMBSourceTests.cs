using BackupHelper.Sources.Abstractions;
using BackupHelper.Sources.SMB;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.SMB.Tests.Source;

[TestFixture]
public class SMBSourceTests : SMBTestsBase
{
    private ISource GetSMBSource()
    {
        return ServiceScope
            .ServiceProvider.GetServices<ISource>()
            .Single(source => source.GetScheme() == SMBSource.Scheme);
    }

    private void CreateTestFileStructure()
    {
        SMBTestConfigurationProvider.CreateTestDirectory("SubDir1");
        SMBTestConfigurationProvider.CreateTestDirectory("SubDir2");
        SMBTestConfigurationProvider.CreateTestFile("file1.txt", "Content of file 1");
        SMBTestConfigurationProvider.CreateTestFile("file2.txt", "Content of file 2");
        SMBTestConfigurationProvider.CreateTestFile(
            "SubDir1\\file3.txt",
            "Content of file 3 in SubDir1"
        );
        SMBTestConfigurationProvider.CreateTestFile(
            "SubDir2\\file4.txt",
            "Content of file 4 in SubDir2"
        );
        SMBTestConfigurationProvider.CreateTestFile(
            "SubDir2\\file5.txt",
            "Content of file 5 in SubDir2"
        );
    }

    [Test]
    public async Task GivenSMBShare_WhenReadingFileWithContent_ThenExpectedContentIsReturned()
    {
        var testFileName = "testFile.txt";
        var expectedText = "This is a test file.";
        SMBTestConfigurationProvider.CreateTestFile(testFileName, expectedText);

        var smbSource = GetSMBSource();
        await using var stream = await smbSource.GetStreamAsync(
            $"{SMBTestConfigurationProvider.TestsDirectoryPath}\\{testFileName}"
        );
        using var streamReader = new StreamReader(stream);
        var actualText = await streamReader.ReadToEndAsync();

        Assert.That(actualText, Is.EqualTo(expectedText));
    }

    [Test]
    public async Task GivenSMBShare_WhenListingSubDirectories_ThenCorrectSubDirectoriesAreReturned()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var subDirectories = (
            await smbSource.GetSubDirectoriesAsync(
                $"{SMBTestConfigurationProvider.TestsDirectoryPath}"
            )
        ).ToList();
        Assert.That(
            subDirectories.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\SubDir1")
        );
        Assert.That(
            subDirectories.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\SubDir2")
        );
    }

    [Test]
    public async Task GivenSMBShare_WhenListingFiles_ThenCorrectFilesAreReturned()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var files = (
            await smbSource.GetFilesAsync($"{SMBTestConfigurationProvider.TestsDirectoryPath}")
        ).ToList();
        Assert.That(
            files.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\file1.txt")
        );
        Assert.That(
            files.Contains($"{SMBTestConfigurationProvider.TestsDirectoryPath}\\file2.txt")
        );
    }

    [Test]
    public async Task GivenExistingSMBFile_WhenCheckingIfFileExists_ThenReturnsTrue()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var exists = await smbSource.FileExistsAsync(
            $"{SMBTestConfigurationProvider.TestsDirectoryPath}\\file1.txt"
        );
        Assert.That(exists);
    }

    [Test]
    public async Task GivenNonExistingSMBFile_WhenCheckingIfFileExists_ThenReturnsFalse()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var exists = await smbSource.FileExistsAsync(
            $"{SMBTestConfigurationProvider.TestsDirectoryPath}\\nonexistent.txt"
        );
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task GivenExistingSMBDirectory_WhenCheckingIfDirectoryExists_ThenReturnsTrue()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var exists = await smbSource.DirectoryExistsAsync(
            $"{SMBTestConfigurationProvider.TestsDirectoryPath}\\SubDir1"
        );
        Assert.That(exists);
    }

    [Test]
    public async Task GivenNonExistingSMBDirectory_WhenCheckingIfDirectoryExists_ThenReturnsFalse()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var exists = await smbSource.DirectoryExistsAsync(
            $"{SMBTestConfigurationProvider.TestsDirectoryPath}\\NonExistentDirectory"
        );
        Assert.That(exists, Is.False);
    }
}
