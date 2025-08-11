using BackupHelper.Abstractions;
using BackupHelper.Core.Features;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public record CreateBackupStepParameters : IWizardParameters;

public class CreateBackupStep : IWizardStep<CreateBackupStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public CreateBackupStep(IMediator mediator, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(CreateBackupStepParameters parameters, CancellationToken cancellationToken)
    {
        var useProfile = Prompt.Confirm("Do you want to use an existing backup profile?");

        if (useProfile)
        {
            var backupProfiles = await _mediator.Send(new GetBackupProfileNamesQuery(), cancellationToken);
            var backupProfileName = Prompt.Select("Select a backup profile", backupProfiles, 5);
            var backupProfile = await _mediator.Send(new GetBackupProfileQuery(backupProfileName), cancellationToken);

            if (backupProfile == null)
            {
                Console.WriteLine($"Backup profile '{backupProfileName}' not found.");

                return parameters;
            }

            return new SelectKeePassDatabaseStepParameters(
                backupProfile.BackupPlanLocation,
                backupProfile.BackupDirectory,
                backupProfile.KeePassDbLocation);
        }

        var backupPlanLocation = Prompt.Input<string>("Select backup plan location", validators: [Validators.Required()]);
        var outputZipPath = Prompt.Input<string>("Select output zip path", validators: [Validators.Required()]);

        return new SelectKeePassDatabaseStepParameters(backupPlanLocation, outputZipPath);
    }
}