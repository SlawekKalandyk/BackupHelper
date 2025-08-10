namespace BackupHelper.ConsoleApp.Wizard;

public class ExitStep : WizardStepBase<ExitStepParameters>
{
    public ExitStep(ExitStepParameters parameters) : base(parameters) { }

    public override Task<IWizardStep?> Execute()
    {
        Console.WriteLine("Exiting the application. Goodbye!");
        return null;
    }
}

public class ExitStepParameters { }