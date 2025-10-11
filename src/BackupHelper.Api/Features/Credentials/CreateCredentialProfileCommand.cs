using BackupHelper.Abstractions;
using BackupHelper.Core.Credentials;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record CreateCredentialProfileCommand(
    string CredentialProfileName,
    string CredentialProfilePassword
) : IRequest;

public class CreateCredentialProfileCommandHandler : IRequestHandler<CreateCredentialProfileCommand>
{
    private readonly IApplicationDataHandler _applicationDataHandler;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public CreateCredentialProfileCommandHandler(
        IApplicationDataHandler applicationDataHandler,
        ICredentialsProviderFactory credentialsProviderFactory
    )
    {
        _applicationDataHandler = applicationDataHandler;
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(CreateCredentialProfileCommand request, CancellationToken cancellationToken)
    {
        var credentialProfilePath = Path.Combine(
            _applicationDataHandler.GetCredentialProfilesPath(),
            request.CredentialProfileName
        );

        if (File.Exists(credentialProfilePath))
        {
            throw new InvalidOperationException(
                $"Credential profile with name '{request.CredentialProfileName}' already exists."
            );
        }

        var keePassCredentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            credentialProfilePath,
            request.CredentialProfilePassword
        );
        using var _ = _credentialsProviderFactory.Create(keePassCredentialsProviderConfiguration);

        return Task.CompletedTask;
    }
}
