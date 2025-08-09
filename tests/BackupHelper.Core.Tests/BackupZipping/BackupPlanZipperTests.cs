using System.IO.Compression;
using BackupHelper.Abstractions;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class BackupPlanZipperTests : ZipTestsBase
{
    private BackupPlanZipperTestsDateTimeProvider _dateTimeProvider;
    private IBackupPlanZipper _backupPlanZipper;
    private Unzipper _unzipper;

    [SetUp]
    protected override void Setup()
    {
        base.Setup();
        _dateTimeProvider = (BackupPlanZipperTestsDateTimeProvider)ServiceScope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        _backupPlanZipper = ServiceScope.ServiceProvider.GetRequiredService<IBackupPlanZipper>();

        _dateTimeProvider.Now = new DateTime(2023, 10, 1, 12, 0, 0); // Set a fixed date for tests
        _unzipper = new Unzipper(ZipFilePath, UnzippedFilesDirectoryPath);
    }

    [TearDown]
    protected override void Cleanup()
    {
        base.Cleanup();
    }

    protected override void OverrideServices(IServiceCollection services, IConfiguration configuration)
    {
        base.OverrideServices(services, configuration);
        services.AddSingleton<IDateTimeProvider, BackupPlanZipperTestsDateTimeProvider>();
    }

    private void PrepareFileStructure()
    {
        // Prepare: {ZippedFilesDirectoryPath}\dir1\file1.txt
        var dir1 = Path.Combine(ZippedFilesDirectoryPath);
        Directory.CreateDirectory(dir1);
        File.WriteAllText(Path.Combine(dir1, "file1.txt"), "file1 content");

        // Prepare: {ZippedFilesDirectoryPath}\auto-%Y-%m-%d_%H-%M
        var autoDir = Path.Combine(ZippedFilesDirectoryPath, "auto-2023-10-01_00-00");
        Directory.CreateDirectory(autoDir);
    }

    private BackupPlan CreateSampleBackupPlan()
    {
        // All paths are relative to ZippedFilesDirectoryPath
        return new BackupPlan
        {
            Items = new List<BackupEntry>
            {
                new BackupFileEntry
                {
                    FilePath = Path.Combine(ZippedFilesDirectoryPath, "auto-%Y-%m-%d_%H-%M"),
                    CronExpression = "0 0 * * *",
                    TimeZone = "local"
                },
                new BackupDirectoryEntry
                {
                    DirectoryName = "dir1",
                    Items = new List<BackupEntry>
                    {
                        new BackupFileEntry
                        {
                            FilePath = Path.Combine(ZippedFilesDirectoryPath, "file1.txt")
                        }
                    }
                }
            }
        };
    }

    [Test]
    public void GivenBackupEntryWithBackupOnlyDirectory_WhenZipFileIsUnzipped_ThenUnzippedFileIsInThatDirectory()
    {
        // Arrange
        PrepareFileStructure();
        var backupPlan = CreateSampleBackupPlan();

        // Act
        _backupPlanZipper.CreateZipFile(backupPlan, ZipFilePath);
        _unzipper.UnzipFile();

        // Assert
        Assert.That(File.Exists(Path.Combine(UnzippedFilesDirectoryPath, "dir1", "file1.txt")));
    }

    [Test]
    public void GivenBackupEntryWithCronBasedDirectory_WhenZipFileIsUnzipped_ThenUnzippedDirectoryIsResolvedWithDate()
    {
        // Arrange
        PrepareFileStructure();
        var backupPlan = CreateSampleBackupPlan();

        // Act
        _backupPlanZipper.CreateZipFile(backupPlan, ZipFilePath);
        _unzipper.UnzipFile();

        // Assert
        Assert.That(Directory.Exists(Path.Combine(UnzippedFilesDirectoryPath, "auto-2023-10-01_00-00")));
    }
}