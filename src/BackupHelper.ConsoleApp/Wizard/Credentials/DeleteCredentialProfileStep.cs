using BackupHelper.Api.Features.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record DeleteCredentialProfileStepParameters : IWizardParameters;

public class DeleteCredentialProfileStep : IWizardStep<DeleteCredentialProfileStepParameters>
{
    private readonly IMediator _mediator;

    public DeleteCredentialProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(DeleteCredentialProfileStepParameters request, CancellationToken cancellationToken)
    {
        var credentialProfileNames = await _mediator.Send(new GetCredentialProfileNamesQuery(), cancellationToken);

        if (credentialProfileNames.Count == 0)
        {
            Console.WriteLine("No credential profiles available to delete.");
            return new ManageCredentialProfilesStepParameters();
        }

        var credentialProfileName = Prompt.Select("Select a credential profile to delete", credentialProfileNames, 5);
        var confirmation = Prompt.Confirm($"Are you sure you want to delete the credential profile '{credentialProfileName}'?");

        if (confirmation)
        {
            await _mediator.Send(new DeleteCredentialProfileCommand(credentialProfileName), cancellationToken);
            Console.WriteLine($"Credential profile '{credentialProfileName}' deleted successfully!");
        }

        return new ManageCredentialProfilesStepParameters();
    }
}