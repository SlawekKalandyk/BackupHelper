using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class MainMenuStep : WizardStepBase<MainMenuStepParameters>
{
    public MainMenuStep(MainMenuStepParameters parameters) : base(parameters) { }

    public override Task<IWizardStep?> Execute()
    {
        var choice = Prompt.Select("Main Menu", new[]
        {
            "Create backup",
            "Add SMB credential",
            "Exit"
        });

        return choice switch
        {
            "Create backup" => Task.FromResult<IWizardStep?>(new CreateBackupStep(new())),
            "Add SMB credential" => Task.FromResult<IWizardStep?>(new AddSmbCredentialStep(new())),
            "Exit" => Task.FromResult<IWizardStep?>(new ExitStep(new())),
            _ => throw new ArgumentOutOfRangeException(nameof(choice), $"Invalid choice: {choice}")
        };
    }
}

public record MainMenuStepParameters;