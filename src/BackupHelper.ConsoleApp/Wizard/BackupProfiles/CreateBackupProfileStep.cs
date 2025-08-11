using BackupHelper.Api.Features.BackupProfiles;
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

    public async Task<IWizardParameters?> Handle(CreateBackupProfileStepParameters request, CancellationToken cancellationToken)
    {
        var name = Prompt.Input<string>("Enter backup profile name", validators: [Validators.Required()]);
        var backupPlanLocation = Prompt.Input<string>("Select backup plan location", validators: [Validators.Required(), ValidatorsHelper.FileExists]);
        var backupDirectory = Prompt.Input<string>("Select backup directory", validators: [Validators.Required(), ValidatorsHelper.DirectoryExists]);
        var keePassDbLocation = Prompt.Input<string>("Select KeePass database location", validators: [Validators.Required(), ValidatorsHelper.FileExists]);

        await _mediator.Send(
            new CreateBackupProfileCommand(
                name,
                backupPlanLocation,
                backupDirectory,
                keePassDbLocation),
            cancellationToken);

        Console.WriteLine("Backup profile created successfully!");

        return new ManageBackupProfilesStepParameters();
    }
}