using BackupHelper.Api.Features.BackupProfiles;
using MediatR;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard.BackupProfiles;

public record DeleteBackupProfileStepParameters : IWizardParameters;

public class DeleteBackupProfileStep : IWizardStep<DeleteBackupProfileStepParameters>
{
    private readonly IMediator _mediator;

    public DeleteBackupProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(
        DeleteBackupProfileStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var backupProfileNames = await _mediator.Send(
            new GetBackupProfileNamesQuery(),
            cancellationToken
        );

        if (backupProfileNames.Count == 0)
        {
            Console.WriteLine("No backup profiles available to delete.");

            return new ManageBackupProfilesStepParameters();
        }

        var backupProfileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a backup profile to delete")
                .PageSize(5)
                .AddChoices(backupProfileNames)
        );
        var confirmation = AnsiConsole.Confirm(
            $"Are you sure you want to delete the backup profile '{backupProfileName}'?"
        );

        if (confirmation)
        {
            await _mediator.Send(
                new DeleteBackupProfileCommand(backupProfileName),
                cancellationToken
            );
        }

        Console.WriteLine($"Backup profile '{backupProfileName}' deleted successfully!");

        return new ManageBackupProfilesStepParameters();
    }
}
