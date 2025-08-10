using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public record MainMenuStepParameters : IWizardParameters;

public class MainMenuStep : IWizardStep<MainMenuStepParameters>
{
    public Task<IWizardParameters?> Handle(MainMenuStepParameters parameters, CancellationToken cancellationToken)
    {
        var choice = Prompt.Select(
            "Main Menu",
            [
                "Create backup",
                "Add SMB credential",
                "Exit"
            ]);

        return Task.FromResult<IWizardParameters?>(
            choice switch
            {
                "Create backup" => new CreateBackupStepParameters(),
                "Add SMB credential" => new AddSmbCredentialStepParameters(),
                "Exit" => null,
                _ => throw new ArgumentOutOfRangeException(nameof(choice), $"Invalid choice: {choice}")
            });
    }
}