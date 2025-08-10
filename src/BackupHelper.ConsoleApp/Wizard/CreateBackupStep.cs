using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public record CreateBackupStepParameters : IWizardParameters;

public class CreateBackupStep : IWizardStep<CreateBackupStepParameters>
{
    public Task<IWizardParameters?> Handle(CreateBackupStepParameters parameters, CancellationToken cancellationToken)
    {
        var backupPlanLocation = Prompt.Input<string>("Select backup plan location", validators: [Validators.Required()]);
        var outputZipPath = Prompt.Input<string>("Select output zip path", validators: [Validators.Required()]);

        return Task.FromResult<IWizardParameters?>(new SelectKeePassDatabaseStepParameters(backupPlanLocation, outputZipPath));
    }
}
