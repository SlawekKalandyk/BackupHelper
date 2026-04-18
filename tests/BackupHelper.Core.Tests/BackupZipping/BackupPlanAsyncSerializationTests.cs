using BackupHelper.Core.BackupZipping;
using BackupHelper.Sinks.FileSystem;
using BackupHelper.Sinks.SMB;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class BackupPlanAsyncSerializationTests
{
    [Test]
    public async Task GivenBackupPlan_WhenSerializedAndDeserializedAsync_ThenRoundTripContainsAsyncProperties()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "BackupHelper.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        var backupPlanPath = Path.Combine(testDirectory, "backup-plan.json");

        var backupPlan = new BackupPlan
        {
            Items =
            [
                new BackupDirectoryEntry
                {
                    DirectoryName = "root",
                    Items =
                    [
                        new BackupFileEntry
                        {
                            FilePath = @"C:\\example\\file.txt",
                            CompressionLevel = 5,
                            TimeZone = "local",
                        },
                    ],
                },
            ],
            Sinks =
            [
                new FileSystemSinkDestination(@"C:\\backup"),
                new SMBSinkDestination("192.168.1.10", "backup-share", "nightly"),
            ],
            LogDirectory = @"C:\\logs",
            EncryptHeaders = true,
            ThreadLimit = 2,
            MemoryLimitMB = 128,
            CompressionLevel = 6,
            ZipFileNameSuffix = "nightly",
            SinkUploadParallelism = 3,
        };

        try
        {
            await backupPlan.ToJsonFileAsync(backupPlanPath);
            var deserializedPlan = await BackupPlan.FromJsonFileAsync(backupPlanPath);

            deserializedPlan.SinkUploadParallelism.ShouldBe(3);
            deserializedPlan.LogDirectory.ShouldBe(@"C:\\logs");
            deserializedPlan.ThreadLimit.ShouldBe(2);
            deserializedPlan.Sinks.Count.ShouldBe(2);
            deserializedPlan.Sinks[0].ShouldBeOfType<FileSystemSinkDestination>();
            deserializedPlan.Sinks[1].ShouldBeOfType<SMBSinkDestination>();
            deserializedPlan.Items.Count.ShouldBe(1);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }
}