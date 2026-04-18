using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.SMB;
using BackupHelper.Core.Sinks;
using BackupHelper.Sinks.FileSystem;
using BackupHelper.Sinks.SMB;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.SMB.Tests.Sink;

[TestFixture]
public class SMBSinkFactoryTests : TestsBase
{
    private const string Server = "192.168.1.10";
    private const string ShareName = "backup-share";
    private const string Username = "backup-user";
    private const string Password = "secret";

    protected override void AddCredentials(
        TestCredentialsProvider credentialsProvider,
        IConfiguration configuration
    )
    {
        base.AddCredentials(credentialsProvider, configuration);

        using var password = new SensitiveString(Password);
        using var smbCredential = new SMBCredential(Server, ShareName, Username, password);
        credentialsProvider.SetCredential(smbCredential.ToCredentialEntry());
    }

    [Test]
    public void GivenSMBSinkDestination_WhenResolvedBySinkManager_ThenSMBSinkFactoryIsUsed()
    {
        var credentialsProvider =
            ServiceScope.ServiceProvider.GetRequiredService<ICredentialsProvider>();

        var sinkManager = new SinkManager(
            [new FileSystemSinkFactory(), new SMBSinkFactory(credentialsProvider)]
        );

        using var sink = sinkManager.GetSink(
            new SMBSinkDestination(Server, ShareName, "nightly")
        );

        sink.ShouldBeOfType<SMBSink>();
        sink.Destination.Kind.ShouldBe(SMBSinkDestination.SinkKind);
    }

    [Test]
    public void GivenMissingSMBCredential_WhenCreatingSink_ThenFactoryThrowsKeyNotFoundException()
    {
        var credentialsProvider =
            ServiceScope.ServiceProvider.GetRequiredService<ICredentialsProvider>();
        var sinkFactory = new SMBSinkFactory(credentialsProvider);

        var exception = Should.Throw<KeyNotFoundException>(() =>
            sinkFactory.CreateSink(new SMBSinkDestination(Server, "missing-share", ""))
        );

        exception.Message.ShouldContain("missing-share");
    }
}
