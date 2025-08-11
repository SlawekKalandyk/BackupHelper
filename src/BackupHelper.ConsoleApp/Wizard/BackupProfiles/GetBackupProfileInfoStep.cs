using BackupHelper.Api.Features.BackupProfiles;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.BackupProfiles;

public record GetBackupProfileInfoStepParameters : IWizardParameters;

public class GetBackupProfileInfoStep : IWizardStep<GetBackupProfileInfoStepParameters>
{
    private readonly IMediator _mediator;

    public GetBackupProfileInfoStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(GetBackupProfileInfoStepParameters request, CancellationToken cancellationToken)
    {
        var backupProfileNames = await _mediator.Send(new GetBackupProfileNamesQuery(), cancellationToken);

        if (backupProfileNames.Count == 0)
        {
            Console.WriteLine("No backup profiles available to edit.");

            return new ManageBackupProfilesStepParameters();
        }

        var backupProfileName = Prompt.Select("Select a backup profile to view", backupProfileNames, 5);
        var backupProfile = await _mediator.Send(new GetBackupProfileQuery(backupProfileName), cancellationToken);

        if (backupProfile == null)
        {
            Console.WriteLine($"Backup profile '{backupProfileName}' not found.");

            return new ManageBackupProfilesStepParameters();
        }

        Console.WriteLine($"Backup Profile Name: {backupProfile.Name}");
        Console.WriteLine($"Backup Plan Location: {backupProfile.BackupPlanLocation}");
        Console.WriteLine($"Backup Directory: {backupProfile.BackupDirectory}");
        Console.WriteLine($"KeePass Database Location: {backupProfile.KeePassDbLocation}");

        return new ManageBackupProfilesStepParameters();
    }
}