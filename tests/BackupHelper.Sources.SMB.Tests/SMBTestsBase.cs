using BackupHelper.Tests.Shared;
using Microsoft.Extensions.Configuration;

namespace BackupHelper.Sources.SMB.Tests;

[TestFixture]
public abstract class SMBTestsBase : TestsBase
{
    protected SMBTestConfigurationProvider SMBTestConfigurationProvider { get; private set; }

    protected override void AddCredentials(TestCredentialsProvider credentialsProvider, IConfiguration configuration)
    {
        base.AddCredentials(credentialsProvider, configuration);

        SMBTestConfigurationProvider = new SMBTestConfigurationProvider(configuration);
        credentialsProvider.SetCredential(
            SMBTestConfigurationProvider.SharePath,
            SMBTestConfigurationProvider.Username,
            SMBTestConfigurationProvider.Password);
    }

    [SetUp]
    protected override void Setup()
    {
        base.Setup();

        using (var smbConnection = SMBTestConfigurationProvider.GetSMBConnection())
        {
            smbConnection.CreateDirectory(SMBTestConfigurationProvider.TestsDirectoryName);
        }
    }

    [TearDown]
    protected override void Cleanup()
    {
        using (var smbConnection = SMBTestConfigurationProvider.GetSMBConnection())
        {
            smbConnection.DeleteDirectory(SMBTestConfigurationProvider.TestsDirectoryName);
        }

        base.Cleanup();
    }
}