using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features;
using BackupHelper.Core.BackupZipping;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Spectre.Console;

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
        string keePassDbLocation
    )
        : this(backupPlanLocation, workingDirectory)
    {
        KeePassDbLocation = keePassDbLocation;
    }

    public string BackupPlanLocation { get; }
    public string? WorkingDirectory { get; }
    public string? KeePassDbLocation { get; }
}

public class PerformBackupStep : IWizardStep<PerformBackupStepParameters>
{
    private const string BackupLogDirectoryPropertyName = "BackupLogDirectory";
    private static readonly Lock LogSinkRegistrationLock = new();
    private static readonly HashSet<string> RegisteredLogDirectories = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly IMediator _mediator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public PerformBackupStep(
        IMediator mediator,
        ILoggerFactory loggerFactory,
        ICredentialsProviderFactory credentialsProviderFactory
    )
    {
        _mediator = mediator;
        _loggerFactory = loggerFactory;
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public async Task<IWizardParameters?> Handle(
        PerformBackupStepParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var useEncryption = AnsiConsole.Confirm("Do you want to encrypt the backup?");

        SensitiveString? backupPassword = null;
        CreateBackupFileCommandResult? result = null;
        try
        {
            if (useEncryption)
            {
                backupPassword = GetBackupPassword();
            }

            var backupPlan = BackupPlan.FromJsonFile(parameters.BackupPlanLocation);
            var logDirectory = string.IsNullOrWhiteSpace(backupPlan.LogDirectory)
                ? null
                : AddBackupLogSink(backupPlan.LogDirectory);
            using var logDirectoryScope = logDirectory == null
                ? null
                : LogContext.PushProperty(BackupLogDirectoryPropertyName, logDirectory);

            result = await _mediator.Send(
                new CreateBackupFileCommand(
                    backupPlan,
                    parameters.WorkingDirectory,
                    backupPassword
                ),
                cancellationToken
            );

            foreach (var sinkDestination in backupPlan.Sinks)
            {
                var sinkUploadResult = await _mediator.Send(
                    new UploadBackupToSinkCommand(sinkDestination, result.OutputFilePath),
                    cancellationToken
                );

                switch (sinkUploadResult.Status)
                {
                    case BackupSinkUploadStatus.Uploaded:
                        Console.WriteLine(
                            $"Uploaded backup to sink: {sinkUploadResult.SinkDescription}"
                        );
                        break;
                    case BackupSinkUploadStatus.Failed:
                        Console.WriteLine(
                            $"Failed to upload backup to sink '{sinkUploadResult.SinkDescription}': {sinkUploadResult.FailureMessage}"
                        );
                        break;
                    case BackupSinkUploadStatus.SkippedUnavailable:
                        Console.WriteLine(
                            $"Sink '{sinkUploadResult.SinkDescription}' is not available. Skipping upload."
                        );
                        break;
                }
            }

            Console.WriteLine("Backup completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Backup failed:");
            Console.WriteLine(ex.GetBaseException().Message);
        }
        finally
        {
            _credentialsProviderFactory.ClearDefaultCredentialsProviderConfiguration();

            backupPassword?.Dispose();

            if (result != null && File.Exists(result.OutputFilePath))
            {
                File.Delete(result.OutputFilePath);
            }
        }

        return new MainMenuStepParameters();
    }

    private string AddBackupLogSink(string logDirectory)
    {
        var normalizedLogDirectory = Path.GetFullPath(logDirectory);

        lock (LogSinkRegistrationLock)
        {
            if (RegisteredLogDirectories.Contains(normalizedLogDirectory))
                return normalizedLogDirectory;

            Directory.CreateDirectory(normalizedLogDirectory);

            var logFilePath = Path.Combine(normalizedLogDirectory, "backup-.log");
            var logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Filter.ByIncludingOnly(logEvent =>
                    logEvent.Properties.TryGetValue(
                        BackupLogDirectoryPropertyName,
                        out var logDirectoryProperty
                    )
                    && logDirectoryProperty is ScalarValue { Value: string propertyValue }
                    && string.Equals(
                        propertyValue,
                        normalizedLogDirectory,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
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
            RegisteredLogDirectories.Add(normalizedLogDirectory);

            return normalizedLogDirectory;
        }
    }

    private SensitiveString GetBackupPassword()
    {
        SensitiveString? backupPassword = null;

        while (backupPassword == null)
        {
            backupPassword = SecureConsole.PromptPassword("Enter backup password");
            using var confirm = SecureConsole.PromptPassword("Confirm password");

            if (!backupPassword.Equals(confirm))
            {
                Console.WriteLine("Passwords do not match. Please try again.");
                backupPassword.Dispose();
                backupPassword = null;
            }
        }

        return backupPassword;
    }

}