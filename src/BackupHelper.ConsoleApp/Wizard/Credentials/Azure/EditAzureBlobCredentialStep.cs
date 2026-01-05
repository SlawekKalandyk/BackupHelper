using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Connectors.Azure;
using BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.Azure;

public record EditAzureBlobCredentialStepParameters(
    CredentialProfile CredentialProfile,
    CredentialEntry? CredentialEntryToEdit = null
) : IWizardParameters;

public class EditAzureBlobCredentialStep : IWizardStep<EditAzureBlobCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;
    private readonly IApplicationDataHandler _applicationDataHandler;
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;

    public EditAzureBlobCredentialStep(
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
        EditAzureBlobCredentialStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var credentialEntries =
            _credentialHandlerRegistry.GetAllCredentialEntriesOfType<AzureBlobCredential>(
                request.CredentialProfile.Credentials
            );

        if (credentialEntries.Count == 0)
        {
            Console.WriteLine(
                "No Azure Blob Credentials available to edit. Please add a credential first."
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

            var credentialEntryName = Prompt.Select(
                "Select an Azure Blob Credential to edit",
                credentialDictionary.Keys.ToList(),
                5
            );

            credentialEntryToEdit = credentialDictionary[credentialEntryName];
        }

        var choice = Prompt.Select(
            "Select an option to edit",
            ["Change Shared Access Storage token", "Back to Credential Profile Menu"]
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

        var existingCredential = credentialsProvider.GetCredential<AzureBlobCredential>(
            credentialEntryToEdit
        );

        if (existingCredential == null)
        {
            Console.WriteLine(
                $"No existing Azure Blob credentials found for {credentialEntryToEdit.EntryTitle}. Please create them first."
            );
            return new EditCredentialProfileStepParameters(request.CredentialProfile);
        }

        if (choice == "Change Shared Access Storage token")
        {
            var newSasToken = Prompt.Password(
                "Enter new Shared Access Storage token",
                validators: [Validators.Required()]
            );

            var newCredential = existingCredential with { SharedAccessSignature = newSasToken };
            await _mediator.Send(
                new UpdateCredentialCommand(
                    credentialsProviderConfiguration,
                    existingCredential.ToCredentialEntry(),
                    newCredential.ToCredentialEntry()
                ),
                cancellationToken
            );
            Console.WriteLine("Azure Blob credential updated successfully!");
        }

        return new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}