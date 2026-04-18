using BackupHelper.Abstractions.Credentials;
using BackupHelper.Sinks.SMB;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.SMB.Tests.Sink;

[TestFixture]
public class SMBSinkIntegrationTests : SMBTestsBase
{
    private string _sinkDestinationDirectory = null!;

    [SetUp]
    protected override void Setup()
    {
        base.Setup();

        _sinkDestinationDirectory = Path.Join(
            SMBTestConfigurationProvider.TestsDirectoryName,
            "smb-sink-upload-tests"
        );

        using var smbConnection = SMBTestConfigurationProvider.GetSMBConnection();

        if (smbConnection.DirectoryExists(_sinkDestinationDirectory))
        {
            smbConnection.DeleteDirectory(_sinkDestinationDirectory);
        }
    }

    [TearDown]
    protected override void Cleanup()
    {
        using var smbConnection = SMBTestConfigurationProvider.GetSMBConnection();

        if (smbConnection.DirectoryExists(_sinkDestinationDirectory))
        {
            smbConnection.DeleteDirectory(_sinkDestinationDirectory);
        }

        base.Cleanup();
    }

    [Test]
    public async Task GivenLocalBackupFile_WhenUploadedViaSMBSink_ThenFileExistsOnSMBShare()
    {
        var sourceFilePath = Path.Combine(TestsDirectoryRootPath, "backup-to-upload.zip");
        const string expectedContent = "test-backup-content";
        await File.WriteAllTextAsync(sourceFilePath, expectedContent);

        var sinkDestination = new SMBSinkDestination(
            SMBTestConfigurationProvider.ServerAddress,
            SMBTestConfigurationProvider.ShareName,
            _sinkDestinationDirectory
        );

        var credentialsProvider =
            ServiceScope.ServiceProvider.GetRequiredService<ICredentialsProvider>();
        using var sink = new SMBSinkFactory(credentialsProvider).CreateSink(sinkDestination);

        await sink.UploadAsync(sourceFilePath, CancellationToken.None);

        var uploadedFilePath = Path.Join(
            _sinkDestinationDirectory,
            Path.GetFileName(sourceFilePath)
        );

        using var smbConnection = SMBTestConfigurationProvider.GetSMBConnection();
        smbConnection.FileExists(uploadedFilePath).ShouldBeTrue();

        await using var uploadedFileStream = smbConnection.GetStream(uploadedFilePath);
        using var streamReader = new StreamReader(uploadedFileStream);
        var uploadedFileContent = await streamReader.ReadToEndAsync();

        uploadedFileContent.ShouldBe(expectedContent);
    }
}
