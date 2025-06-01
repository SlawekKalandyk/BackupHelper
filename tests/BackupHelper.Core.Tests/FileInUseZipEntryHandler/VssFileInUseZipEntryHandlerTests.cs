using BackupHelper.Core.FileInUseZipEntryHandler;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileInUseZipEntryHandler;

[TestFixture]
public class VssFileInUseZipEntryHandlerTests : FileInUseZipEntryHandlerTestsBase
{
    protected override void OverrideServices(IServiceCollection services)
    {
        services.AddTransient<VssFileInUseZipEntryHandlerFactory>();
    }

    protected override IFileInUseZipEntryHandlerFactory CreateFileInUseZipEntryHandlerFactory()
        => ServiceScope.ServiceProvider.GetRequiredService<VssFileInUseZipEntryHandlerFactory>();
}