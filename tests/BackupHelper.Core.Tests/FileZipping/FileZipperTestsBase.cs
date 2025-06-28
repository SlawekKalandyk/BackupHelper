using System.IO.Compression;
using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Tests.Extensions;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public abstract class FileZipperTestsBase : ZipTestsBase
{
    protected IFileZipper CreateFileZipper()
    {
        var fileZipperFactory = ServiceScope.ServiceProvider.GetRequiredService<IFileZipperFactory>();
        return fileZipperFactory.Create(ZipFilePath, true);
    }

    protected void PrepareFileStructure(TestFileStructure testFileStructure, Action<IFileZipper> addFilesToFileZipper)
    {
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        using (var fileZipper = CreateFileZipper())
        {
            addFilesToFileZipper(fileZipper);
            if (fileZipper.HasToBeSaved)
                fileZipper.Save();
        }

        UnzipFile();
    }

    private void UnzipFile()
    {
        ZipFile.ExtractToDirectory(ZipFilePath, UnzippedFilesDirectoryPath);
    }

    [Test]
    public void GivenSingleFile_WhenAddedToZip_ThenUnzippedDirectoryContainsThatFile()
    {
        var testFile = new TestFile("file1");
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
    public void GivenFileWithCustomZipPath_WhenAddedToZip_ThenUnzippedFileIsInSpecifiedSubdirectory()
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
    public void GivenEmptyDirectory_WhenAddedToZip_ThenUnzippedDirectoryContainsEmptySubdirectory()
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
    public void GivenEmptyDirectoryWithCustomZipPath_WhenAddedToZip_ThenUnzippedDirectoryContainsSpecifiedEmptySubdirectory()
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
    public void GivenMultipleFiles_WhenDirectoryContentAddedToZip_ThenUnzippedDirectoryContainsAllFiles()
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

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddDirectoryContent(testFileStructure);
            });

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

        PrepareFileStructure(
            testFileStructure,
            fileZipper =>
            {
                fileZipper.AddTopLevelFilesAndDirectoriesSeparately(testFileStructure);
            });

        testFileStructure.AssertCorrectUnzippedFileStructure();
    }
}