using System.Reflection;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.FileInUseZipEntryHandler;
using BackupHelper.Core.FileZipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core;

public static class ConfigureServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(serviceConfiguration => serviceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IBackupPlanZipper, BackupPlanZipper>();
        services.AddTransient<IFileZipperFactory, OnDiskFileZipperFactory>();
        services.AddTransient<IFileInUseZipEntryHandlerManager, FileInUseZipEntryHandlerManager>();
        services.AddTransient<VssFileInUseZipEntryHandlerFactory>();

        return services;
    }
}