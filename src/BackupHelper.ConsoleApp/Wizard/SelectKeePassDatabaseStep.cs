using BackupHelper.Core.Credentials;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class SelectKeePassDatabaseStep : WizardStepBase<SelectKeePassDatabaseStepParameters>
{
    public SelectKeePassDatabaseStep(SelectKeePassDatabaseStepParameters parameters) : base(parameters) { }

    public override Task<IWizardStep?> Execute()
    {
        var selectKeePassDb = Prompt.Confirm("Do you want to select an existing KeePass DB?");

        if (!selectKeePassDb)
        {
            return Task.FromResult<IWizardStep?>(new PerformBackupStep(new(Parameters.BackupPlanLocation, Parameters.OutputZipPath)));
        }

        var keePassDbLocation = Prompt.Input<string>("Enter KeePass DB location: ", validators: [Validators.Required()]);

        if (File.Exists(keePassDbLocation))
        {
            var keePassDbPassword = GetKeePassDbPassword(keePassDbLocation);

            return Task.FromResult<IWizardStep?>(
                new PerformBackupStep(new(Parameters.BackupPlanLocation, Parameters.OutputZipPath, keePassDbLocation, keePassDbPassword)));
        }
        else
        {
            Console.WriteLine("KeePass DB file does not exist.");

            return Task.FromResult<IWizardStep?>(new SelectKeePassDatabaseStep(Parameters));
        }
    }

    private string GetKeePassDbPassword(string keePassDbLocation)
    {
        string? keePassDbPassword = null;

        while (keePassDbPassword == null)
        {
            keePassDbPassword = Prompt.Password("Enter KeePass DB password: ");
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

public record SelectKeePassDatabaseStepParameters(string BackupPlanLocation, string OutputZipPath);