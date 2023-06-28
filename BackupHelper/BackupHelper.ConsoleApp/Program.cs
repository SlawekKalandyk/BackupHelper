using BackupHelper.Core;
using BackupHelper.Core.Features;
using BackupHelper.Core.FileZipping;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            if (args.Length != 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
            {
                throw new ArgumentException();
            }

            var backupSavePath = GetBackupSavePath(args[0]);
            var backupConfigPath = args[1];
            var backupConfiguration = BackupConfiguration.FromJsonFile(backupConfigPath);

            var mediator = services.GetRequiredService<IMediator>();
            await mediator.Send(new CreateBackupCommand(backupConfiguration, backupSavePath));
        }

        private static string GetBackupSavePath(string argPath)
        {
            if (File.Exists(argPath))
            {
                return argPath;
            }

            if (Directory.Exists(argPath))
            {
                return Path.Combine(argPath, CreateDateTimeBasedZipFileName());
            }

            if (Path.HasExtension(argPath))
            {
                var directories = Path.GetDirectoryName(argPath);
                Directory.CreateDirectory(directories);
                return argPath;
            }

            Directory.CreateDirectory(argPath);
            return Path.Combine(argPath, CreateDateTimeBasedZipFileName());
        }

        private static string CreateDateTimeBasedZipFileName()
        {
            var time = DateTime.Now;
            var baseFileName = $"{time:yyyy-MM-dd_HH-mm-ss}_backup";
            var fileName = $"{baseFileName}.zip";
            var i = 1;
            while (File.Exists(fileName))
            {
                fileName = baseFileName + $".{i}.zip";
                ++i;
            }
            return fileName;
        }
    }
}