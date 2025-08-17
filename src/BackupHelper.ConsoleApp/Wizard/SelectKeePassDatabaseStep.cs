using BackupHelper.Abstractions;
using BackupHelper.Core.Credentials;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public record SelectKeePassDatabaseStepParameters(string BackupPlanLocation, string OutputZipPath, string? KeePassDbLocation = null) : IWizardParameters;

public class SelectKeePassDatabaseStep : IWizardStep<SelectKeePassDatabaseStepParameters>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public SelectKeePassDatabaseStep(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<IWizardParameters?> Handle(SelectKeePassDatabaseStepParameters parameters, CancellationToken cancellationToken)
    {
        var keePassDbLocation = parameters.KeePassDbLocation;

        if (string.IsNullOrEmpty(parameters.KeePassDbLocation))
        {
            var selectKeePassDb = Prompt.Confirm("Do you want to select an existing KeePass DB?");

            if (!selectKeePassDb)
            {
                return Task.FromResult<IWizardParameters?>(new PerformBackupStepParameters(parameters.BackupPlanLocation, parameters.OutputZipPath));
            }

            keePassDbLocation = Prompt.Input<string>("Enter KeePass DB location", validators: [Validators.Required()]);
        }

        if (File.Exists(keePassDbLocation))
        {
            var keePassDbPassword = GetKeePassDbPassword(keePassDbLocation);
            var defaultCredentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(keePassDbLocation, keePassDbPassword);
            _credentialsProviderFactory.SetDefaultCredentialsProviderConfiguration(defaultCredentialsProviderConfiguration);

            return Task.FromResult<IWizardParameters?>(
                new PerformBackupStepParameters(
                    parameters.BackupPlanLocation,
                    parameters.OutputZipPath,
                    keePassDbLocation,
                    keePassDbPassword));
        }
        else
        {
            Console.WriteLine("KeePass DB file does not exist.");

            return Task.FromResult<IWizardParameters?>(parameters);
        }
    }

    private string GetKeePassDbPassword(string keePassDbLocation)
    {
        string? keePassDbPassword = null;

        while (keePassDbPassword == null)
        {
            keePassDbPassword = Prompt.Password("Enter KeePass DB password");
            var correctPasswordProvided = KeePassCredentialsProvider.CanLogin(keePassDbLocation, keePassDbPassword);

            if (!correctPasswordProvided)
            {
                Console.WriteLine("Incorrect password. Please try again.");
                keePassDbPassword = null;
            }
        }

        return keePassDbPassword;
    }
}