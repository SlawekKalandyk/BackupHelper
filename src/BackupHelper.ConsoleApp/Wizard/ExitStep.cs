namespace BackupHelper.ConsoleApp.Wizard;

public record ExitStepParameters : IWizardParameters;

public class ExitStep : IWizardStep<ExitStepParameters>
{
    public Task<IWizardParameters?> Handle(ExitStepParameters parameters, CancellationToken cancellationToken)
    {
        Console.WriteLine("Exiting the application. Goodbye!");

        return Task.FromResult<IWizardParameters?>(null);
    }
}