using BackupHelper.Core.FileZipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public class OnDiskFileZipperTests : FileZipperTestsBase
{
    protected override void OverrideServices(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        base.OverrideServices(services, configuration);
        services.AddTransient<IFileZipperFactory, OnDiskFileZipperFactory>();
    }
}
