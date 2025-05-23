using BackupHelper.Core;
using BackupHelper.Core.Features;
using BackupHelper.Core.FileZipping;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace BackupHelper.ConsoleApp
{
    internal class Program
    {
        internal static async Task Main(string[] args)
        {
            //first arg - save paths separated by semicolon, e.g. "C:\Users\user\Documents;C:\Users\user\Pictures"
            //second arg - path to backup configuration file, e.g. "C:\Users\user\Documents\backup.json"

            if (args.Length != 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
            {
                throw new ArgumentException();
            }

            var splitSavePaths = args[0].Split(';');
            var backupPlanFilePath = args[1];
            var backupPlan = BackupPlan.FromJsonFile(backupPlanFilePath);

            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            var serviceCollection = new ServiceCollection()
                .AddCoreServices(configuration);

            if (!string.IsNullOrEmpty(backupPlan.LogDirectory))
            {
                serviceCollection.AddLogging(LogLevel.Debug, backupPlan.LogDirectory);
            }
            
            var services = serviceCollection.BuildServiceProvider();
            var mediator = services.GetRequiredService<IMediator>();
            var savePaths = splitSavePaths.Select(GetBackupSavePath).ToArray();
            await mediator.Send(new CreateBackupCommand(backupPlan, savePaths));
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
                if (string.IsNullOrEmpty(directories))
                {
                    return argPath;
                }

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

    internal static class Logging
    {
        public static void AddLogging(this IServiceCollection services, LogLevel logLevel, string logDirectory)
        {
            services.AddScoped(typeof(ILogger<>), typeof(NullLogger<>));
        }
    }

    // Temporarily using a null logger to avoid logging issues
    public class NullLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => false;

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception, string> formatter) { }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}