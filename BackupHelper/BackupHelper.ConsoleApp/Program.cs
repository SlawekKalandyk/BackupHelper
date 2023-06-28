using BackupHelper.Core;
using BackupHelper.Core.Features;
using BackupHelper.Core.FileZipping;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackupHelper.ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            var services = new ServiceCollection()
                .AddCoreServices(configuration)
                .BuildServiceProvider();

            var backupSavePath = args[0];
            var backupConfigPath = args[1];
            var backupConfiguration = BackupConfiguration.FromJsonFile(backupConfigPath);

            var mediator = services.GetRequiredService<IMediator>();
            await mediator.Send(new CreateBackupCommand(backupConfiguration, backupSavePath));
        }
    }
}