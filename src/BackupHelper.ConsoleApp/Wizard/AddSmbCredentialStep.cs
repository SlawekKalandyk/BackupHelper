using BackupHelper.Core.Credentials;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class AddSmbCredentialStep : WizardStepBase<AddSmbCredentialStepParameters>
{
    public AddSmbCredentialStep(AddSmbCredentialStepParameters parameters) : base(parameters) { }

    public override Task<IWizardStep?> Execute()
    {
        var keePassDbLocation = Parameters.KeePassDbLocation ?? Prompt.Input<string>("Enter KeePass DB location", validators: [Validators.Required()]);
        var keePassDbPassword = Parameters.KeePassDbPassword ?? GetKeePassDbPassword(keePassDbLocation);

        using var keePassCredentialsProvider = new KeePassCredentialsProvider(keePassDbLocation, keePassDbPassword);

        var server = Prompt.Input<string>("Enter SMB server address: ", validators: [Validators.Required()]);
        var share = Prompt.Input<string>("Enter SMB share name: ", validators: [Validators.Required()]);
        var username = Prompt.Input<string>("Enter SMB username: ", validators: [Validators.Required()]);
        var password = Prompt.Password("Enter SMB password: ", validators: [Validators.Required()]);
        var credentialTitle = $@"\\{server}\{share}";
        keePassCredentialsProvider.SetCredential(credentialTitle, username, password);

        Console.WriteLine($"SMB credential for {credentialTitle} added successfully.");
        var addAnother = Prompt.Confirm("Do you want to add another SMB credential?");

        return addAnother
                   ? Task.FromResult<IWizardStep?>(new AddSmbCredentialStep(new(keePassDbLocation, keePassDbPassword)))
                   : Task.FromResult<IWizardStep?>(new MainMenuStep(new()));
    }

    private string GetKeePassDbPassword(string keePassDbLocation)
    {
        string? keePassDbPassword = null;

        if (File.Exists(keePassDbLocation))
        {
            keePassDbPassword ??= Prompt.Password("Enter KeePass DB password: ");
        }
        else
        {
            Console.WriteLine("KeePass DB file does not exist. It will be created.");

            while (keePassDbPassword == null)
            {
                keePassDbPassword = Prompt.Password("Enter new KeePass DB password: ");
                var confirm = Prompt.Password("Confirm password: ");

                if (keePassDbPassword != confirm)
                {
                    Console.WriteLine("Passwords do not match. Please try again.");
                    keePassDbPassword = null;
                }
            }
        }

        return keePassDbPassword;
    }
}

public class AddSmbCredentialStepParameters
{
    public AddSmbCredentialStepParameters() { }

    public AddSmbCredentialStepParameters(string keePassDbLocation, string keePassDbPassword)
    {
        KeePassDbLocation = keePassDbLocation;
        KeePassDbPassword = keePassDbPassword;
    }

    public string? KeePassDbPassword { get; set; }

    public string? KeePassDbLocation { get; set; }
}