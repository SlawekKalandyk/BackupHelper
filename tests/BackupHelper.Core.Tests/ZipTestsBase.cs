using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace BackupHelper.Core.Tests;

[TestFixture]
public abstract class ZipTestsBase
{
    /// <summary>
    /// Scope for services used in tests.
    /// </summary>
    protected IServiceScope ServiceScope { get; private set; }

    private ServiceProvider _serviceProvider { get; set; }

    /// <summary>
    /// Directory where files to be zipped will be stored during tests.
    /// </summary>
    protected string ZippedFilesDirectoryPath => Path.Combine(_fileZipperTestRootPath, "file-zipper-tests-zipped");

    /// <summary>
    /// Directory where unzipped files will be stored during tests.
    /// </summary>
    protected string UnzippedFilesDirectoryPath => Path.Combine(_fileZipperTestRootPath, "file-zipper-tests-unzipped");

    /// <summary>
    /// File path for the zip file used in tests.
    /// </summary>
    protected string ZipFilePath => Path.Combine(_fileZipperTestRootPath, "file-zipper-tests-zipped-file.zip");

    private string _fileZipperTestRootPath { get; set; }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var jsonTestSettings = File.ReadAllText("testSettings.json");
        var testSettings = JsonConvert.DeserializeObject<TestSettings>(jsonTestSettings);

        if (testSettings == null)
        {
            throw new ArgumentNullException($"Failed deserializing {nameof(TestSettings)}");
        }

        _fileZipperTestRootPath = testSettings.FileZipperTestsDirectory;
        _serviceProvider = CreateServiceProvider();
    }

    [OneTimeTearDown]
    public void OneTimeCleanup()
    {
        _serviceProvider?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        if (string.IsNullOrEmpty(_fileZipperTestRootPath))
            throw new ArgumentNullException($"{nameof(_fileZipperTestRootPath)} cannot be null");

        Directory.CreateDirectory(_fileZipperTestRootPath);
        Directory.CreateDirectory(ZippedFilesDirectoryPath);
        Directory.CreateDirectory(UnzippedFilesDirectoryPath);

        ServiceScope = _serviceProvider.CreateScope();
    }

    [TearDown]
    public void Cleanup()
    {
        ServiceScope.Dispose();
        Directory.Delete(_fileZipperTestRootPath, true);
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