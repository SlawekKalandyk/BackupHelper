using System.IO.Compression;
using BackupHelper.Core.FileInUseZipEntryHandler;

namespace BackupHelper.Core.Tests.FileInUseZipEntryHandler;

[TestFixture]
public abstract class FileInUseZipEntryHandlerTestsBase : ZipTestsBase
{
    private IFileInUseZipEntryHandler _fileInUseZipEntryHandler;

    protected abstract IFileInUseZipEntryHandlerFactory CreateFileInUseZipEntryHandlerFactory();

    [SetUp]
    public void SetUp()
    {
        var fileInUseZipEntryHandlerFactory = CreateFileInUseZipEntryHandlerFactory();
        _fileInUseZipEntryHandler = fileInUseZipEntryHandlerFactory.Create();
    }

    [TearDown]
    public void CleanUp()
    {
        _fileInUseZipEntryHandler.Dispose();
    }

    [Test]
    public void GivenFileInUse_WhenFileAddedToZipArchive_ThenNoExceptionThrown()
    {
        var fileName = "file1";
        var testFile = new TestFile(fileName);
        using var testFileStructure = new TestFileStructure([testFile], []);
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        using var memoryStream = new MemoryStream();
        using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: false);
        using var file = new FileStream(testFile.GeneratedFilePath, FileMode.Open, FileAccess.Read, FileShare.None);

        _fileInUseZipEntryHandler.AddEntry(zipArchive, testFile.GeneratedFilePath, fileName, CompressionLevel.Optimal);
    }
}