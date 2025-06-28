using System.Text;
using BackupHelper.Sources.Abstractions;
using BackupHelper.Tests.Shared;
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
        CreateTestDirectory("SubDir1");
        CreateTestDirectory("SubDir2");
        CreateTestFile("file1.txt", "Content of file 1");
        CreateTestFile("file2.txt", "Content of file 2");
        CreateTestFile("SubDir1\\file3.txt", "Content of file 3 in SubDir1");
        CreateTestFile("SubDir2\\file4.txt", "Content of file 4 in SubDir2");
        CreateTestFile("SubDir2\\file5.txt", "Content of file 5 in SubDir2");
    }

    private void CreateTestDirectory(string directoryPath)
    {
        using (var smbConnection = GetSMBConnection())
        {
            smbConnection.CreateDirectory(Path.Join(TestsDirectoryName, directoryPath));
        }
    }

    private void CreateTestFile(string filePath, string content = "")
    {
        using var smbConnection = GetSMBConnection();
        using var writeStream = smbConnection.CreateFile(Path.Join(TestsDirectoryName, filePath));
        if (!string.IsNullOrEmpty(content))
        {
            writeStream.Write(Encoding.UTF8.GetBytes(content));
        }
    }

    [Test]
    public void GivenSMBShare_WhenReadingFileWithContent_ThenExpectedContentIsReturned()
    {
        var testFileName = "testFile.txt";
        var expectedText = "This is a test file.";
        CreateTestFile(testFileName, expectedText);

        var smbSource = GetSMBSource();
        using var stream = smbSource.GetStream($"{SMBTestsDirectoryPath}\\{testFileName}");
        using var streamReader = new StreamReader(stream);
        var actualText = streamReader.ReadToEnd();

        Assert.That(actualText, Is.EqualTo(expectedText));
    }

    [Test]
    public void GivenSMBShare_WhenListingSubDirectories_ThenCorrectSubDirectoriesAreReturned()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var subDirectories = smbSource.GetSubDirectories($"{SMBTestsDirectoryPath}").ToList();
        Assert.That(subDirectories.Contains($"{SMBTestsDirectoryPath}\\SubDir1"));
        Assert.That(subDirectories.Contains($"{SMBTestsDirectoryPath}\\SubDir2"));
    }

    [Test]
    public void GivenSMBShare_WhenListingFiles_ThenCorrectFilesAreReturned()
    {
        CreateTestFileStructure();

        var smbSource = GetSMBSource();
        var files = smbSource.GetFiles($"{SMBTestsDirectoryPath}").ToList();
        Assert.That(files.Contains($"{SMBTestsDirectoryPath}\\file1.txt"));
        Assert.That(files.Contains($"{SMBTestsDirectoryPath}\\file2.txt"));
    }
}