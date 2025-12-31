using BackupHelper.Abstractions;
using BackupHelper.ConsoleApp.Utilities;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Credentials;
using BackupHelper.Core.Features;
using BackupHelper.Sinks.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class PerformBackupStepParameters : IWizardParameters
{
    public PerformBackupStepParameters(string backupPlanLocation, string? workingDirectory)
    {
        BackupPlanLocation = backupPlanLocation;
        WorkingDirectory = workingDirectory;
    }

    public PerformBackupStepParameters(
        string backupPlanLocation,
        string? workingDirectory,
        string keePassDbLocation,
        string keePassDbPassword
    )
        : this(backupPlanLocation, workingDirectory)
    {
        KeePassDbLocation = keePassDbLocation;
        KeePassDbPassword = keePassDbPassword;
    }

    public string BackupPlanLocation { get; }
    public string? WorkingDirectory { get; }
    public string? KeePassDbLocation { get; }
    public string? KeePassDbPassword { get; }
}

public class PerformBackupStep : IWizardStep<PerformBackupStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;
    private readonly ILoggerFactory _loggerFactory;

    public PerformBackupStep(
        IMediator mediator,
        ICredentialsProviderFactory credentialsProviderFactory,
        ILoggerFactory loggerFactory
    )
    {
        _mediator = mediator;
        _credentialsProviderFactory = credentialsProviderFactory;
        _loggerFactory = loggerFactory;
    }

    public async Task<IWizardParameters?> Handle(
        PerformBackupStepParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var useEncryption = Prompt.Confirm("Do you want to encrypt the backup?");
        string? backupPassword = null;

        if (useEncryption)
        {
            backupPassword = GetBackupPassword();
        }

        var backupPlan = BackupPlan.FromJsonFile(parameters.BackupPlanLocation);
        using var credentialsProvider = GetCredentialsProvider(parameters);

        if (!string.IsNullOrEmpty(backupPlan.LogDirectory))
            AddBackupLogSink(backupPlan.LogDirectory);

        CreateBackupCommandResult? result = null;
        try
        {
            result = await _mediator.Send(
                new CreateBackupCommand(backupPlan, parameters.WorkingDirectory, backupPassword),
                cancellationToken
            );

            var backupSinks = GetBackupSinks(backupPlan);

            foreach (var sink in backupSinks)
            {
                await UploadToSink(sink, result.OutputFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Backup failed:");
            Console.WriteLine(ex.GetBaseException().Message);
        }
        finally
        {
            if (result != null && File.Exists(result.OutputFilePath))
            {
                File.Delete(result.OutputFilePath);
            }
        }

        return new MainMenuStepParameters();
    }

    private void AddBackupLogSink(string logDirectory)
    {
        if (!string.IsNullOrEmpty(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);

            var logFilePath = Path.Combine(logDirectory, "backup-.log");
            var logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
                .Enrich.WithThreadId()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Month,
                    fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 12,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [ThreadId:{ThreadId}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _loggerFactory.AddSerilog(logger, dispose: true);
        }
    }

    private ICredentialsProvider GetCredentialsProvider(PerformBackupStepParameters parameters)
    {
        if (parameters.KeePassDbLocation == null || parameters.KeePassDbPassword == null)
        {
            return _credentialsProviderFactory.Create(new NullCredentialsProviderConfiguration());
        }

        var keePassCredentialsConfiguration = new KeePassCredentialsProviderConfiguration(
            parameters.KeePassDbLocation,
            parameters.KeePassDbPassword
        );

        return _credentialsProviderFactory.Create(keePassCredentialsConfiguration);
    }

    private string GetBackupPassword()
    {
        string? backupPassword = null;

        while (backupPassword == null)
        {
            backupPassword = Prompt.Password("Enter backup password");
            var confirm = Prompt.Password("Confirm password");

            if (backupPassword != confirm)
            {
                Console.WriteLine("Passwords do not match. Please try again.");
                backupPassword = null;
            }
        }

        return backupPassword;
    }

    private IReadOnlyCollection<ISink> GetBackupSinks(BackupPlan backupPlan)
    {
        return backupPlan.Sinks.Select(sinkDestination => sinkDestination.CreateSink()).ToList();
    }

    private async Task UploadToSink(ISink sink, string outputFilePath)
    {
        if (await sink.IsAvailableAsync())
        {
            try
            {
                await sink.UploadAsync(outputFilePath);
                Console.WriteLine($"Uploaded backup to sink: {sink.Description}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to upload backup to sink '{sink.Description}': {ex.GetBaseException().Message}"
                );
            }
        }
        else
        {
            Console.WriteLine($"Sink '{sink.Description}' is not available. Skipping upload.");
        }
    }
}