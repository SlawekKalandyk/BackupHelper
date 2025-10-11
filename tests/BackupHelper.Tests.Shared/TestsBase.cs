using BackupHelper.Abstractions;
using BackupHelper.Api;
using BackupHelper.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace BackupHelper.Tests.Shared;

[TestFixture]
public abstract class TestsBase
{
    /// <summary>
    /// Scope for services used in tests.
    /// </summary>
    protected IServiceScope ServiceScope { get; private set; }

    /// <summary>
    /// Directory root path for files used in tests.
    /// </summary>
    protected string TestsDirectoryRootPath { get; set; }

    private ServiceProvider ServiceProvider { get; set; }

    [OneTimeSetUp]
    protected virtual void OneTimeSetup()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<TestsBase>().Build();

        var testsDirectoryPath = configuration["TestsDirectory"];

        if (string.IsNullOrEmpty(testsDirectoryPath))
        {
            throw new ArgumentNullException(
                $"{nameof(testsDirectoryPath)} cannot be null or empty"
            );
        }

        TestsDirectoryRootPath = testsDirectoryPath;
        ServiceProvider = CreateServiceProvider(configuration)!;
    }

    [OneTimeTearDown]
    protected virtual void OneTimeCleanup()
    {
        ServiceProvider?.Dispose();
    }

    [SetUp]
    protected virtual void Setup()
    {
        if (string.IsNullOrEmpty(TestsDirectoryRootPath))
            throw new ArgumentNullException($"{nameof(TestsDirectoryRootPath)} cannot be null");

        Directory.CreateDirectory(TestsDirectoryRootPath);
        ServiceScope = ServiceProvider.CreateScope();
    }

    [TearDown]
    protected virtual void Cleanup()
    {
        ServiceScope.Dispose();
        Directory.Delete(TestsDirectoryRootPath, true);
    }

    protected virtual void OverrideServices(
        IServiceCollection services,
        IConfiguration configuration
    ) { }

    protected virtual void AddCredentials(
        TestCredentialsProvider credentialsProvider,
        IConfiguration configuration
    ) { }

    private ServiceProvider CreateServiceProvider(IConfiguration configuration)
    {
        var credentialsProviderFactory = new TestCredentialsProviderFactory();
        var serviceCollection = new ServiceCollection()
            .AddCoreServices(configuration)
            .AddApiServices(configuration)
            .AddSingleton<ICredentialsProviderFactory>(credentialsProviderFactory)
            .AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        OverrideServices(serviceCollection, configuration);
        AddCredentials(credentialsProviderFactory.TestCredentialsProvider, configuration);

        return serviceCollection.BuildServiceProvider();
    }
}
