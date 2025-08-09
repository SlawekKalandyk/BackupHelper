using System.Reflection;
using BackupHelper.Abstractions;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Sources;
using BackupHelper.Core.Utilities;
using BackupHelper.Sources.Abstractions;
using BackupHelper.Sources.FileSystem;
using BackupHelper.Sources.FileSystem.FileInUseSource;
using BackupHelper.Sources.SMB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core;

public static class ConfigureServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(serviceConfiguration => serviceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSources();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IBackupPlanZipper, BackupPlanZipper>();
        services.AddTransient<IFileZipperFactory, OnDiskFileZipperFactory>();

        return services;
    }

    private static IServiceCollection AddSources(this IServiceCollection services)
    {
        services.AddTransient<ISourceManager, SourceManager>();
        services.AddTransient<ISource, FileSystemSource>();
        services.AddTransient<ISource, SMBSource>();

        services.AddTransient<IFileInUseSourceManager, FileInUseSourceManager>();
        services.AddTransient<VssFileInUseSourceFactory>();

        return services;
    }
}