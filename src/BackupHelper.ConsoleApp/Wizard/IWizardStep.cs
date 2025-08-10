namespace BackupHelper.ConsoleApp.Wizard;

public interface IWizardStep
{
    Task<IWizardStep?> Execute();
}

public abstract class WizardStepBase<T> : IWizardStep
    where T : class
{
    protected WizardStepBase(T parameters)
    {
        Parameters = parameters;
    }

    protected T Parameters { get; }

    public abstract Task<IWizardStep?> Execute();
}