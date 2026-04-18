using System.Text.Json;
using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Abstractions.Retention;
using BackupHelper.Sinks.FileSystem;
using BackupHelper.Tests.Shared;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class FileSystemSinkPruningTests : TestsBase
{
    private static readonly JsonSerializerOptions ManifestJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private string DestinationDirectoryPath =>
        Path.Combine(TestsDirectoryRootPath, "file-system-sink-pruning-tests");

    [Test]
    public async Task GivenMoreManagedBackupsThanLimit_WhenPruning_ThenOldestManagedBackupIsDeleted()
    {
        var destinationDirectory = DestinationDirectoryPath;
        Directory.CreateDirectory(destinationDirectory);
        using var sink = new FileSystemSink(new FileSystemSinkDestination(destinationDirectory));

        var firstBackup = Path.Join(destinationDirectory, "backup-1.zip");
        var secondBackup = Path.Join(destinationDirectory, "backup-2.zip");
        var thirdBackup = Path.Join(destinationDirectory, "backup-3.zip");

        await File.WriteAllTextAsync(firstBackup, "backup-1");
        await File.WriteAllTextAsync(secondBackup, "backup-2");
        await File.WriteAllTextAsync(thirdBackup, "backup-3");

        await WriteManifestAsync(
            destinationDirectory,
            new BackupsManifest
            {
                Backups =
                [
                    new BackupManifestEntry
                    {
                        FileName = "backup-1.zip",
                        UploadedUtc = DateTime.UtcNow.AddMinutes(-3),
                    },
                    new BackupManifestEntry
                    {
                        FileName = "backup-2.zip",
                        UploadedUtc = DateTime.UtcNow.AddMinutes(-2),
                    },
                ],
            }
        );

        await sink.PruneBackupsAsync("backup-3.zip", maxBackups: 2, CancellationToken.None);

        File.Exists(firstBackup).ShouldBeFalse();
        File.Exists(secondBackup).ShouldBeTrue();
        File.Exists(thirdBackup).ShouldBeTrue();

        var manifest = await ReadManifestAsync(destinationDirectory);
        manifest.Backups.Count.ShouldBe(2);
        manifest.Backups.Select(entry => entry.FileName).ShouldContain("backup-2.zip");
        manifest.Backups.Select(entry => entry.FileName).ShouldContain("backup-3.zip");
    }

    [Test]
    public async Task GivenBackupMissingFromManifest_WhenPruning_ThenMissingManifestEntryDoesNotDeleteBackup()
    {
        var destinationDirectory = DestinationDirectoryPath;
        Directory.CreateDirectory(destinationDirectory);
        using var sink = new FileSystemSink(new FileSystemSinkDestination(destinationDirectory));

        var unmanagedBackup = Path.Join(destinationDirectory, "manual-keep.zip");
        var managedFirst = Path.Join(destinationDirectory, "managed-1.zip");
        var managedSecond = Path.Join(destinationDirectory, "managed-2.zip");

        await File.WriteAllTextAsync(unmanagedBackup, "manual");
        await File.WriteAllTextAsync(managedFirst, "managed-1");
        await File.WriteAllTextAsync(managedSecond, "managed-2");

        await WriteManifestAsync(
            destinationDirectory,
            new BackupsManifest
            {
                Backups =
                [
                    new BackupManifestEntry
                    {
                        FileName = "managed-1.zip",
                        UploadedUtc = DateTime.UtcNow.AddMinutes(-2),
                    },
                    new BackupManifestEntry
                    {
                        FileName = "managed-2.zip",
                        UploadedUtc = DateTime.UtcNow.AddMinutes(-1),
                    },
                ],
            }
        );

        await sink.PruneBackupsAsync("managed-2.zip", maxBackups: 1, CancellationToken.None);

        File.Exists(unmanagedBackup).ShouldBeTrue();
        File.Exists(managedFirst).ShouldBeFalse();
        File.Exists(managedSecond).ShouldBeTrue();

        var manifest = await ReadManifestAsync(destinationDirectory);
        manifest.Backups.Count.ShouldBe(1);
        manifest.Backups[0].FileName.ShouldBe("managed-2.zip");
    }

    private static async Task<BackupsManifest> ReadManifestAsync(string destinationDirectory)
    {
        var manifestPath = Path.Join(destinationDirectory, "backups.json");
        var manifestJson = await File.ReadAllTextAsync(manifestPath);

        return JsonSerializer.Deserialize<BackupsManifest>(
                manifestJson,
                ManifestJsonSerializerOptions
            ) ?? new BackupsManifest();
    }

    private static async Task WriteManifestAsync(
        string destinationDirectory,
        BackupsManifest manifest
    )
    {
        var manifestPath = Path.Join(destinationDirectory, "backups.json");
        var manifestJson = JsonSerializer.Serialize(manifest, ManifestJsonSerializerOptions);

        await File.WriteAllTextAsync(manifestPath, manifestJson);
    }
}
