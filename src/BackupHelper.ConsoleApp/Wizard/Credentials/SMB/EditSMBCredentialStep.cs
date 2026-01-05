using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Connectors.SMB;
using BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.SMB;

public record EditSMBCredentialStepParameters(
    CredentialProfile CredentialProfile,
    CredentialEntry? CredentialEntryToEdit = null
) : IWizardParameters;

public class EditSMBCredentialStep : IWizardStep<EditSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;
    private readonly IApplicationDataHandler _applicationDataHandler;
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;

    public EditSMBCredentialStep(
        IMediator mediator,
        ICredentialsProviderFactory credentialsProviderFactory,
        IApplicationDataHandler applicationDataHandler,
        CredentialHandlerRegistry credentialHandlerRegistry
    )
    {
        _mediator = mediator;
        _credentialsProviderFactory = credentialsProviderFactory;
        _applicationDataHandler = applicationDataHandler;
        _credentialHandlerRegistry = credentialHandlerRegistry;
    }

    public async Task<IWizardParameters?> Handle(
        EditSMBCredentialStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var credentialEntries =
            _credentialHandlerRegistry.GetAllCredentialEntriesOfType<SMBCredential>(
                request.CredentialProfile.Credentials
            );

        if (credentialEntries.Count == 0)
        {
            Console.WriteLine(
                "No SMB credentials available to edit. Please add a credential first."
            );
            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        var credentialEntryToEdit = request.CredentialEntryToEdit;

        if (credentialEntryToEdit == null)
        {
            var credentialDictionary = credentialEntries.ToDictionary(
                entry => entry.EntryTitle,
                entry => entry
            );
            var credentialTitle = Prompt.Select(
                "Select a credential to edit",
                credentialDictionary.Keys,
                5
            );
            credentialEntryToEdit = credentialDictionary[credentialTitle];
        }

        var choice = Prompt.Select(
            "Select an option to edit",
            ["Change username", "Change password", "Back to Credential Profile Menu"]
        );

        if (choice == "Back to Credential Profile Menu")
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

        var existingCredential = credentialsProvider.GetCredential<SMBCredential>(
            credentialEntryToEdit
        );

        if (existingCredential == null)
        {
            Console.WriteLine(
                $"No existing SMB credentials found for {credentialEntryToEdit.EntryTitle}. Please create them first."
            );
            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        if (choice == "Change username")
        {
            Console.WriteLine($"Current username: {existingCredential.Username}");
            var newUsername = Prompt.Input<string>(
                "Enter new username",
                validators: [Validators.Required()]
            );
            var newCredential = existingCredential with { Username = newUsername };
            await _mediator.Send(
                new UpdateCredentialCommand(
                    credentialsProviderConfiguration,
                    existingCredential.ToCredentialEntry(),
                    newCredential.ToCredentialEntry()
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

            var newSMBCredentials = existingCredential with { Password = newPassword };
            await _mediator.Send(
                new UpdateCredentialCommand(
                    credentialsProviderConfiguration,
                    existingCredential.ToCredentialEntry(),
                    newSMBCredentials.ToCredentialEntry()
                ),
                cancellationToken
            );
            Console.WriteLine("SMB credential password updated successfully!");
        }

        return new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}