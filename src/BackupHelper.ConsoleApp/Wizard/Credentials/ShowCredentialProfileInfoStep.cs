using BackupHelper.Api.Features.Credentials;
using MediatR;
using Sharprompt;

namespace BackupHelper.ConsoleApp.Wizard.Credentials;

public record ShowCredentialProfileInfoStepParameters(CredentialProfile? CredentialProfile = null) : IWizardParameters;

public class ShowCredentialProfileInfoStep : IWizardStep<ShowCredentialProfileInfoStepParameters>
{
    private readonly IMediator _mediator;

    public ShowCredentialProfileInfoStep(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IWizardParameters?> Handle(ShowCredentialProfileInfoStepParameters request, CancellationToken cancellationToken)
    {
        var credentialProfile = request.CredentialProfile;

        if (credentialProfile == null)
        {
            var credentialProfileNames = await _mediator.Send(new GetCredentialProfileNamesQuery(), cancellationToken);

            if (credentialProfileNames.Count == 0)
            {
                Console.WriteLine("No credential profiles available to view.");

                return new ManageCredentialProfilesStepParameters();
            }

            var credentialProfileName = Prompt.Select("Select a credential profile to view", credentialProfileNames, 5);

            var password = Prompt.Password("Enter the password for the credential profile", validators: [Validators.Required()]);
            credentialProfile = await _mediator.Send(new GetCredentialProfileQuery(credentialProfileName, password), cancellationToken);

            if (credentialProfile == null)
            {
                Console.WriteLine($"Credential profile '{credentialProfileName}' not found.");

                return new ManageCredentialProfilesStepParameters();
            }
        }

        Console.WriteLine($"Credential Profile: {credentialProfile.Name}");

        foreach (var credential in credentialProfile.Credentials)
        {
            Console.WriteLine(credential.ToDisplayString());
        }

        return new ManageCredentialProfilesStepParameters();
    }
}