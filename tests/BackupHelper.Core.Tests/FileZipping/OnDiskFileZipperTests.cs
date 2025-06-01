using BackupHelper.Core.FileZipping;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public class OnDiskFileZipperTests : FileZipperTestsBase
{
    protected override void OverrideServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IFileZipperFactory, OnDiskFileZipperFactory>();
    }
}