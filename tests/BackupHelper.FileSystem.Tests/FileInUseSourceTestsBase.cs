using BackupHelper.Core.Tests;
using BackupHelper.Sources.FileSystem.FileInUseSource;
using BackupHelper.Tests.Shared;

namespace BackupHelper.FileSystem.Tests;

[TestFixture]
public abstract class FileInUseSourceTestsBase : ZipTestsBase
{
    private IFileInUseSource _fileInUseSource;

    protected abstract IFileInUseSourceFactory CreateFileInUseZipEntrySourceFactory();

    [SetUp]
    protected override void Setup()
    {
        base.Setup();

        var fileInUseZipEntryHandlerFactory = CreateFileInUseZipEntrySourceFactory();
        _fileInUseSource = fileInUseZipEntryHandlerFactory.Create();
    }

    [TearDown]
    protected override void Cleanup()
    {
        _fileInUseSource.Dispose();

        base.Cleanup();
    }

    [Test]
    public async Task GivenFileInUse_WhenGettingStream_ThenNoExceptionThrown()
    {
        var fileName = "file1";
        var testFile = new TestFile(fileName);
        using var testFileStructure = new TestFileStructure([testFile], []);
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);

        using var file = new FileStream(
            testFile.GeneratedFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None
        );

        await using var fileInUseStream = await _fileInUseSource.GetStreamAsync(
            testFile.GeneratedFilePath
        );
    }
}
