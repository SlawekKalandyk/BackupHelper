using MediatR;

namespace BackupHelper.ConsoleApp.Wizard;

public interface IWizardParameters : IRequest<IWizardParameters?>;

public interface IWizardStep<TParameters> : IRequestHandler<TParameters, IWizardParameters?>
    where TParameters : IWizardParameters
{
}