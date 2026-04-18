using BackupHelper.Abstractions.Credentials;
using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Tests.Extensions;
using BackupHelper.Tests.Shared;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public abstract class FileZipperTestsBase : ZipTestsBase
{
    private Unzipper _unzipper = null!;
    private const string TestPassword = "Test$Password123";
    private const string WrongPassword = "WrongPassword!";

    [SetUp]
    protected override void Setup()
    {
        base.Setup();
        _unzipper = new Unzipper(ZipFilePath, UnzippedFilesDirectoryPath);
    }

    protected IFileZipper CreateFileZipper()
    {
        var fileZipperFactory =
            ServiceScope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        return fileZipperFactory.Create(ZipFilePath, true);
    }

    protected async Task PrepareZipFileAsync(
        TestFileStructure testFileStructure,
        Func<IFileZipper, Task> addFilesToFileZipper
    )
    {
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        await using var fileZipper = CreateFileZipper();

        await addFilesToFileZipper(fileZipper);
        if (fileZipper.HasToBeSaved)
            await fileZipper.SaveAsync();
    }

    protected IFileZipper CreateFileZipperWithPassword(string password)
    {
        using var sensitivePassword = new SensitiveString(password);
        var fileZipperFactory =
            ServiceScope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        return fileZipperFactory.Create(ZipFilePath, true, sensitivePassword);
    }

    protected async Task PrepareZipFileWithPasswordAsync(
        TestFileStructure testFileStructure,
        string password,
        Func<IFileZipper, Task> addFilesToFileZipper
    )
    {
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        await using var fileZipper = CreateFileZipperWithPassword(password);

        await addFilesToFileZipper(fileZipper);
        if (fileZipper.HasToBeSaved)
            await fileZipper.SaveAsync();
    }

    [Test]
    public async Task GivenSingleFile_WhenAddedToZip_ThenUnzippedDirectoryContainsThatFile()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddFileAsync(testFile);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenFileWithCustomZipPath_WhenAddedToZip_ThenUnzippedFileIsInSpecifiedSubdirectory()
    {
        var testFile = new TestFile("file1", "zip-dir1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddFileAsync(testFile);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenEmptyDirectory_WhenAddedToZip_ThenUnzippedDirectoryContainsEmptySubdirectory()
    {
        var testDirectory = new TestDirectory("dir1", [], []);
        using var testFileStructure = new TestFileStructure([], [testDirectory]);

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddDirectoryAsync(testDirectory);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenEmptyDirectoryWithCustomZipPath_WhenAddedToZip_ThenUnzippedDirectoryContainsSpecifiedEmptySubdirectory()
    {
        var testDirectory = new TestDirectory("dir1", [], [], "zip-dir1");
        using var testFileStructure = new TestFileStructure([], [testDirectory]);

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddDirectoryAsync(testDirectory);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenMultipleFiles_WhenDirectoryContentAddedToZip_ThenUnzippedDirectoryContainsAllFiles()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2");
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], []);

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddDirectoryContentAsync(testFileStructure);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenNestedDirectoriesAndFiles_WhenDirectoryContentAddedToZip_ThenUnzippedDirectoryMatchesFullStructure()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2");
        var testFile3 = new TestFile("file3");
        var testFile4 = new TestFile("file4");
        var testFile5 = new TestFile("file5");
        var testDirectory1 = new TestDirectory("dir1", [testFile3, testFile4], []);
        var testDirectory2 = new TestDirectory("dir2", [testFile5], []);
        using var testFileStructure = new TestFileStructure(
            [testFile1, testFile2],
            [testDirectory1, testDirectory2]
        );

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddDirectoryContentAsync(testFileStructure);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenFilesAndDirectoriesWithCustomZipPaths_WhenAddedSeparatelyToZip_ThenUnzippedStructureMatchesCustomPaths()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2", "zip-dir1");
        var testFile3 = new TestFile("file3");
        var testFile4 = new TestFile("file4");
        var testFile5 = new TestFile("file5");
        var testDirectory1 = new TestDirectory("dir1", [testFile3, testFile4], []);
        var testDirectory2 = new TestDirectory("dir2", [testFile5], [], "zip-dir2");
        using var testFileStructure = new TestFileStructure(
            [testFile1, testFile2],
            [testDirectory1, testDirectory2]
        );

        await PrepareZipFileAsync(
            testFileStructure,
            async fileZipper =>
            {
                await fileZipper.AddTopLevelFilesAndDirectoriesSeparatelyAsync(testFileStructure);
            }
        );
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenSingleFile_WhenZippedWithPassword_ThenCanUnzipWithCorrectPassword()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        await PrepareZipFileWithPasswordAsync(
            testFileStructure,
            TestPassword,
            fileZipper => fileZipper.AddFileAsync(testFile)
        );

        _unzipper.UnzipFile(TestPassword);
        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public async Task GivenSingleFile_WhenZippedWithPassword_ThenUnzippingWithWrongPasswordThrows()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        await PrepareZipFileWithPasswordAsync(
            testFileStructure,
            TestPassword,
            fileZipper => fileZipper.AddFileAsync(testFile)
        );

        Assert.Throws<ZipException>(() => _unzipper.UnzipFile(WrongPassword));
    }
}
