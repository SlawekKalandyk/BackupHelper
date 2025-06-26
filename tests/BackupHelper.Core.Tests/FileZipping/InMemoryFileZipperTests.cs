using BackupHelper.Core.FileZipping;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public class InMemoryFileZipperTests : FileZipperTestsBase
{
    protected override void OverrideServices(IServiceCollection services)
    {
        services.AddTransient<IFileZipperFactory, InMemoryFileZipperFactory>();
    }
}