using BackupHelper.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests;

[TestFixture]
public abstract class ZipTestsBase : TestsBase
{
    /// <summary>
    /// Directory where files to be zipped will be stored during tests.
    /// </summary>
    protected string ZippedFilesDirectoryPath => Path.Combine(TestsDirectoryRootPath, "file-zipper-tests-zipped");

    /// <summary>
    /// Directory where unzipped files will be stored during tests.
    /// </summary>
    protected string UnzippedFilesDirectoryPath => Path.Combine(TestsDirectoryRootPath, "file-zipper-tests-unzipped");

    /// <summary>
    /// File path for the zip file used in tests.
    /// </summary>
    protected string ZipFilePath => Path.Combine(TestsDirectoryRootPath, "file-zipper-tests-zipped-file.zip");

    [SetUp]
    protected override void Setup()
    {
        base.Setup();

        Directory.CreateDirectory(ZippedFilesDirectoryPath);
        Directory.CreateDirectory(UnzippedFilesDirectoryPath);
    }

    [TearDown]
    protected override void Cleanup()
    {
        Directory.Delete(ZippedFilesDirectoryPath, true);
        Directory.Delete(UnzippedFilesDirectoryPath, true);

        base.Cleanup();
    }
}