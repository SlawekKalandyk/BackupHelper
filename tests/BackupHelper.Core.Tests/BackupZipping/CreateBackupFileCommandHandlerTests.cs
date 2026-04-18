using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features;
using BackupHelper.Core.BackupZipping;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Tests.BackupZipping;

[TestFixture]
public class CreateBackupFileCommandHandlerTests
{
    [Test]
    public async Task GivenCreateBackupCommand_WhenHandled_ThenUsesAsyncBackupZipper()
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var zipper = new TestBackupPlanZipper();
        var handler = new CreateBackupFileCommandHandler(
            loggerFactory.CreateLogger<CreateBackupFileCommandHandler>(),
            zipper
        );

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "BackupHelper.Tests",
            Guid.NewGuid().ToString("N")
        );
        using var password = new SensitiveString("test-password");
        var backupPlan = new BackupPlan();
        using var cts = new CancellationTokenSource();

        try
        {
            var result = await handler.Handle(
                new CreateBackupFileCommand(backupPlan, outputDirectory, password),
                cts.Token
            );

            zipper.AsyncCalled.ShouldBeTrue();
            zipper.ReceivedPlan.ShouldBe(backupPlan);
            zipper.ReceivedPassword.ShouldBe(password);
            zipper.ReceivedCancellationToken.ShouldBe(cts.Token);

            result.OutputFilePath.ShouldStartWith(outputDirectory);
            Directory.Exists(outputDirectory).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private sealed class TestBackupPlanZipper : IBackupPlanZipper
    {
        public bool AsyncCalled { get; private set; }
        public BackupPlan? ReceivedPlan { get; private set; }
        public SensitiveString? ReceivedPassword { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task CreateZipFileAsync(
            BackupPlan plan,
            string outputFilePath,
            SensitiveString? password = null,
            CancellationToken cancellationToken = default
        )
        {
            AsyncCalled = true;
            ReceivedPlan = plan;
            ReceivedPassword = password;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}