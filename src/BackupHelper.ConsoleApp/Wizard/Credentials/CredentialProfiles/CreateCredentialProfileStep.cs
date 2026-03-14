using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using MediatR;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;

public record CreateCredentialProfileStepParameters : IWizardParameters;

public class CreateCredentialProfileStep : IWizardStep<CreateCredentialProfileStepParameters>
{
    private readonly IMediator _mediator;

    public CreateCredentialProfileStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(
        CreateCredentialProfileStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var name = AnsiConsole.Ask<string>("Enter credential profile name");
        var profileExists = await _mediator.Send(
            new CheckCredentialProfileExistsQuery(name),
            cancellationToken
        );

        if (profileExists)
        {
            Console.WriteLine(
                $"Credential profile '{name}' already exists. Please choose a different name."
            );

            return new CreateCredentialProfileStepParameters();
        }

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter credential profile password")
                .Secret()
        ).ToCharArray();
        var sensitivePassword = new SensitiveString(password); // zeroes password array

        await _mediator.Send(
            new CreateCredentialProfileCommand(name, sensitivePassword),
            cancellationToken
        );

        Console.WriteLine("Credential profile created successfully!");

        return new ManageCredentialProfilesStepParameters();
    }
}
