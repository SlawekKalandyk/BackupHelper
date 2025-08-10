using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class CreateBackupStep : WizardStepBase<CreateBackupStepParameters>
{
    public CreateBackupStep(CreateBackupStepParameters parameters) : base(parameters) { }

    public override Task<IWizardStep?> Execute()
    {
        var backupPlanLocation = Prompt.Input<string>("Select backup plan location: ", validators: [Validators.Required()]);
        var outputZipPath = Prompt.Input<string>("Select output zip path: ", validators: [Validators.Required()]);

        return Task.FromResult<IWizardStep?>(new SelectKeePassDatabaseStep(new(backupPlanLocation, outputZipPath)));
    }
}

public record CreateBackupStepParameters;