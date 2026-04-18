using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using BackupHelper.Api.Features;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Sinks;
using BackupHelper.Sinks.Abstractions;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class UploadBackupToSinkParallelismTests : ZipTestsBase
{
    private const int LargeSourceFileSizeMb = 96;
    private const int SinkCount = 3;
    private const int CopyChunkSizeBytes = 1024 * 1024;
    private static readonly TimeSpan ChunkDelay = TimeSpan.FromMilliseconds(30);

    [Test]
    public async Task GivenLargeBackupZip_WhenUploadingToMultipleSinksInParallel_ThenUploadsAreSafeAndConcurrent()
    {
        // Arrange: create a large, incompressible backup zip to exercise sustained sink uploads.
        using var testFileStructure = new TestFileStructure(
            [new TestFile("parallel-upload-source.bin", fileSizeInMb: LargeSourceFileSizeMb)],
            []
        );
        testFileStructure.Generate(ZippedFilesDirectoryPath, UnzippedFilesDirectoryPath);
        var sourceFilePath = testFileStructure.Files.Single().GeneratedFilePath;

        var backupPlan = new BackupPlan
        {
            CompressionLevel = 0,
            Items = [new BackupFileEntry { FilePath = sourceFilePath, CompressionLevel = 0 }],
        };

        var backupPlanZipper = ServiceScope.ServiceProvider.GetRequiredService<IBackupPlanZipper>();
        await backupPlanZipper.CreateZipFileAsync(backupPlan, ZipFilePath);

        var zipSizeBytes = new FileInfo(ZipFilePath).Length;
        zipSizeBytes.ShouldBeGreaterThan(50L * 1024 * 1024);

        var sinkDestinations = Enumerable
            .Range(1, SinkCount)
            .Select(index =>
                new SlowCopySinkDestination(
                    $"parallel-sink-{index}",
                    Path.Combine(TestsDirectoryRootPath, $"upload-target-{index}")
                )
            )
            .ToList();

        foreach (var sinkDestination in sinkDestinations)
        {
            Directory.CreateDirectory(sinkDestination.DestinationDirectory);
        }

        var concurrencyTracker = new ConcurrencyTracker();
        var sinks = sinkDestinations.ToDictionary(
            sinkDestination => sinkDestination.Kind,
            sinkDestination => (ISink)new SlowCopySink(sinkDestination, concurrencyTracker)
        );

        var sinkManager = new TestSinkManager(sinks);
        var handler = new UploadBackupToSinkCommandHandler(sinkManager);
        var uploadResults = new ConcurrentBag<BackupSinkUploadResult>();

        // Act: upload to all sinks concurrently, matching sink parallelism behavior.
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(
            sinkDestinations,
            new ParallelOptions { MaxDegreeOfParallelism = SinkCount },
            async (sinkDestination, cancellationToken) =>
            {
                var uploadResult = await handler.Handle(
                    new UploadBackupToSinkCommand(sinkDestination, ZipFilePath),
                    cancellationToken
                );
                uploadResults.Add(uploadResult);
            }
        );

        stopwatch.Stop();

        // Assert: all uploads succeeded, ran concurrently, took a few seconds, and produced identical bytes.
        uploadResults.Count.ShouldBe(SinkCount);
        uploadResults.All(result => result.Status == BackupSinkUploadStatus.Uploaded).ShouldBeTrue();
        concurrencyTracker.MaxObservedConcurrency.ShouldBeGreaterThan(1);
        (stopwatch.Elapsed >= TimeSpan.FromSeconds(2)).ShouldBeTrue();

        sinks.Values.OfType<SlowCopySink>().All(sink => sink.WasDisposed).ShouldBeTrue();

        var sourceHash = await ComputeSha256HexAsync(ZipFilePath);

        foreach (var sinkDestination in sinkDestinations)
        {
            var uploadedZipPath = Path.Combine(
                sinkDestination.DestinationDirectory,
                Path.GetFileName(ZipFilePath)
            );

            File.Exists(uploadedZipPath).ShouldBeTrue();
            var uploadedHash = await ComputeSha256HexAsync(uploadedZipPath);
            uploadedHash.ShouldBe(sourceHash);
        }
    }

    private static async Task<string> ComputeSha256HexAsync(string filePath)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );
        using var sha256 = SHA256.Create();

        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash);
    }

    private sealed record SlowCopySinkDestination(string Kind, string DestinationDirectory)
        : ISinkDestination;

    private sealed class TestSinkManager : ISinkManager
    {
        private readonly IReadOnlyDictionary<string, ISink> _sinks;

        public TestSinkManager(IReadOnlyDictionary<string, ISink> sinks)
        {
            _sinks = sinks;
        }

        public ISink GetSink(ISinkDestination destination)
        {
            return _sinks[destination.Kind];
        }
    }

    private sealed class SlowCopySink : ISink
    {
        private readonly SlowCopySinkDestination _destination;
        private readonly ConcurrencyTracker _concurrencyTracker;

        public SlowCopySink(
            SlowCopySinkDestination destination,
            ConcurrencyTracker concurrencyTracker
        )
        {
            _destination = destination;
            _concurrencyTracker = concurrencyTracker;
        }

        public ISinkDestination Destination => _destination;
        public string Description => $"Slow copy sink to {_destination.DestinationDirectory}";
        public bool WasDisposed { get; private set; }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Directory.Exists(_destination.DestinationDirectory));
        }

        public async Task UploadAsync(
            string sourceFilePath,
            CancellationToken cancellationToken = default
        )
        {
            _concurrencyTracker.Enter();
            try
            {
                var destinationPath = Path.Combine(
                    _destination.DestinationDirectory,
                    Path.GetFileName(sourceFilePath)
                );

                await using var sourceStream = new FileStream(
                    sourceFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: CopyChunkSizeBytes,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan
                );

                await using var destinationStream = new FileStream(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: CopyChunkSizeBytes,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan
                );

                var buffer = new byte[CopyChunkSizeBytes];
                while (true)
                {
                    var bytesRead = await sourceStream.ReadAsync(
                        buffer.AsMemory(0, buffer.Length),
                        cancellationToken
                    );
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await destinationStream.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken
                    );

                    await Task.Delay(ChunkDelay, cancellationToken);
                }
            }
            finally
            {
                _concurrencyTracker.Exit();
            }
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }

    private sealed class ConcurrencyTracker
    {
        private int _activeUploads;
        private int _maxObservedConcurrency;

        public int MaxObservedConcurrency => Volatile.Read(ref _maxObservedConcurrency);

        public void Enter()
        {
            var activeUploads = Interlocked.Increment(ref _activeUploads);
            SetMaxObservedConcurrency(activeUploads);
        }

        public void Exit()
        {
            Interlocked.Decrement(ref _activeUploads);
        }

        private void SetMaxObservedConcurrency(int activeUploads)
        {
            while (true)
            {
                var currentMax = Volatile.Read(ref _maxObservedConcurrency);
                if (activeUploads <= currentMax)
                {
                    return;
                }

                if (
                    Interlocked.CompareExchange(
                        ref _maxObservedConcurrency,
                        activeUploads,
                        currentMax
                    ) == currentMax
                )
                {
                    return;
                }
            }
        }
    }
}