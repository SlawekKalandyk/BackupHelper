using BackupHelper.Sources.FileSystem.FileInUseSource;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Sources.FileSystem.Tests.FileInUseSource;

[TestFixture]
public class VssFileInUseSourceTests : FileInUseSourceTestsBase
{
    protected override void OverrideServices(IServiceCollection services, IConfiguration configuration)
    {
        base.OverrideServices(services, configuration);
        services.AddTransient<VssFileInUseSourceFactory>();
    }

    protected override IFileInUseSourceFactory CreateFileInUseZipEntrySourceFactory()
        => ServiceScope.ServiceProvider.GetRequiredService<VssFileInUseSourceFactory>();
}