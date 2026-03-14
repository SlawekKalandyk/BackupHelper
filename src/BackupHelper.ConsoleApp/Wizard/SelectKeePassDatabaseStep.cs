using BackupHelper.Abstractions.Credentials;
using BackupHelper.Core.Credentials;
using Spectre.Console;

namespace BackupHelper.ConsoleApp.Wizard;

public record SelectKeePassDatabaseStepParameters(
    string BackupPlanLocation,
    string OutputDirectory,
    string? KeePassDbLocation = null
) : IWizardParameters;

public class SelectKeePassDatabaseStep : IWizardStep<SelectKeePassDatabaseStepParameters>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public SelectKeePassDatabaseStep(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<IWizardParameters?> Handle(
        SelectKeePassDatabaseStepParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var keePassDbLocation = parameters.KeePassDbLocation;

        if (string.IsNullOrEmpty(parameters.KeePassDbLocation))
        {
            var selectKeePassDb = AnsiConsole.Confirm("Do you want to select an existing KeePass DB?");

            if (!selectKeePassDb)
            {
                return Task.FromResult<IWizardParameters?>(
                    new PerformBackupStepParameters(
                        parameters.BackupPlanLocation,
                        parameters.OutputDirectory
                    )
                );
            }

            keePassDbLocation = AnsiConsole.Ask<string>("Enter KeePass DB location");
        }

        if (File.Exists(keePassDbLocation))
        {
            var masterPassword = GetKeePassDbPassword(keePassDbLocation);
            var defaultCredentialsProviderConfiguration =
                new KeePassCredentialsProviderConfiguration(
                    keePassDbLocation,
                    () => masterPassword.Clone()
                );
            _credentialsProviderFactory.SetDefaultCredentialsProviderConfiguration(
                defaultCredentialsProviderConfiguration
            );

            return Task.FromResult<IWizardParameters?>(
                new PerformBackupStepParameters(
                    parameters.BackupPlanLocation,
                    parameters.OutputDirectory,
                    keePassDbLocation
                )
            );
        }
        else
        {
            Console.WriteLine("KeePass DB file does not exist.");

            return Task.FromResult<IWizardParameters?>(parameters);
        }
    }

    private SensitiveString GetKeePassDbPassword(string keePassDbLocation)
    {
        SensitiveString? sensitivePassword = null;

        while (sensitivePassword == null)
        {
            var keePassDbPassword = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter KeePass DB password")
                    .Secret()
            ).ToCharArray();
            sensitivePassword = new SensitiveString(keePassDbPassword);
            var correctPasswordProvided = KeePassCredentialsProvider.CanLogin(
                keePassDbLocation,
                () => sensitivePassword.Clone()
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
}