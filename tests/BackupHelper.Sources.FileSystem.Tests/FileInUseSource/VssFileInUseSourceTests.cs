using BackupHelper.Sources.FileSystem.FileInUseSource;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Sources.FileSystem.Tests.FileInUseSource;

[TestFixture]
public class VssFileInUseSourceTests : FileInUseSourceTestsBase
{
    protected override void OverrideServices(IServiceCollection services)
    {
        services.AddTransient<VssFileInUseSourceFactory>();
    }

    protected override IFileInUseSourceFactory CreateFileInUseZipEntrySourceFactory()
        => ServiceScope.ServiceProvider.GetRequiredService<VssFileInUseSourceFactory>();
}