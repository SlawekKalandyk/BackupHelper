using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.SMB;
using BackupHelper.Core.Credentials;
using BackupHelper.Sources.SMB;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record EditSMBCredentialStepParameters(CredentialProfile CredentialProfile, string Server, string ShareName)
    : IWizardParameters;

public class EditSMBCredentialStep : IWizardStep<EditSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public EditSMBCredentialStep(IMediator mediator, ICredentialsProviderFactory credentialsProviderFactory, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _credentialsProviderFactory = credentialsProviderFactory;
        _applicationDataHandler = applicationDataHandler;
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
            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            Path.Combine(_applicationDataHandler.GetCredentialProfilesPath(), request.CredentialProfile.Name),
            request.CredentialProfile.Password);
        using var credentialsProvider = _credentialsProviderFactory.Create(credentialsProviderConfiguration);
        var credentialName = SMBCredentialHelper.GetSMBCredentialTitle(request.Server, request.ShareName);
        var existingCredentials = credentialsProvider.GetCredential(credentialName);

        if (existingCredentials == null)
        {
            Console.WriteLine($"No existing SMB credentials found for {credentialName}. Please create them first.");

            return new EditCredentialProfileStepParameters(request.CredentialProfile);
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
                    credentialsProviderConfiguration,
                    existingSMBCredentials,
                    newSMBCredentials),
                cancellationToken);
            Console.WriteLine("SMB credential username updated successfully!");
        }
        else if (choice == "Change password")
        {
            var newPassword = Prompt.Password("Enter new password", validators: [Validators.Required()]);
            var confirmNewPassword = Prompt.Password("Confirm new password", validators: [Validators.Required()]);

            if (newPassword != confirmNewPassword)
            {
                Console.WriteLine("Passwords do not match. Please try again.");

                return new EditSMBCredentialStepParameters(request.CredentialProfile, request.Server, request.ShareName);
            }

            var newSMBCredentials = existingSMBCredentials with { Password = newPassword };
            await _mediator.Send(
                new UpdateSMBCredentialCommand(
                    credentialsProviderConfiguration,
                    existingSMBCredentials,
                    newSMBCredentials),
                cancellationToken);
            Console.WriteLine("SMB credential password updated successfully!");
        }

        return new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}