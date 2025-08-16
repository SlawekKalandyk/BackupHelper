using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials.SMB;
using BackupHelper.Sources.SMB;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record EditSMBCredentialStepParameters(ICredentialsProviderConfiguration CredentialsProviderConfiguration, string Server, string ShareName)
    : IWizardParameters;

public class EditSMBCredentialStep : IWizardStep<EditSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public EditSMBCredentialStep(IMediator mediator, ICredentialsProviderFactory credentialsProviderFactory)
    {
        _mediator = mediator;
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public async Task<IWizardParameters?> Handle(EditSMBCredentialStepParameters request, CancellationToken cancellationToken)
    {
        var choice = Prompt.Select(
            "Select an action",
            [
                "Change username",
                "Change password",
                "Cancel"
            ]);

        if (choice == "Cancel")
        {
            return new ManageCredentialProfilesStepParameters();
        }

        using var credentialsProvider = _credentialsProviderFactory.Create(request.CredentialsProviderConfiguration);
        var credentialName = SMBCredentialHelper.GetSMBCredentialTitle(request.Server, request.ShareName);
        var existingCredentials = credentialsProvider.GetCredential(credentialName);

        if (existingCredentials == null)
        {
            Console.WriteLine($"No existing SMB credentials found for {credentialName}. Please create them first.");

            return new ManageCredentialProfilesStepParameters();
        }

        var existingSMBCredentials = new SMBCredential(
            request.Server,
            request.ShareName,
            existingCredentials.Username,
            existingCredentials.Password);

        if (choice == "Change username")
        {
            Console.WriteLine($"Current username: {existingSMBCredentials.Username}");
            var newUsername = Prompt.Input<string>("Enter new username", validators: [Validators.Required()]);
            var newSMBCredentials = existingSMBCredentials with { Username = newUsername };
            await _mediator.Send(
                new UpdateSMBCredentialCommand(
                    request.CredentialsProviderConfiguration,
                    existingSMBCredentials,
                    newSMBCredentials),
                cancellationToken);
            Console.WriteLine("SMB credential username updated successfully!");
        }
        else if (choice == "Change password")
        {
            var newPassword = Prompt.Password("Enter new password", validators: [Validators.Required()]);
            var newSMBCredentials = existingSMBCredentials with { Password = newPassword };
            await _mediator.Send(
                new UpdateSMBCredentialCommand(
                    request.CredentialsProviderConfiguration,
                    existingSMBCredentials,
                    newSMBCredentials),
                cancellationToken);
            Console.WriteLine("SMB credential password updated successfully!");
        }

        return new ManageCredentialProfilesStepParameters();
    }
}