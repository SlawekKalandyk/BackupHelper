using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.ConsoleApp.Utilities;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.BackupProfiles;

public record EditBackupProfileStepParameters(string? BackupProfileName = null) : IWizardParameters;

public class EditBackupProfileStep : IWizardStep<EditBackupProfileStepParameters>
{
    private readonly IMediator _mediator;

    public EditBackupProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(EditBackupProfileStepParameters request, CancellationToken cancellationToken)
    {
        var backupProfileName = request.BackupProfileName;

        if (string.IsNullOrEmpty(backupProfileName))
        {
            var backupProfileNames = await _mediator.Send(new GetBackupProfileNamesQuery(), cancellationToken);

            if (backupProfileNames.Count == 0)
            {
                Console.WriteLine("No backup profiles available to edit.");

                return new ManageBackupProfilesStepParameters();
            }

            backupProfileName = Prompt.Select("Select a backup profile to edit", backupProfileNames, 5);
        }

        var backupProfile = await _mediator.Send(new GetBackupProfileQuery(backupProfileName), cancellationToken);

        if (backupProfile == null)
        {
            Console.WriteLine($"Backup profile '{backupProfileName}' not found.");

            return new ManageBackupProfilesStepParameters();
        }

        var choice = Prompt.Select(
            "Select property to edit",
            [
                "Show Backup Profile Info",
                "Name",
                "Backup Plan Location",
                "Backup Directory",
                "KeePass Database Location",
                "Cancel"
            ]);

        if (choice == "Cancel")
        {
            return new ManageBackupProfilesStepParameters();
        }
        else if (choice == "Show Backup Profile Info")
        {
            await _mediator.Send(new ShowBackupProfileInfoStepParameters(backupProfile), cancellationToken);
            return new EditBackupProfileStepParameters(backupProfile.Name);
        }
        else if (choice == "Name")
        {
            var newName = Prompt.Input<string>("Enter new name", validators: [Validators.Required()]);
            var updatedBackupProfile = backupProfile with { Name = newName };
            await _mediator.Send(new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile), cancellationToken);
            Console.WriteLine("Backup profile name updated successfully!");
        }
        else if (choice == "Backup Plan Location")
        {
            var newLocation = Prompt.Input<string>("Enter new backup plan location", validators: [Validators.Required(), ValidatorsHelper.FileExists]);
            var updatedBackupProfile = backupProfile with { BackupPlanLocation = newLocation };
            await _mediator.Send(new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile), cancellationToken);
            Console.WriteLine("Backup plan location updated successfully!");
        }
        else if (choice == "Backup Directory")
        {
            var newDirectory = Prompt.Input<string>("Enter new backup directory", validators: [Validators.Required(), ValidatorsHelper.DirectoryExists]);
            var updatedBackupProfile = backupProfile with { BackupDirectory = newDirectory };
            await _mediator.Send(new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile), cancellationToken);
            Console.WriteLine("Backup directory updated successfully!");
        }
        else if (choice == "KeePass Database Location")
        {
            var newKeePassDbLocation = Prompt.Input<string>("Enter new KeePass database location", validators: [Validators.Required(), ValidatorsHelper.FileExists]);
            var updatedBackupProfile = backupProfile with { KeePassDbLocation = newKeePassDbLocation };
            await _mediator.Send(new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile), cancellationToken);
            Console.WriteLine("KeePass database location updated successfully!");
        }

        return new ManageBackupProfilesStepParameters();
    }
}