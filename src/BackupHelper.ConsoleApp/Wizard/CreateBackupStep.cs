using BackupHelper.Abstractions;
using BackupHelper.Api.Features.BackupProfiles;
using BackupHelper.Api.Features.Credentials;
using BackupHelper.Core.Credentials;
using BackupHelper.Core.Features;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public record CreateBackupStepParameters : IWizardParameters;

public class CreateBackupStep : IWizardStep<CreateBackupStepParameters>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDataHandler _applicationDataHandler;
    private ICredentialsProviderFactory _credentialsProviderFactory;

    public CreateBackupStep(IMediator mediator, IApplicationDataHandler applicationDataHandler, ICredentialsProviderFactory credentialsProviderFactory)
    {
        _mediator = mediator;
        _applicationDataHandler = applicationDataHandler;
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public async Task<IWizardParameters?> Handle(CreateBackupStepParameters parameters, CancellationToken cancellationToken)
    {
        var useProfile = Prompt.Confirm("Do you want to use an existing backup profile?");

        if (useProfile)
        {
            var backupProfiles = await _mediator.Send(new GetBackupProfileNamesQuery(), cancellationToken);
            var backupProfileName = Prompt.Select("Select a backup profile", backupProfiles, 5);
            var backupProfile = await _mediator.Send(new GetBackupProfileQuery(backupProfileName), cancellationToken);

            if (backupProfile == null)
            {
                Console.WriteLine($"Backup profile '{backupProfileName}' not found.");

                return parameters;
            }

            var credentialProfileExists = await _mediator.Send(new CheckCredentialProfileExistsQuery(backupProfile.CredentialProfileName), cancellationToken);

            if (!credentialProfileExists)
            {
                Console.WriteLine($"Credential profile '{backupProfile.CredentialProfileName}' not found. Please create it first.");

                return new MainMenuStepParameters();
            }

            var keePassDbLocation = Path.Combine(_applicationDataHandler.GetCredentialProfilesPath(), backupProfile.CredentialProfileName);
            var credentialProfilePassword = GetCredentialProfilePassword(keePassDbLocation);
            var defaultCredentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(keePassDbLocation, credentialProfilePassword);
            _credentialsProviderFactory.SetDefaultCredentialsProviderConfiguration(defaultCredentialsProviderConfiguration);

            var canBackup = await CanBackupWithCredentialProfile(backupProfile.CredentialProfileName, credentialProfilePassword, cancellationToken);

            return canBackup
                ? new PerformBackupStepParameters(
                    backupProfile.BackupPlanLocation,
                    backupProfile.BackupDirectory,
                    keePassDbLocation,
                    credentialProfilePassword)
                : new MainMenuStepParameters();
        }

        var backupPlanLocation = Prompt.Input<string>("Select backup plan location", validators: [Validators.Required()]);
        var outputZipPath = Prompt.Input<string>("Select output zip path", validators: [Validators.Required()]);

        return new SelectKeePassDatabaseStepParameters(backupPlanLocation, outputZipPath);
    }

    private string GetCredentialProfilePassword(string keePassDbLocation)
    {
        string? keePassDbPassword = null;

        while (keePassDbPassword == null)
        {
            keePassDbPassword = Prompt.Password("Enter credential profile password");
            var correctPasswordProvided = KeePassCredentialsProvider.CanLogin(keePassDbLocation, keePassDbPassword);

            if (!correctPasswordProvided)
            {
                Console.WriteLine("Incorrect password. Please try again.");
                keePassDbPassword = null;
            }
        }

        return keePassDbPassword;
    }

    private async Task<bool> CanBackupWithCredentialProfile(string credentialProfileName, string credentialProfilePassword, CancellationToken cancellationToken)
    {
        var nonConnectedCredentials = await _mediator.Send(
                                          new CheckCredentialsConnectivityQuery(credentialProfileName, credentialProfilePassword),
                                          cancellationToken);

        if (nonConnectedCredentials.Count > 0)
        {
            Console.WriteLine("The following credentials could not connect to their respective resources:");

            foreach (var credential in nonConnectedCredentials)
            {
                Console.WriteLine(string.Join(Environment.NewLine, credential.ToDisplayString()));
            }

            var backupAnyway = Prompt.Confirm("Do you want to proceed with the backup anyway?", false);

            return backupAnyway;
        }

        return true;
    }
}