using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public class InMemoryFileZipperTests : FileZipperTestsBase
{
    protected override void OverrideServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IFileZipperFactory, InMemoryFileZipperFactory>();
    }
}