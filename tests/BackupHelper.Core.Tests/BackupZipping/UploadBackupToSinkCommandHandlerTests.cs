using BackupHelper.Api.Features;
using BackupHelper.Core.Sinks;
using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Azure;
using BackupHelper.Sinks.FileSystem;
using BackupHelper.Sinks.SMB;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class UploadBackupToSinkCommandHandlerTests : TestsBase
{
    [Test]
    public async Task GivenAvailableSink_WhenUploadSucceeds_ThenResultIsUploadedAndSinkIsDisposed()
    {
        var sink = new TestSink(description: "sink-a", isAvailable: true);
        var sinkManager = new TestSinkManager(new Dictionary<string, ISink> { ["sink-a"] = sink });
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(new TestSinkDestination("sink-a"), "output.zip"),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasUploadCalled.ShouldBeTrue();
        sink.WasDisposed.ShouldBeTrue();
    }

    [Test]
    public async Task GivenUnavailableSink_WhenUploading_ThenResultIsSkippedAndSinkIsDisposed()
    {
        var sink = new TestSink(description: "sink-b", isAvailable: false);
        var sinkManager = new TestSinkManager(new Dictionary<string, ISink> { ["sink-b"] = sink });
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(new TestSinkDestination("sink-b"), "output.zip"),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.SkippedUnavailable);
        sink.WasUploadCalled.ShouldBeFalse();
        sink.WasDisposed.ShouldBeTrue();
    }

    [Test]
    public async Task GivenFileSystemSinkWithMissingDestinationDirectory_WhenUploading_ThenDirectoryIsCreatedAndUploadSucceeds()
    {
        var sourceDirectory = Path.Join(TestsDirectoryRootPath, "source");
        Directory.CreateDirectory(sourceDirectory);
        var sourceFilePath = Path.Join(sourceDirectory, "output.zip");
        await File.WriteAllTextAsync(sourceFilePath, "backup-content");

        var destinationDirectory = Path.Join(TestsDirectoryRootPath, "missing-destination");

        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink>
            {
                [FileSystemSinkDestination.SinkKind] = new FileSystemSink(
                    new FileSystemSinkDestination(destinationDirectory)
                ),
            }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new FileSystemSinkDestination(destinationDirectory),
                sourceFilePath
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        Directory.Exists(destinationDirectory).ShouldBeTrue();
        File.Exists(Path.Join(destinationDirectory, Path.GetFileName(sourceFilePath))).ShouldBeTrue();
    }

    [Test]
    public async Task GivenAvailableSinkThatThrows_WhenUploading_ThenResultIsFailedAndSinkIsDisposed()
    {
        var sink = new TestSink(
            description: "sink-c",
            isAvailable: true,
            uploadException: new InvalidOperationException("Upload failed")
        );
        var sinkManager = new TestSinkManager(new Dictionary<string, ISink> { ["sink-c"] = sink });
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(new TestSinkDestination("sink-c"), "output.zip"),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Failed);
        result.FailureMessage.ShouldBe("Upload failed");
        sink.WasDisposed.ShouldBeTrue();
    }

    [Test]
    public async Task GivenSink_WhenAvailabilityCheckThrows_ThenSinkIsDisposedAndExceptionIsPropagated()
    {
        var availabilityException = new InvalidOperationException("availability error");
        var failedSink = new TestSink(
            description: "sink-fail",
            isAvailable: true,
            availabilityException: availabilityException
        );

        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink>
            {
                ["sink-fail"] = failedSink,
            }
        );
        var handler = CreateHandler(sinkManager);

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await handler.Handle(
                new UploadBackupToSinkCommand(new TestSinkDestination("sink-fail"), "output.zip"),
                CancellationToken.None
            )
        );

        exception.Message.ShouldBe("availability error");
        failedSink.WasDisposed.ShouldBeTrue();
    }

    [Test]
    public async Task GivenPrunableSinkAndMaxBackups_WhenUploadSucceeds_ThenPruningIsInvoked()
    {
        var sink = new TestSink(description: "sink-prune", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink> { ["sink-prune"] = sink }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new TestSinkDestination("sink-prune"),
                @"C:\\tmp\\backup.zip",
                MaxBackups: 3
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeTrue();
        sink.LastPrunedUploadedFileName.ShouldBe("backup.zip");
        sink.LastPruneMaxBackups.ShouldBe(3);
    }

    [Test]
    public async Task GivenPrunableSinkAndNoMaxBackups_WhenUploadSucceeds_ThenPruningIsSkipped()
    {
        var sink = new TestSink(description: "sink-no-prune", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink> { ["sink-no-prune"] = sink }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(new TestSinkDestination("sink-no-prune"), "output.zip"),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeFalse();
    }

    [Test]
    public async Task GivenPrunableSinkAndInvalidMaxBackups_WhenUploadSucceeds_ThenPruningIsSkipped()
    {
        var sink = new TestSink(description: "sink-invalid-max", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink> { ["sink-invalid-max"] = sink }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new TestSinkDestination("sink-invalid-max"),
                "output.zip",
                MaxBackups: 0
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeFalse();
    }

    [Test]
    public async Task GivenPrunableSinkThatThrowsDuringPruning_WhenUploadSucceeds_ThenUploadStillSucceeds()
    {
        var sink = new TestSink(
            description: "sink-prune-fails",
            isAvailable: true,
            pruneException: new InvalidOperationException("Prune failed")
        );
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink> { ["sink-prune-fails"] = sink }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new TestSinkDestination("sink-prune-fails"),
                "output.zip",
                MaxBackups: 2
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasUploadCalled.ShouldBeTrue();
        sink.WasPruneCalled.ShouldBeTrue();
        sink.WasDisposed.ShouldBeTrue();
    }

    [Test]
    public async Task GivenSinkSpecificMaxBackupsOverride_WhenUploadSucceeds_ThenOverrideIsUsed()
    {
        var sink = new TestSink(description: "sink-filesystem", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink>
            {
                [FileSystemSinkDestination.SinkKind] = sink,
            }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new FileSystemSinkDestination(@"C:\\backup", MaxBackups: 2),
                "output.zip",
                MaxBackups: 5
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeTrue();
        sink.LastPruneMaxBackups.ShouldBe(2);
    }

    [Test]
    public async Task GivenSinkSpecificMaxBackupsNull_WhenUploadSucceeds_ThenPlanDefaultIsUsed()
    {
        var sink = new TestSink(description: "sink-smb", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink>
            {
                [SMBSinkDestination.SinkKind] = sink,
            }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new SMBSinkDestination("server", "share", "nightly", MaxBackups: null),
                "output.zip",
                MaxBackups: 4
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeTrue();
        sink.LastPruneMaxBackups.ShouldBe(4);
    }

    [Test]
    public async Task GivenSinkSpecificMaxBackupsZero_WhenUploadSucceeds_ThenPruningIsDisabledForSink()
    {
        var sink = new TestSink(description: "sink-azure", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink>
            {
                [AzureBlobStorageSinkDestination.SinkKind] = sink,
            }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new AzureBlobStorageSinkDestination("account", "container", MaxBackups: 0),
                "output.zip",
                MaxBackups: 6
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeFalse();
    }

    [Test]
    public async Task GivenPlanDefaultNullAndSinkOverride_WhenUploadSucceeds_ThenSinkOverrideIsUsed()
    {
        var sink = new TestSink(description: "sink-azure-override", isAvailable: true);
        var sinkManager = new TestSinkManager(
            new Dictionary<string, ISink>
            {
                [AzureBlobStorageSinkDestination.SinkKind] = sink,
            }
        );
        var handler = CreateHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(
                new AzureBlobStorageSinkDestination("account", "container", MaxBackups: 3),
                "output.zip",
                MaxBackups: null
            ),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.Uploaded);
        sink.WasPruneCalled.ShouldBeTrue();
        sink.LastPruneMaxBackups.ShouldBe(3);
    }

    private UploadBackupToSinkCommandHandler CreateHandler(ISinkManager sinkManager)
    {
        var logger = ServiceScope.ServiceProvider.GetRequiredService<
            ILogger<UploadBackupToSinkCommandHandler>
        >();

        return new UploadBackupToSinkCommandHandler(
            sinkManager,
            logger
        );
    }

    private sealed record TestSinkDestination(string Kind) : ISinkDestination;

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

    private sealed class TestSink : ISink, IPrunableSink
    {
        private readonly bool _isAvailable;
        private readonly Exception? _uploadException;
        private readonly Exception? _availabilityException;
        private readonly Exception? _pruneException;

        public TestSink(
            string description,
            bool isAvailable,
            Exception? uploadException = null,
            Exception? availabilityException = null,
            Exception? pruneException = null
        )
        {
            Description = description;
            _isAvailable = isAvailable;
            _uploadException = uploadException;
            _availabilityException = availabilityException;
            _pruneException = pruneException;
            Destination = new TestSinkDestination(description);
        }

        public bool WasUploadCalled { get; private set; }
        public bool WasPruneCalled { get; private set; }
        public bool WasDisposed { get; private set; }
        public string? LastPrunedUploadedFileName { get; private set; }
        public int? LastPruneMaxBackups { get; private set; }

        public ISinkDestination Destination { get; }
        public string Description { get; }

        public Task UploadAsync(string sourceFilePath, CancellationToken cancellationToken = default)
        {
            WasUploadCalled = true;

            if (_uploadException != null)
            {
                throw _uploadException;
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            if (_availabilityException != null)
            {
                throw _availabilityException;
            }

            return Task.FromResult(_isAvailable);
        }

        public Task PruneBackupsAsync(
            string uploadedBackupFileName,
            int maxBackups,
            CancellationToken cancellationToken = default
        )
        {
            WasPruneCalled = true;
            LastPrunedUploadedFileName = uploadedBackupFileName;
            LastPruneMaxBackups = maxBackups;

            if (_pruneException != null)
            {
                throw _pruneException;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}