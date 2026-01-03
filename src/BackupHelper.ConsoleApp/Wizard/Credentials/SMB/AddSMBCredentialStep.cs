using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Api.Features.Credentials.SMB;
using BackupHelper.Connectors.SMB;
using BackupHelper.ConsoleApp.Utilities;
using BackupHelper.Core.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record AddSMBCredentialStepParameters(CredentialProfile CredentialProfile)
    : IWizardParameters;

public class AddSMBCredentialStep : IWizardStep<AddSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public AddSMBCredentialStep(IMediator mediator, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(
        AddSMBCredentialStepParameters request,
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

        var server = Prompt.Input<string>(
            "Enter SMB server address",
            validators: [Validators.Required(), ValidatorsHelper.IPAddressOrHostname]
        );
        var share = Prompt.Input<string>(
            "Enter SMB share name",
            validators: [Validators.Required(), ValidatorsHelper.HasNoInvalidChars]
        );
        var credential = await _mediator.Send(
            new GetSMBCredentialQuery(credentialsProviderConfiguration, server, share),
            cancellationToken
        );

        if (credential != null)
        {
            var title = SMBCredentialHelper.GetSMBCredentialTitle(server, share);
            var editCredential = Prompt.Confirm(
                $"SMB credential for {title} exists. Do you want to edit it?"
            );

            if (editCredential)
            {
                return new EditSMBCredentialStepParameters(request.CredentialProfile, credential);
            }
            else
            {
                return new EditCredentialProfileStepParameters(request.CredentialProfile);
            }
        }

        var username = Prompt.Input<string>(
            "Enter SMB username",
            validators: [Validators.Required()]
        );
        var password = Prompt.Password("Enter SMB password", validators: [Validators.Required()]);

        var smbCredential = new SMBCredential(server, share, username, password);
        await _mediator.Send(
            new AddCredentialCommand(
                credentialsProviderConfiguration,
                smbCredential.ToCredentialEntry()
            ),
            cancellationToken
        );

        Console.WriteLine("SMB credential added successfully.");
        var addAnother = Prompt.Confirm("Do you want to add another SMB credential?");

        return addAnother
            ? new AddSMBCredentialStepParameters(request.CredentialProfile)
            : new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}