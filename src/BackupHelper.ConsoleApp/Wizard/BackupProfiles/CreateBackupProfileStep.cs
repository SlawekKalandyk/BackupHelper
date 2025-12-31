using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.ConsoleApp.Utilities;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.BackupProfiles;

public record CreateBackupProfileStepParameters : IWizardParameters;

public class CreateBackupProfileStep : IWizardStep<CreateBackupProfileStepParameters>
{
    private readonly IMediator _mediator;

    public CreateBackupProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(
        CreateBackupProfileStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var name = Prompt.Input<string>(
            "Enter backup profile name",
            validators: [Validators.Required()]
        );
        var profileExists = await _mediator.Send(
            new CheckBackupProfileExistsQuery(name),
            cancellationToken
        );

        if (profileExists)
        {
            Console.WriteLine(
                $"Backup profile '{name}' already exists. Please choose a different name."
            );

            return new CreateBackupProfileStepParameters();
        }

        var backupPlanLocation = Prompt.Input<string>(
            "Select backup plan location",
            validators: [Validators.Required(), ValidatorsHelper.FileExists]
        );
        var workingDirectory = Prompt.Input<string>(
            "Enter working directory for temporary files (leave blank to use temp directory)",
            defaultValue: string.Empty,
            validators: [ValidatorsHelper.DirectoryExistsIfNotEmpty]
        );
        var credentialProfileNames = await _mediator.Send(
            new GetCredentialProfileNamesQuery(),
            cancellationToken
        );
        var credentialProfileName = Prompt.Select(
            "Select a credential profile to use for this backup profile",
            credentialProfileNames,
            pageSize: 5
        );

        await _mediator.Send(
            new CreateBackupProfileCommand(
                name,
                backupPlanLocation,
                credentialProfileName,
                workingDirectory
            ),
            cancellationToken
        );

        Console.WriteLine("Backup profile created successfully!");

        return new ManageBackupProfilesStepParameters();
    }
}