using BackupHelper.ConsoleApp.Wizard.BackupProfiles;
using BackupHelper.ConsoleApp.Wizard.Credentials;
using BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public record MainMenuStepParameters : IWizardParameters;

public class MainMenuStep : IWizardStep<MainMenuStepParameters>
{
    public Task<IWizardParameters?> Handle(
        MainMenuStepParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var choice = Prompt.Select(
            "Main Menu",
            ["Create backup", "Manage backup profiles", "Manage credential profiles", "Exit"]
        );

        return Task.FromResult<IWizardParameters?>(
            choice switch
            {
                "Create backup" => new CreateBackupStepParameters(),
                "Manage backup profiles" => new ManageBackupProfilesStepParameters(),
                "Manage credential profiles" => new ManageCredentialProfilesStepParameters(),
                "Exit" => null,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(choice),
                    $"Invalid choice: {choice}"
                ),
            }
        );
    }
}
