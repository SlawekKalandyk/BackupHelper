using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.Azure;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Connectors.Azure;
using BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.Azure;

public record AddAzureBlobCredentialStepParameters(CredentialProfile CredentialProfile)
    : IWizardParameters;

public class AddAzureBlobCredentialStep : IWizardStep<AddAzureBlobCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public AddAzureBlobCredentialStep(
        IMediator mediator,
        IApplicationDataHandler applicationDataHandler
    )
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(
        AddAzureBlobCredentialStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var keePassDbLocation = Path.Combine(
            _applicationDataHandler.GetCredentialProfilesPath(),
            request.CredentialProfile.Name
        );

        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            keePassDbLocation,
            request.CredentialProfile.Password
        );

        var accountName = Prompt.Input<string>(
            "Enter Azure Storage Account Name",
            validators: [Validators.Required()]
        );

        var credentialEntry = await _mediator.Send(
            new GetAzureBlobCredentialQuery(credentialsProviderConfiguration, accountName),
            cancellationToken
        );

        if (credentialEntry != null)
        {
            var editCredential = Prompt.Confirm(
                $"Azure Blob credential for account '{accountName}' exists. Do you want to edit it?"
            );

            if (editCredential)
            {
                return new EditAzureBlobCredentialStepParameters(
                    request.CredentialProfile,
                    credentialEntry
                );
            }
            else
            {
                return new EditCredentialProfileStepParameters(request.CredentialProfile);
            }
        }

        var sharedAccessSignature = Prompt.Password(
            "Enter Azure Storage Account Shared Access Signature (SAS)",
            validators: [Validators.Required()]
        );

        var credential = new AzureBlobCredential(accountName, sharedAccessSignature);

        await _mediator.Send(
            new AddCredentialCommand(
                credentialsProviderConfiguration,
                credential.ToCredentialEntry()
            ),
            cancellationToken
        );

        Console.WriteLine("Azure Blob credential added successfully!");
        var addAnother = Prompt.Confirm("Do you want to add another Azure Blob credential?");

        return addAnother
            ? new AddAzureBlobCredentialStepParameters(request.CredentialProfile)
            : new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}