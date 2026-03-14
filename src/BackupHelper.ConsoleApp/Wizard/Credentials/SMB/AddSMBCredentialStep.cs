using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Api.Features.Credentials.SMB;
using BackupHelper.Connectors.SMB;
using BackupHelper.ConsoleApp.Utilities;
using BackupHelper.ConsoleApp.Wizard.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard.Credentials.SMB;

public record AddSMBCredentialStepParameters(CredentialProfile CredentialProfile)
    : IWizardParameters;

public class AddSMBCredentialStep : IWizardStep<AddSMBCredentialStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;

    public AddSMBCredentialStep(IMediator mediator, IApplicationDataHandler applicationDataHandler)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IWizardParameters?> Handle(
        AddSMBCredentialStepParameters request,
        CancellationToken cancellationToken
    )
    {
        var keePassDbLocation = Path.Combine(
            _applicationDataHandler.GetCredentialProfilesPath(),
            request.CredentialProfile.Name
        );
        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            keePassDbLocation,
            () => request.CredentialProfile.Password.Clone()
        );

        var server = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter SMB server address")
                .Validate(ValidatorsHelper.IPAddressOrHostname)
        );
        var share = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter SMB share name")
                .Validate(ValidatorsHelper.HasNoInvalidChars)
        );
        var credentialEntry = await _mediator.Send(
            new GetSMBCredentialQuery(credentialsProviderConfiguration, server, share),
            cancellationToken
        );

        if (credentialEntry != null)
        {
            var title = SMBCredentialHelper.GetSMBCredentialTitle(server, share);
            var editCredential = AnsiConsole.Confirm(
                $"SMB credential for {title} exists. Do you want to edit it?"
            );

            if (editCredential)
            {
                return new EditSMBCredentialStepParameters(
                    request.CredentialProfile,
                    credentialEntry
                );
            }
            else
            {
                credentialEntry.Dispose();
                return new EditCredentialProfileStepParameters(request.CredentialProfile);
            }
        }

        var username = AnsiConsole.Ask<string>("Enter SMB username");
        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter SMB password")
                .Secret()
        ).ToCharArray();

        var credential = new SMBCredential(server, share, username, new SensitiveString(password));
        using var credentialEntryToAdd = credential.ToCredentialEntry();
        await _mediator.Send(
            new AddCredentialCommand(
                credentialsProviderConfiguration,
                credentialEntryToAdd
            ),
            cancellationToken
        );

        Console.WriteLine("SMB credential added successfully.");
        var addAnother = AnsiConsole.Confirm("Do you want to add another SMB credential?");

        return addAnother
            ? new AddSMBCredentialStepParameters(request.CredentialProfile)
            : new EditCredentialProfileStepParameters(request.CredentialProfile);
    }
}