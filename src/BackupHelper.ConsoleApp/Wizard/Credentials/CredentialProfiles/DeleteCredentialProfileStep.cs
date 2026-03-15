using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using MediatR;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;

public record DeleteCredentialProfileStepParameters : IWizardParameters;

public class DeleteCredentialProfileStep : IWizardStep<DeleteCredentialProfileStepParameters>
{
    private readonly IMediator _mediator;

    public DeleteCredentialProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(
        DeleteCredentialProfileStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var credentialProfileNames = await _mediator.Send(
            new GetCredentialProfileNamesQuery(),
            cancellationToken
        );

        if (credentialProfileNames.Count == 0)
        {
            Console.WriteLine("No credential profiles available to delete.");
            return new ManageCredentialProfilesStepParameters();
        }

        var credentialProfileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a credential profile to delete")
                .PageSize(5)
                .AddChoices(credentialProfileNames)
        );
        var confirmation = AnsiConsole.Confirm(
            $"Are you sure you want to delete the credential profile '{credentialProfileName}'?"
        );

        if (confirmation)
        {
            await _mediator.Send(
                new DeleteCredentialProfileCommand(credentialProfileName),
                cancellationToken
            );
            Console.WriteLine(
                $"Credential profile '{credentialProfileName}' deleted successfully!"
            );
        }

        return new ManageCredentialProfilesStepParameters();
    }
}
