using BackupHelper.Abstractions;
using BackupHelper.Api;
using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Credentials;
using BackupHelper.Core.Features;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class PerformBackupStep : WizardStepBase<PerformBackupStepParameters>
{
    public PerformBackupStep(PerformBackupStepParameters parameters) : base(parameters) { }

    public override async Task<IWizardStep?> Execute()
    {
        var useEncryption = Prompt.Confirm("Do you want to encrypt the backup?");
        string? backupPassword = null;

        if (useEncryption)
        {
            backupPassword = GetBackupPassword();
        }

        var backupPlan = BackupPlan.FromJsonFile(Parameters.BackupPlanLocation);
        using var credentialsProvider = GetCredentialsProvider();
        var configuration = new ConfigurationBuilder()
            .Build();
        var serviceCollection = new ServiceCollection()
            .AddApiServices(configuration, credentialsProvider);

        if (!string.IsNullOrEmpty(backupPlan.LogDirectory))
        {
            Directory.CreateDirectory(backupPlan.LogDirectory);
            serviceCollection.AddLogging(backupPlan.LogDirectory);
        }

        await using var services = serviceCollection.BuildServiceProvider();
        var mediator = services.GetRequiredService<IMediator>();

        await mediator.Send(new CreateBackupCommand(backupPlan, BackupSavePathHelper.GetBackupSavePath(Parameters.OutputZipPath), backupPassword))
                      .ContinueWith(
                          _ =>
                          {
                              Console.WriteLine($"Backup completed successfully. Output file: {Parameters.OutputZipPath}");
                          });

        return new MainMenuStep(new());
    }

    private ICredentialsProvider GetCredentialsProvider()
    {
        return Parameters.KeePassDbLocation != null && Parameters.KeePassDbPassword != null
                   ? new KeePassCredentialsProvider(Parameters.KeePassDbLocation, Parameters.KeePassDbPassword)
                   : new NullCredentialsProvider();
    }

    private string GetBackupPassword()
    {
        string? backupPassword = null;

        while (backupPassword == null)
        {
            backupPassword = Prompt.Password("Enter backup password: ");
            var confirm = Prompt.Password("Confirm password: ");

            if (backupPassword != confirm)
            {
                Console.WriteLine("Passwords do not match. Please try again.");
                backupPassword = null;
            }
        }

        return backupPassword;
    }
}

public class PerformBackupStepParameters
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