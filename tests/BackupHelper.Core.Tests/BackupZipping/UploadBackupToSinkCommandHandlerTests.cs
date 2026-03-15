using BackupHelper.Api.Features;
using BackupHelper.Core.Sinks;
using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class UploadBackupToSinkCommandHandlerTests
{
    [Test]
    public async Task GivenAvailableSink_WhenUploadSucceeds_ThenResultIsUploadedAndSinkIsDisposed()
    {
        var sink = new TestSink(description: "sink-a", isAvailable: true);
        var sinkManager = new TestSinkManager(new Dictionary<string, ISink> { ["sink-a"] = sink });
        var handler = new UploadBackupToSinkCommandHandler(sinkManager);

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
        var handler = new UploadBackupToSinkCommandHandler(sinkManager);

        var result = await handler.Handle(
            new UploadBackupToSinkCommand(new TestSinkDestination("sink-b"), "output.zip"),
            CancellationToken.None
        );

        result.Status.ShouldBe(BackupSinkUploadStatus.SkippedUnavailable);
        sink.WasUploadCalled.ShouldBeFalse();
        sink.WasDisposed.ShouldBeTrue();
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
        var handler = new UploadBackupToSinkCommandHandler(sinkManager);

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
        var handler = new UploadBackupToSinkCommandHandler(sinkManager);

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await handler.Handle(
                new UploadBackupToSinkCommand(new TestSinkDestination("sink-fail"), "output.zip"),
                CancellationToken.None
            )
        );

        exception.Message.ShouldBe("availability error");
        failedSink.WasDisposed.ShouldBeTrue();
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

    private sealed class TestSink : ISink
    {
        private readonly bool _isAvailable;
        private readonly Exception? _uploadException;
        private readonly Exception? _availabilityException;

        public TestSink(
            string description,
            bool isAvailable,
            Exception? uploadException = null,
            Exception? availabilityException = null
        )
        {
            Description = description;
            _isAvailable = isAvailable;
            _uploadException = uploadException;
            _availabilityException = availabilityException;
            Destination = new TestSinkDestination(description);
        }

        public bool WasUploadCalled { get; private set; }
        public bool WasDisposed { get; private set; }

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

        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}