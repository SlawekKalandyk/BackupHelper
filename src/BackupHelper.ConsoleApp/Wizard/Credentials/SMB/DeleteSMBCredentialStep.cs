using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.SMB;
using BackupHelper.Core.Credentials;
using BackupHelper.Sources.SMB;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record DeleteSMBCredentialStepParameters(CredentialProfile CredentialProfile, CredentialEntry? CredentialToDelete = null) : IWizardParameters;

public class DeleteSMBCredentialStep : IWizardStep<DeleteSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public DeleteSMBCredentialStep(IMediator mediator, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(DeleteSMBCredentialStepParameters request, CancellationToken cancellationToken)
    {
        if (request.CredentialProfile.Credentials.Count == 0)
        {
            Console.WriteLine("No credentials available to delete. Please add a credential first.");

            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var credentialToDelete = request.CredentialToDelete;

        if (credentialToDelete == null)
        {
            var credentialDictionary = request.CredentialProfile.Credentials.ToDictionary(credential => credential.Title, credential => credential);
            var credentialTitle = Prompt.Select("Select a credential to delete", credentialDictionary.Keys, 5);
            var confirmation = Prompt.Confirm($"Are you sure you want to delete the credential '{credentialTitle}'?");

            if (!confirmation)
            {
                Console.WriteLine("Credential deletion cancelled.");

                return new EditCredentialProfileStepParameters(request.CredentialProfile);
            }

            credentialToDelete = credentialDictionary[credentialTitle];
        }

        var (server, shareName) = SMBCredentialHelper.DeconstructSMBCredentialTitle(credentialToDelete.Title);
        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            Path.Combine(_applicationDataHandler.GetCredentialProfilesPath(), request.CredentialProfile.Name),
            request.CredentialProfile.Password);

        await _mediator.Send(new DeleteSMBCredentialCommand(credentialsProviderConfiguration, server, shareName), cancellationToken);
        Console.WriteLine("SMB credential deleted successfully!");

        return new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}