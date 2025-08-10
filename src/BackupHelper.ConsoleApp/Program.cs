using BackupHelper.Api;
using BackupHelper.ConsoleApp.Wizard;
using BackupHelper.Core;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.ConsoleApp;

internal class Program
{
    internal static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .Build();
        var serviceCollection = new ServiceCollection()
                                .AddCoreServices(configuration)
                                .AddApiServices(configuration)
                                .AddConsoleInterfaceServices(configuration)
                                .AddLogging();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        IWizardParameters? parameters = new MainMenuStepParameters();

        do
        {
            parameters = await mediator.Send(parameters);
        } while (parameters != null);
    }
}