using System.Runtime.CompilerServices;
using BackupHelper.Abstractions;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Sources.SMB.Tests;

[TestFixture]
public abstract class SMBTestsBase : TestsBase
{
    protected SMBTestConfigurationProvider SMBTestConfigurationProvider { get; private set; }

    protected override void OverrideServices(IServiceCollection services, IConfiguration configuration)
    {
        base.OverrideServices(services, configuration);

        SMBTestConfigurationProvider = new SMBTestConfigurationProvider(configuration);

        var credentialsProvider = new TestCredentialsProvider();
        credentialsProvider.SetCredential(
            SMBTestConfigurationProvider.SharePath,
            SMBTestConfigurationProvider.Username,
            SMBTestConfigurationProvider.Password);
        services.AddSingleton<ICredentialsProvider>(credentialsProvider);
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