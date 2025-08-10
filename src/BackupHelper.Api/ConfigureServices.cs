using System.Reflection;
using BackupHelper.Abstractions;
using BackupHelper.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Api;

public static class ConfigureServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, ICredentialsProvider credentialsProvider)
    {
        services.AddMediatR(serviceConfiguration => serviceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddCoreServices(configuration)
                .AddSingleton(_ => credentialsProvider);

        return services;
    }
}