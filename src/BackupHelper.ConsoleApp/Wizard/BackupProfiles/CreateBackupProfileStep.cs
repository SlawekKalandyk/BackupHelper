using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.ConsoleApp.Utilities;
using MediatR;
using Spectre.Console;

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
        var name = AnsiConsole.Ask<string>("Enter backup profile name");
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

        var backupPlanLocation = AnsiConsole.Prompt(
            new TextPrompt<string>("Select backup plan location")
                .Validate(ValidatorsHelper.FileExists)
        );
        var workingDirectory = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter working directory for temporary files (leave blank to use temp directory)")
                .AllowEmpty()
                .Validate(ValidatorsHelper.DirectoryExistsIfNotEmpty)
        );
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