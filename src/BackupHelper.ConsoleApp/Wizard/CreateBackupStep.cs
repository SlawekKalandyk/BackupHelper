using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard;

public record CreateBackupStepParameters : IWizardParameters;

public class CreateBackupStep : IWizardStep<CreateBackupStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;
    private ICredentialsProviderFactory _credentialsProviderFactory;

    public CreateBackupStep(
        IMediator mediator,
        IApplicationDataHandler applicationDataHandler,
        ICredentialsProviderFactory credentialsProviderFactory
    )
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public async Task<IWizardParameters?> Handle(
        CreateBackupStepParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var useProfile = AnsiConsole.Confirm("Do you want to use an existing backup profile?");

        if (useProfile)
        {
            var backupProfiles = await _mediator.Send(
                new GetBackupProfileNamesQuery(),
                cancellationToken
            );
            var backupProfileName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a backup profile")
                    .PageSize(5)
                    .AddChoices(backupProfiles)
            );
            var backupProfile = await _mediator.Send(
                new GetBackupProfileQuery(backupProfileName),
                cancellationToken
            );

            if (backupProfile == null)
            {
                Console.WriteLine($"Backup profile '{backupProfileName}' not found.");

                return parameters;
            }

            var credentialProfileExists = await _mediator.Send(
                new CheckCredentialProfileExistsQuery(backupProfile.CredentialProfileName),
                cancellationToken
            );

            if (!credentialProfileExists)
            {
                Console.WriteLine(
                    $"Credential profile '{backupProfile.CredentialProfileName}' not found. Please create it first."
                );

                return new MainMenuStepParameters();
            }

            var keePassDbLocation = Path.Combine(
                _applicationDataHandler.GetCredentialProfilesPath(),
                backupProfile.CredentialProfileName
            );
            using var masterPassword = GetCredentialProfilePassword(keePassDbLocation);
            var defaultCredentialsProviderConfiguration =
                new KeePassCredentialsProviderConfiguration(
                    keePassDbLocation,
                    masterPassword
                );
            _credentialsProviderFactory.SetDefaultCredentialsProviderConfiguration(
                defaultCredentialsProviderConfiguration
            );

            var canBackup = await CanBackupWithCredentialProfile(
                backupProfile.CredentialProfileName,
                masterPassword,
                cancellationToken
            );

            return canBackup
                ? new PerformBackupStepParameters(
                    backupProfile.BackupPlanLocation,
                    backupProfile.WorkingDirectory,
                    keePassDbLocation
                )
                : new MainMenuStepParameters();
        }

        var backupPlanLocation = AnsiConsole.Ask<string>("Select backup plan location");
        var outputDirectory = AnsiConsole.Ask<string>("Select output directory");

        return new SelectKeePassDatabaseStepParameters(backupPlanLocation, outputDirectory);
    }

    private SensitiveString GetCredentialProfilePassword(string keePassDbLocation)
    {
        SensitiveString? sensitivePassword = null;

        while (sensitivePassword == null)
        {
            sensitivePassword = SecureConsole.PromptPassword("Enter credential profile password");
            var correctPasswordProvided = KeePassCredentialsProvider.CanLogin(
                keePassDbLocation,
                sensitivePassword
            );

            if (!correctPasswordProvided)
            {
                Console.WriteLine("Incorrect password. Please try again.");
                sensitivePassword.Dispose();
                sensitivePassword = null;
            }
        }

        return sensitivePassword;
    }

    private async Task<bool> CanBackupWithCredentialProfile(
        string credentialProfileName,
        SensitiveString credentialProfilePassword,
        CancellationToken cancellationToken
    )
    {
        var nonConnectedCredentials = await _mediator.Send(
            new CheckCredentialsConnectivityQuery(credentialProfileName, credentialProfilePassword),
            cancellationToken
        );

        if (nonConnectedCredentials.Count > 0)
        {
            Console.WriteLine(
                "The following credentials could not connect to their respective resources:"
            );

            foreach (var credential in nonConnectedCredentials)
            {
                Console.WriteLine(string.Join(Environment.NewLine, credential.ToDisplayString()));
            }

            var backupAnyway = AnsiConsole.Confirm(
                "Do you want to proceed with the backup anyway?",
                false
            );

            return backupAnyway;
        }

        return true;
    }
}