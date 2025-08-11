using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.BackupProfiles;

public record ManageBackupProfilesStepParameters : IWizardParameters;

public class ManageBackupProfilesStep : IWizardStep<ManageBackupProfilesStepParameters>
{
    public Task<IWizardParameters?> Handle(ManageBackupProfilesStepParameters request, CancellationToken cancellationToken)
    {
        var choice = Prompt.Select(
            "Manage Backup Profiles",
            [
                "Create new backup profile",
                "Show backup profile info",
                "Edit existing backup profile",
                "Delete backup profile",
                "Back to main menu"
            ]);

        return Task.FromResult<IWizardParameters?>(
            choice switch
            {
                "Create new backup profile" => new CreateBackupProfileStepParameters(),
                "Show backup profile info" => new GetBackupProfileInfoStepParameters(),
                "Edit existing backup profile" => new EditBackupProfileStepParameters(),
                "Delete backup profile" => new DeleteBackupProfileStepParameters(),
                "Back to main menu" => new MainMenuStepParameters(),
                _ => throw new ArgumentOutOfRangeException(nameof(choice), $"Invalid choice: {choice}")
            });
    }
}