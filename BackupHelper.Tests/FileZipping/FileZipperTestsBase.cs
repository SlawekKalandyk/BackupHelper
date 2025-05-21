using System.IO.Compression;
using BackupHelper.Core.FileZipping;
using BackupHelper.Tests.Utilities;
using Newtonsoft.Json;

namespace BackupHelper.Tests.FileZipping;

[TestFixture]
public abstract class FileZipperTestsBase
{
    private string FileZipperTestRootPath { get; set; }
    private string ZippedFilesDirectoryPath => Path.Combine(FileZipperTestRootPath, "file-zipper-tests-zipped");
    private string UnzippedFilesDirectoryPath => Path.Combine(FileZipperTestRootPath, "file-zipper-tests-unzipped");
    private string ZipFilePath => Path.Combine(FileZipperTestRootPath, "file-zipper-tests-zipped-file.zip");

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var jsonTestSettings = File.ReadAllText("testSettings.json");
        var testSettings = JsonConvert.DeserializeObject<TestSettings>(jsonTestSettings);

        if (testSettings == null)
        {
            throw new ArgumentNullException($"Failed deserializing {nameof(TestSettings)}");
        }

        FileZipperTestRootPath = testSettings.ZipperTestsDirectory;
    }

    [SetUp]
    public void Setup()
    {
        if (string.IsNullOrEmpty(FileZipperTestRootPath))
            throw new ArgumentNullException($"{nameof(FileZipperTestRootPath)} cannot be null");

        Directory.CreateDirectory(FileZipperTestRootPath);
        Directory.CreateDirectory(ZippedFilesDirectoryPath);
        Directory.CreateDirectory(UnzippedFilesDirectoryPath);
    }

    [TearDown]
    public void Cleanup()
    {
        Directory.Delete(FileZipperTestRootPath, true);
    }

    protected abstract IFileZipper CreateFileZipper();

    protected void PrepareFileStructure(TestFileStructure testFileStructure, Action<IFileZipper> addFilesToFileZipper)
    {
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        using var fileZipper = CreateFileZipper();
        addFilesToFileZipper(fileZipper);
        if (fileZipper.HasToBeSaved)
            fileZipper.Save(ZipFilePath, true);
        UnzipFile();
    }

    private void UnzipFile()
    {
        ZipFile.ExtractToDirectory(ZipFilePath, UnzippedFilesDirectoryPath);
    }

    [Test]
    public void GivenFile_WhenAddingToFileZipper_ThenUnzippedFilesDirectoryShouldContainAddedFile()
    {
        var testFile = new TestFile("file1");
        using var testFileStructure = new TestFileStructure(
            new() { testFile },
            new());

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddFile(testFile);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenFileWithCustomZipPath_WhenAddingToFileZipper_ThenUnzippedFilesDirectoryShouldContainAddedFileUnderGivenZipPath()
    {
        var testFile = new TestFile("file1", "zip-dir1");
        using var testFileStructure = new TestFileStructure([testFile], []);

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddFile(testFile);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenEmptyDirectory_WhenAddingToFileZipper_ThenUnzippedFilesDirectoryShouldContainAddedEmptyDirectory()
    {
        var testDirectory = new TestDirectory("dir1", [], []);
        using var testFileStructure = new TestFileStructure([], [testDirectory]);

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectory(testDirectory);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenEmptyDirectory_WhenAddingToFileZipper_ThenUnzippedFilesDirectoryShouldContainAddedEmptyDirectoryUnderGivenZipPath()
    {
        var testDirectory = new TestDirectory("dir1", [], [], "zip-dir1");
        using var testFileStructure = new TestFileStructure([], [testDirectory]);

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectory(testDirectory);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenMultipleFiles_WhenAddingDirectoryContent_ThenUnzippedFilesDirectoryShouldContainAddedFiles()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2");
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], []);

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectoryContent(testFileStructure);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenComplexFileStructure_WhenAddingDirectoryContent_ThenUnzippedFilesDirectoryShouldContainExactSameFileStructure()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2");
        var testFile3 = new TestFile("file3");
        var testFile4 = new TestFile("file4");
        var testFile5 = new TestFile("file5");
        var testDirectory1 = new TestDirectory("dir1", [testFile3, testFile4], []);
        var testDirectory2 = new TestDirectory("dir2", [testFile5], []);
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], [testDirectory1, testDirectory2]);

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectoryContent(testFileStructure);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }

    [Test]
    public void GivenComplexFileStructureWithCustomZipPaths_WhenAddingFileStructure_ThenUnzippedFilesDirectoryShouldContainFileStructureIncludingAddedZipPaths()
    {
        var testFile1 = new TestFile("file1");
        var testFile2 = new TestFile("file2", "zip-dir1");
        var testFile3 = new TestFile("file3");
        var testFile4 = new TestFile("file4");
        var testFile5 = new TestFile("file5");
        var testDirectory1 = new TestDirectory("dir1", [testFile3, testFile4], []);
        var testDirectory2 = new TestDirectory("dir2", [testFile5], [], "zip-dir2");
        using var testFileStructure = new TestFileStructure([testFile1, testFile2], [testDirectory1, testDirectory2]);

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddTopLevelFilesAndDirectoriesSeparately(testFileStructure);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }
}