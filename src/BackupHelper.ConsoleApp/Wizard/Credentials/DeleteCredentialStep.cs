using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record DeleteCredentialStepParameters(
    CredentialProfile CredentialProfile,
    CredentialEntry? CredentialToDelete = null
) : IWizardParameters;

public class DeleteCredentialStep : IWizardStep<DeleteCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public DeleteCredentialStep(IMediator mediator, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(
        DeleteCredentialStepParameters request,
        CancellationToken cancellationToken
    )
    {
        if (request.CredentialProfile.Credentials.Count == 0)
        {
            Console.WriteLine("No credentials available to delete. Please add a credential first.");

            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var credentialToDelete = request.CredentialToDelete;

        if (credentialToDelete == null)
        {
            var credentialDictionary = request.CredentialProfile.Credentials.ToDictionary(
                credential => credential.EntryTitle,
                credential => credential
            );
            var credentialTitle = Prompt.Select(
                "Select a credential to delete",
                credentialDictionary.Keys,
                5
            );
            var confirmation = Prompt.Confirm(
                $"Are you sure you want to delete the credential '{credentialTitle}'?"
            );

            if (!confirmation)
            {
                Console.WriteLine("Credential deletion cancelled.");

                return new EditCredentialProfileStepParameters(request.CredentialProfile);
            }

            credentialToDelete = credentialDictionary[credentialTitle];
        }

        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            Path.Combine(
                _applicationDataHandler.GetCredentialProfilesPath(),
                request.CredentialProfile.Name
            ),
            request.CredentialProfile.Password
        );

        await _mediator.Send(
            new DeleteCredentialCommand(credentialsProviderConfiguration, credentialToDelete),
            cancellationToken
        );
        Console.WriteLine("Credential deleted successfully!");

        var credentialProfile = await _mediator.Send(
            new GetCredentialProfileQuery(
                request.CredentialProfile.Name,
                request.CredentialProfile.Password
            ),
            cancellationToken
        );

        return new EditCredentialProfileStepParameters(credentialProfile);
    }
}
