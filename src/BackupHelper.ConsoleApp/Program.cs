using BackupHelper.Api;
using BackupHelper.ConsoleApp.Wizard;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Credentials;
using BackupHelper.Core.Features;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace BackupHelper.ConsoleApp;

internal class Program
{
    internal static async Task Main(string[] args)
    {
        //first arg - save paths separated by semicolon, e.g. "C:\Users\user\Documents;C:\Users\user\Pictures"
        //second arg - path to backup configuration file, e.g. "C:\Users\user\Documents\backup.json"
        //third arg - path to KeePass database file, e.g. "C:\Users\user\Documents\credentials.kdbx"

        // if (args.Length != 3 || args.Any(string.IsNullOrWhiteSpace))
        // {
        //     throw new ArgumentException();
        // }
        //
        // var savePath = args[0];
        // var backupPlanFilePath = args[1];
        // var credentialsDatabaseFilePath = args[2];
        // var backupPlan = BackupPlan.FromJsonFile(backupPlanFilePath);
        //
        // var configuration = new ConfigurationBuilder()
        //                     .AddCommandLine(args)
        //                     .Build();
        // var credentialsDatabasePassword = "test";//ConsoleHelper.PromptCredentialsDatabasePassword();
        // var keepassCredentialsProvider = new KeePassCredentialsProvider(credentialsDatabaseFilePath, credentialsDatabasePassword);
        // var serviceCollection = new ServiceCollection()
        //     .AddApiServices(configuration, keepassCredentialsProvider);
        //
        // if (!string.IsNullOrEmpty(backupPlan.LogDirectory))
        // {
        //     Directory.CreateDirectory(backupPlan.LogDirectory);
        //     serviceCollection.AddLogging(backupPlan.LogDirectory);
        // }
        //
        // var services = serviceCollection.BuildServiceProvider();
        // var mediator = services.GetRequiredService<IMediator>();
        // await mediator.Send(new AddSMBCredentialCommand("192.168.0.105", "Public", "backup", "aWYhl5CaV3xtr4Zvhr0u"));
        // await mediator.Send(new CreateBackupCommand(backupPlan, BackupSavePathHelper.GetBackupSavePath(savePath), "test"));

        IWizardStep? step = new MainMenuStep(new());

        do
        {
            step = await step.Execute();
        } while (step is not (null or ExitStep));
    }
}

internal static class Logging
{
    public static void AddLogging(this IServiceCollection services, string logDirectory)
    {
        services.AddLogging(
            builder =>
            {
                builder.ClearProviders();

                var logFilePath = Path.Combine(logDirectory, "backup-.log");
                var logger = new LoggerConfiguration()
                             .MinimumLevel.Is(LogEventLevel.Information)
                             .WriteTo.File(
                                 logFilePath,
                                 rollingInterval: RollingInterval.Month,
                                 fileSizeLimitBytes: 1_000_000,
                                 rollOnFileSizeLimit: true,
                                 retainedFileCountLimit: 12,
                                 shared: true)
                             .CreateLogger();
                builder.AddSerilog(logger, dispose: true);
            });
    }
}