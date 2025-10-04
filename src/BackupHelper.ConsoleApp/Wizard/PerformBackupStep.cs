using BackupHelper.Abstractions;
using BackupHelper.ConsoleApp.Utilities;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Credentials;
using BackupHelper.Core.Features;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class PerformBackupStepParameters : IWizardParameters
{
    public PerformBackupStepParameters(string backupPlanLocation, string outputZipPath)
    {
        BackupPlanLocation = backupPlanLocation;
        OutputZipPath = outputZipPath;
    }

    public PerformBackupStepParameters(string backupPlanLocation, string outputZipPath, string keePassDbLocation, string keePassDbPassword)
        : this(backupPlanLocation, outputZipPath)
    {
        KeePassDbLocation = keePassDbLocation;
        KeePassDbPassword = keePassDbPassword;
    }

    public string OutputZipPath { get; }
    public string BackupPlanLocation { get; }
    public string? KeePassDbLocation { get; }
    public string? KeePassDbPassword { get; }
}

public class PerformBackupStep : IWizardStep<PerformBackupStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;
    private readonly ILoggerFactory _loggerFactory;

    public PerformBackupStep(IMediator mediator, ICredentialsProviderFactory credentialsProviderFactory, ILoggerFactory loggerFactory)
    {
        _mediator = mediator;
        _credentialsProviderFactory = credentialsProviderFactory;
        _loggerFactory = loggerFactory;
    }

    public async Task<IWizardParameters?> Handle(PerformBackupStepParameters parameters, CancellationToken cancellationToken)
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

        var backupSavePath = BackupSavePathHelper.GetBackupSavePath(parameters.OutputZipPath);
        await _mediator.Send(
                           new CreateBackupCommand(backupPlan, backupSavePath, backupPassword),
                           cancellationToken)
                       .ContinueWith(
                           _ =>
                           {
                               Console.WriteLine($"Backup completed successfully. Output file: {backupSavePath}");
                           },
                           cancellationToken);

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
                             outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [ThreadId:{ThreadId}] {Message:lj}{NewLine}{Exception}")
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
            parameters.KeePassDbPassword);

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
}