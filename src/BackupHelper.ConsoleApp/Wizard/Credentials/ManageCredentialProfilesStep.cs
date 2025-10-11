using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record ManageCredentialProfilesStepParameters : IWizardParameters;

public class ManageCredentialProfilesStep : IWizardStep<ManageCredentialProfilesStepParameters>
{
    public Task<IWizardParameters?> Handle(
        ManageCredentialProfilesStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var choice = Prompt.Select(
            "Manage Credential Profiles",
            [
                "Create new credential profile",
                "Show credential profile info",
                "Edit existing credential profile",
                "Delete credential profile",
                "Back to main menu",
            ]
        );

        return Task.FromResult<IWizardParameters?>(
            choice switch
            {
                "Create new credential profile" => new CreateCredentialProfileStepParameters(),
                "Show credential profile info" => new ShowCredentialProfileInfoStepParameters(),
                "Edit existing credential profile" => new EditCredentialProfileStepParameters(),
                "Delete credential profile" => new DeleteCredentialProfileStepParameters(),
                "Back to main menu" => new MainMenuStepParameters(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(choice),
                    $"Invalid choice: {choice}"
                ),
            }
        );
    }
}
