using BackupHelper.Core.Credentials;
using BackupHelper.Core.Features;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard;

public class AddSmbCredentialStepParameters : IWizardParameters
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

public class AddSmbCredentialStep : IWizardStep<AddSmbCredentialStepParameters>
{
    private readonly IMediator _mediator;

    public AddSmbCredentialStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(AddSmbCredentialStepParameters parameters, CancellationToken cancellationToken)
    {
        var keePassDbLocation = parameters.KeePassDbLocation ?? Prompt.Input<string>("Enter KeePass DB location", validators: [Validators.Required()]);
        var keePassDbPassword = parameters.KeePassDbPassword ?? GetKeePassDbPassword(keePassDbLocation);

        var server = Prompt.Input<string>("Enter SMB server address", validators: [Validators.Required()]);
        var share = Prompt.Input<string>("Enter SMB share name", validators: [Validators.Required()]);
        var username = Prompt.Input<string>("Enter SMB username", validators: [Validators.Required()]);
        var password = Prompt.Password("Enter SMB password", validators: [Validators.Required()]);

        var credentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(keePassDbLocation, keePassDbPassword);
        await _mediator.Send(new AddSMBCredentialCommand(credentialsProviderConfiguration, server, share, username, password), cancellationToken);

        Console.WriteLine("SMB credential added successfully.");
        var addAnother = Prompt.Confirm("Do you want to add another SMB credential?");

        return addAnother
                   ? new AddSmbCredentialStepParameters(keePassDbLocation, keePassDbPassword)
                   : new MainMenuStepParameters();
    }

    private string GetKeePassDbPassword(string keePassDbLocation)
    {
        string? keePassDbPassword = null;

        if (File.Exists(keePassDbLocation))
        {
            keePassDbPassword ??= Prompt.Password("Enter KeePass DB password");
        }
        else
        {
            Console.WriteLine("KeePass DB file does not exist. It will be created.");

            while (keePassDbPassword == null)
            {
                keePassDbPassword = Prompt.Password("Enter new KeePass DB password");
                var confirm = Prompt.Password("Confirm password");

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