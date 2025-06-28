using System.Runtime.CompilerServices;
using BackupHelper.Abstractions;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Sources.SMB.Tests;

[TestFixture]
public abstract class SMBTestsBase : TestsBase
{
    protected string SMBTestsDirectoryPath => Path.Combine(SharePath, TestsDirectoryName);
    protected string TestsDirectoryName { get; set; }
    private string SharePath { get; set; }
    private string ServerAddress { get; set; }
    private string ShareName { get; set; }
    private string Username { get; set; }
    private string Password { get; set; }

    protected override void OverrideServices(IServiceCollection services, IConfiguration configuration)
    {
        base.OverrideServices(services, configuration);

        ServerAddress = configuration["SMB:server"];
        ThrowIfNullOrEmpty(ServerAddress);

        ShareName = configuration["SMB:share"];
        ThrowIfNullOrEmpty(ShareName);

        Username = configuration["SMB:username"];
        ThrowIfNullOrEmpty(Username);

        Password = configuration["SMB:password"];
        ThrowIfNullOrEmpty(Password);

        TestsDirectoryName = configuration["SMB:testsDirectory"];
        ThrowIfNullOrEmpty(TestsDirectoryName);

        SharePath = $@"\\{ServerAddress}\{ShareName}";

        var credentialsProvider = new TestCredentialsProvider();
        credentialsProvider.SetCredential(SharePath, Username, Password);
        services.AddSingleton<ICredentialsProvider>(credentialsProvider);
    }

    private void ThrowIfNullOrEmpty(string? value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(name, $"{name} cannot be null or empty");
    }

    [SetUp]
    protected override void Setup()
    {
        base.Setup();

        using (var smbConnection = GetSMBConnection())
        {
            smbConnection.CreateDirectory(TestsDirectoryName);
        }
    }

    [TearDown]
    protected override void Cleanup()
    {
        using (var smbConnection = GetSMBConnection())
        {
            smbConnection.DeleteDirectory(TestsDirectoryName);
        }

        base.Cleanup();
    }

    protected SMBConnection GetSMBConnection()
    {
        return new SMBConnection(ServerAddress, string.Empty, ShareName, Username, Password);
    }
}