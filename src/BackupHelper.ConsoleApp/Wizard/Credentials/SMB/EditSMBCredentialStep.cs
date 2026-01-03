using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Connectors.SMB;
using BackupHelper.Core.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record EditSMBCredentialStepParameters(
    CredentialProfile CredentialProfile,
    CredentialEntry? CredentialEntryToEdit = null
) : IWizardParameters;

public class EditSMBCredentialStep : IWizardStep<EditSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public EditSMBCredentialStep(
        IMediator mediator,
        ICredentialsProviderFactory credentialsProviderFactory,
        IApplicationDataHandler applicationDataHandler
    )
    {
        _mediator = mediator;
        _credentialsProviderFactory = credentialsProviderFactory;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(
        EditSMBCredentialStepParameters request,
        CancellationToken cancellationToken
    )
    {
        if (request.CredentialProfile.Credentials.Count == 0)
        {
            Console.WriteLine("No credentials available to edit. Please add a credential first.");

            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var credentialEntryToEdit = request.CredentialEntryToEdit;

        if (credentialEntryToEdit == null)
        {
            var credentialDictionary = request.CredentialProfile.Credentials.ToDictionary(
                credential => credential.Title,
                credential => credential
            );
            var credentialTitle = Prompt.Select(
                "Select a credential to edit",
                credentialDictionary.Keys,
                5
            );
            credentialEntryToEdit = credentialDictionary[credentialTitle];
        }

        var choice = Prompt.Select(
            "Select an action",
            ["Change username", "Change password", "Cancel"]
        );

        if (choice == "Cancel")
        {
            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            Path.Combine(
                _applicationDataHandler.GetCredentialProfilesPath(),
                request.CredentialProfile.Name
            ),
            request.CredentialProfile.Password
        );
        using var credentialsProvider = _credentialsProviderFactory.Create(
            credentialsProviderConfiguration
        );
        var localTitle = credentialEntryToEdit.GetLocalTitle();
        var existingCredentials = credentialsProvider.GetCredential<SMBCredential>(localTitle);
        var (server, shareName) = SMBCredentialHelper.DeconstructSMBCredentialTitle(localTitle);

        if (existingCredentials == null)
        {
            Console.WriteLine(
                $"No existing SMB credentials found for {credentialEntryToEdit.Title}. Please create them first."
            );

            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var existingSMBCredentials = new SMBCredential(
            server,
            shareName,
            existingCredentials.Username,
            existingCredentials.Password
        );

        if (choice == "Change username")
        {
            Console.WriteLine($"Current username: {existingSMBCredentials.Username}");
            var newUsername = Prompt.Input<string>(
                "Enter new username",
                validators: [Validators.Required()]
            );
            var newSMBCredentials = existingSMBCredentials with { Username = newUsername };
            await _mediator.Send(
                new UpdateCredentialCommand(
                    credentialsProviderConfiguration,
                    existingSMBCredentials.ToCredentialEntry(),
                    newSMBCredentials.ToCredentialEntry()
                ),
                cancellationToken
            );
            Console.WriteLine("SMB credential username updated successfully!");
        }
        else if (choice == "Change password")
        {
            var newPassword = Prompt.Password(
                "Enter new password",
                validators: [Validators.Required()]
            );
            var confirmNewPassword = Prompt.Password(
                "Confirm new password",
                validators: [Validators.Required()]
            );

            if (newPassword != confirmNewPassword)
            {
                Console.WriteLine("Passwords do not match. Please try again.");

                return new EditSMBCredentialStepParameters(
                    request.CredentialProfile,
                    credentialEntryToEdit
                );
            }

            var newSMBCredentials = existingSMBCredentials with { Password = newPassword };
            await _mediator.Send(
                new UpdateCredentialCommand(
                    credentialsProviderConfiguration,
                    existingSMBCredentials.ToCredentialEntry(),
                    newSMBCredentials.ToCredentialEntry()
                ),
                cancellationToken
            );
            Console.WriteLine("SMB credential password updated successfully!");
        }

        return new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}