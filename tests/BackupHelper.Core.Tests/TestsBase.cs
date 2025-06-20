using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace BackupHelper.Core.Tests;

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
        var jsonTestSettings = File.ReadAllText("testSettings.json");
        var testSettings = JsonConvert.DeserializeObject<TestSettings>(jsonTestSettings);

        if (testSettings == null)
        {
            throw new ArgumentNullException($"Failed deserializing {nameof(TestSettings)}");
        }

        TestsDirectoryRootPath = testSettings.TestsDirectory;
        ServiceProvider = CreateServiceProvider()!;
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

    protected abstract void OverrideServices(IServiceCollection services);

    private ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .Build();
        var serviceCollection = new ServiceCollection()
                                .AddCoreServices(configuration)
                                .AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        OverrideServices(serviceCollection);

        return serviceCollection.BuildServiceProvider();
    }
}