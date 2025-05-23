﻿using System.Reflection;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.FileZipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core;

public static class ConfigureServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(serviceConfiguration => serviceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IFileZipperFactory, OnDiskFileZipperFactory>();
        services.AddSingleton<IBackupPlanZipper, BackupPlanZipper>();
        return services;
    }
}