using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record UpdateCredentialProfileNameStepParameters(CredentialProfile CredentialProfile)
    : IWizardParameters;

public class UpdateCredentialProfileNameStep
    : IWizardStep<UpdateCredentialProfileNameStepParameters>
{
    private readonly IMediator _mediator;

    public UpdateCredentialProfileNameStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(
        UpdateCredentialProfileNameStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var credentialProfile = request.CredentialProfile;
        var newName = Prompt.Input<string>(
            "Enter new credential profile name",
            validators: [Validators.Required()]
        );
        var profileExists = await _mediator.Send(
            new CheckCredentialProfileExistsQuery(newName),
            cancellationToken
        );

        if (profileExists)
        {
            Console.WriteLine(
                $"Credential profile '{newName}' already exists. Please choose a different name."
            );

            return new UpdateCredentialProfileNameStepParameters(credentialProfile);
        }

        await _mediator.Send(
            new UpdateCredentialProfileNameCommand(credentialProfile, newName),
            cancellationToken
        );
        var backupProfiles = await _mediator.Send(new GetBackupProfilesQuery(), cancellationToken);

        foreach (var backupProfile in backupProfiles)
        {
            var updatedBackupProfile = backupProfile with { CredentialProfileName = newName };
            await _mediator.Send(
                new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile),
                cancellationToken
            );
        }

        Console.WriteLine("Credential profile name updated successfully!");

        return new EditCredentialProfileStepParameters(credentialProfile);
    }
}
