using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.ConsoleApp.Utilities;
using MediatR;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard.BackupProfiles;

public record EditBackupProfileStepParameters(string? BackupProfileName = null) : IWizardParameters;

public class EditBackupProfileStep : IWizardStep<EditBackupProfileStepParameters>
{
    private readonly IMediator _mediator;

    public EditBackupProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(
        EditBackupProfileStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var backupProfileName = request.BackupProfileName;

        if (string.IsNullOrEmpty(backupProfileName))
        {
            var backupProfileNames = await _mediator.Send(
                new GetBackupProfileNamesQuery(),
                cancellationToken
            );

            if (backupProfileNames.Count == 0)
            {
                Console.WriteLine("No backup profiles available to edit.");

                return new ManageBackupProfilesStepParameters();
            }

            backupProfileName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a backup profile to edit")
                    .PageSize(5)
                    .AddChoices(backupProfileNames)
            );
        }

        var backupProfile = await _mediator.Send(
            new GetBackupProfileQuery(backupProfileName),
            cancellationToken
        );

        if (backupProfile == null)
        {
            Console.WriteLine($"Backup profile '{backupProfileName}' not found.");

            return new ManageBackupProfilesStepParameters();
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select property to edit")
                .AddChoices(
                    "Show Backup Profile Info",
                    "Name",
                    "Backup Plan Location",
                    "Change Credential Profile",
                    "Change Working Directory",
                    "Cancel"
                )
        );

        if (choice == "Cancel")
        {
            return new ManageBackupProfilesStepParameters();
        }
        else if (choice == "Show Backup Profile Info")
        {
            await _mediator.Send(
                new ShowBackupProfileInfoStepParameters(backupProfile),
                cancellationToken
            );
            return new EditBackupProfileStepParameters(backupProfile.Name);
        }
        else if (choice == "Name")
        {
            var newName = AnsiConsole.Ask<string>("Enter new name");
            var updatedBackupProfile = backupProfile with { Name = newName };
            await _mediator.Send(
                new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile),
                cancellationToken
            );
            Console.WriteLine("Backup profile name updated successfully!");
        }
        else if (choice == "Backup Plan Location")
        {
            var newLocation = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter new backup plan location")
                    .Validate(ValidatorsHelper.FileExists)
            );
            var updatedBackupProfile = backupProfile with { BackupPlanLocation = newLocation };
            await _mediator.Send(
                new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile),
                cancellationToken
            );
            Console.WriteLine("Backup plan location updated successfully!");
        }
        else if (choice == "Change Credential Profile")
        {
            var credentialProfileNames = await _mediator.Send(
                new GetCredentialProfileNamesQuery(),
                cancellationToken
            );
            var credentialProfileName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a credential profile to use for this backup profile")
                    .PageSize(5)
                    .AddChoices(credentialProfileNames)
            );
            var updatedBackupProfile = backupProfile with
            {
                CredentialProfileName = credentialProfileName,
            };
            await _mediator.Send(
                new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile),
                cancellationToken
            );
            Console.WriteLine("Credential profile updated successfully!");
        }
        else if (choice == "Change Working Directory")
        {
            var newWorkingDirectory = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter new working directory for temporary files (leave blank to use temp directory)")
                    .AllowEmpty()
                    .Validate(ValidatorsHelper.DirectoryExistsIfNotEmpty)
            );
            var updatedBackupProfile = backupProfile with
            {
                WorkingDirectory = newWorkingDirectory,
            };
            await _mediator.Send(
                new UpdateBackupProfileCommand(backupProfile, updatedBackupProfile),
                cancellationToken
            );
            Console.WriteLine("Working directory updated successfully!");
        }

        return new ManageBackupProfilesStepParameters();
    }
}
