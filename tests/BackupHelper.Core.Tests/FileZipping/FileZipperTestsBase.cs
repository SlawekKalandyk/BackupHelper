using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Tests.Extensions;
using BackupHelper.Tests.Shared;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public abstract class FileZipperTestsBase : ZipTestsBase
{
    private Unzipper _unzipper;
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
        var fileZipperFactory = ServiceScope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        return fileZipperFactory.Create(ZipFilePath, true);
    }

    protected void PrepareZipFile(TestFileStructure testFileStructure, Action<IFileZipper> addFilesToFileZipper)
    {
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        using var fileZipper = CreateFileZipper();

        addFilesToFileZipper(fileZipper);
        if (fileZipper.HasToBeSaved)
            fileZipper.Save();
    }

    protected IFileZipper CreateFileZipperWithPassword(string password)
    {
        var fileZipperFactory = ServiceScope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        return fileZipperFactory.Create(ZipFilePath, true, password);
    }

    protected void PrepareZipFileWithPassword(TestFileStructure testFileStructure, string password, Action<IFileZipper> addFilesToFileZipper)
    {
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        using var fileZipper = CreateFileZipperWithPassword(password);

        addFilesToFileZipper(fileZipper);
        if (fileZipper.HasToBeSaved)
            fileZipper.Save();
    }

    [Test]
    public void GivenSingleFile_WhenAddedToZip_ThenUnzippedDirectoryContainsThatFile()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddFile(testFile);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenFileWithCustomZipPath_WhenAddedToZip_ThenUnzippedFileIsInSpecifiedSubdirectory()
    {
        var testFile = new TestFile("file1", "zip-dir1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddFile(testFile);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenEmptyDirectory_WhenAddedToZip_ThenUnzippedDirectoryContainsEmptySubdirectory()
    {
        var testDirectory = new TestDirectory("dir1", [], []);
        using var testFileStructure = new TestFileStructure([], [testDirectory]);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectory(testDirectory);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenEmptyDirectoryWithCustomZipPath_WhenAddedToZip_ThenUnzippedDirectoryContainsSpecifiedEmptySubdirectory()
    {
        var testDirectory = new TestDirectory("dir1", [], [], "zip-dir1");
        using var testFileStructure = new TestFileStructure([], [testDirectory]);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectory(testDirectory);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenMultipleFiles_WhenDirectoryContentAddedToZip_ThenUnzippedDirectoryContainsAllFiles()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2");
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], []);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectoryContent(testFileStructure);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenNestedDirectoriesAndFiles_WhenDirectoryContentAddedToZip_ThenUnzippedDirectoryMatchesFullStructure()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2");
        var testFile3 = new TestFile("file3");
        var testFile4 = new TestFile("file4");
        var testFile5 = new TestFile("file5");
        var testDirectory1 = new TestDirectory("dir1", [testFile3, testFile4], []);
        var testDirectory2 = new TestDirectory("dir2", [testFile5], []);
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], [testDirectory1, testDirectory2]);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectoryContent(testFileStructure);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenFilesAndDirectoriesWithCustomZipPaths_WhenAddedSeparatelyToZip_ThenUnzippedStructureMatchesCustomPaths()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2", "zip-dir1");
        var testFile3 = new TestFile("file3");
        var testFile4 = new TestFile("file4");
        var testFile5 = new TestFile("file5");
        var testDirectory1 = new TestDirectory("dir1", [testFile3, testFile4], []);
        var testDirectory2 = new TestDirectory("dir2", [testFile5], [], "zip-dir2");
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], [testDirectory1, testDirectory2]);

        PrepareZipFile(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddTopLevelFilesAndDirectoriesSeparately(testFileStructure);
            });
        _unzipper.UnzipFile();

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenSingleFile_WhenZippedWithPassword_ThenCanUnzipWithCorrectPassword()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        PrepareZipFileWithPassword(
            testFileStructure,
            TestPassword,
            fileZipper => fileZipper.AddFile(testFile));

        _unzipper.UnzipFile(TestPassword);
        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenSingleFile_WhenZippedWithPassword_ThenUnzippingWithWrongPasswordThrows()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        PrepareZipFileWithPassword(
            testFileStructure,
            TestPassword,
            fileZipper => fileZipper.AddFile(testFile));

        Assert.Throws<ZipException>(() => _unzipper.UnzipFile(WrongPassword));
    }
}