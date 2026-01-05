using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.ConsoleApp.Wizard.Credentials.Azure;
using BackupHelper.ConsoleApp.Wizard.Credentials.SMB;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;

public record EditCredentialProfileStepParameters(CredentialProfile? CredentialProfile = null)
    : IWizardParameters;

public class EditCredentialProfileStep : IWizardStep<EditCredentialProfileStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public EditCredentialProfileStep(
        IMediator mediator,
        IApplicationDataHandler applicationDataHandler
    )
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(
        EditCredentialProfileStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var credentialProfile = request.CredentialProfile;

        if (credentialProfile == null)
        {
            var credentialProfileNames = await _mediator.Send(
                new GetCredentialProfileNamesQuery(),
                cancellationToken
            );

            if (credentialProfileNames.Count == 0)
            {
                Console.WriteLine("No credential profiles available to edit.");

                return new ManageCredentialProfilesStepParameters();
            }

            var credentialProfileName = Prompt.Select(
                "Select a credential profile to edit",
                credentialProfileNames,
                5
            );
            var password = Prompt.Password(
                "Enter the password for the credential profile",
                validators: [Validators.Required()]
            );
            credentialProfile = await _mediator.Send(
                new GetCredentialProfileQuery(credentialProfileName, password),
                cancellationToken
            );
        }

        if (credentialProfile == null)
        {
            Console.WriteLine(
                "Credential profile not found or incorrect password. Please try again."
            );

            return new ManageCredentialProfilesStepParameters();
        }

        var choice = Prompt.Select(
            "Select property to edit",
            [
                "Name",
                "Add Azure Blob Credential",
                "Edit Azure Blob Credential",
                "Add SMB Credential",
                "Edit SMB Credential",
                "Delete Credential",
                "Cancel",
            ]
        );

        if (choice == "Cancel")
        {
            return new ManageCredentialProfilesStepParameters();
        }

        if (choice == "Name")
        {
            return new UpdateCredentialProfileNameStepParameters(credentialProfile);
        }

        if (choice == "Add Azure Blob Credential")
        {
            return new AddAzureBlobCredentialStepParameters(credentialProfile);
        }

        if (choice == "Edit Azure Blob Credential")
        {
            return new EditAzureBlobCredentialStepParameters(credentialProfile);
        }

        if (choice == "Add SMB Credential")
        {
            return new AddSMBCredentialStepParameters(credentialProfile);
        }

        if (choice == "Edit SMB Credential")
        {
            return new EditSMBCredentialStepParameters(credentialProfile);
        }

        if (choice == "Delete Credential")
        {
            return new DeleteCredentialStepParameters(credentialProfile);
        }

        Console.WriteLine("Unknown choice. Please try again.");

        return new ManageCredentialProfilesStepParameters();
    }
}