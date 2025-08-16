using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.SMB;
using BackupHelper.Core.Credentials;
using BackupHelper.Sources.SMB;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record AddSmbCredentialStepParameters(CredentialProfile CredentialProfile) : IWizardParameters;

public class AddSmbCredentialStep : IWizardStep<AddSmbCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public AddSmbCredentialStep(IMediator mediator, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(AddSmbCredentialStepParameters request, CancellationToken cancellationToken)
    {
        var keePassDbLocation = Path.Combine(_applicationDataHandler.GetCredentialProfilesPath(), request.CredentialProfile.Name);
        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(keePassDbLocation, request.CredentialProfile.Password);

        var server = Prompt.Input<string>("Enter SMB server address", validators: [Validators.Required()]);
        var share = Prompt.Input<string>("Enter SMB share name", validators: [Validators.Required()]);
        var credentialAlreadyExists = await _mediator.Send(new CheckSMBCredentialExistsQuery(credentialsProviderConfiguration, server, share), cancellationToken);

        if (credentialAlreadyExists)
        {
            var title = SMBCredentialHelper.GetSMBCredentialTitle(server, share);
            var editCredential = Prompt.Confirm($"SMB credential for {title} exists. Do you want to edit it?");

            if (editCredential)
            {
                return new EditSMBCredentialStepParameters(credentialsProviderConfiguration, server, share);
            }
            else
            {
                return new AddSmbCredentialStepParameters(request.CredentialProfile);
            }
        }

        var username = Prompt.Input<string>("Enter SMB username", validators: [Validators.Required()]);
        var password = Prompt.Password("Enter SMB password", validators: [Validators.Required()]);

        var smbCredential = new SMBCredential(server, share, username, password);
        await _mediator.Send(new AddSMBCredentialCommand(credentialsProviderConfiguration, smbCredential), cancellationToken);

        Console.WriteLine("SMB credential added successfully.");
        var addAnother = Prompt.Confirm("Do you want to add another SMB credential?");

        return addAnother
                   ? new AddSmbCredentialStepParameters(request.CredentialProfile)
                   : new ManageCredentialProfilesStepParameters();
    }
}