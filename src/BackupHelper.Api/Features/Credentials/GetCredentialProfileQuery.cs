using BackupHelper.Abstractions;
using BackupHelper.Core.Credentials;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record GetCredentialProfileQuery(string Name, string Password) : IRequest<CredentialProfile?>;

public class GetCredentialProfileQueryHandler : IRequestHandler<GetCredentialProfileQuery, CredentialProfile?>
{
    private readonly IApplicationDataHandler _applicationDataHandler;
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public GetCredentialProfileQueryHandler(IApplicationDataHandler applicationDataHandler,
                                            ICredentialsProviderFactory credentialsProviderFactory)
    {
        _applicationDataHandler = applicationDataHandler;
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<CredentialProfile?> Handle(GetCredentialProfileQuery request, CancellationToken cancellationToken)
    {
        var credentialProfilesPath = _applicationDataHandler.GetCredentialProfilesPath();
        var credentialProfileFilePath = Path.Combine(credentialProfilesPath, request.Name);

        if (!File.Exists(credentialProfileFilePath))
        {
            return Task.FromResult<CredentialProfile?>(null);
        }

        var keePassCredentialsProviderConfiguration = new KeePassCredentialsProviderConfiguration(
            credentialProfileFilePath,
            request.Password);
        using var credentialsProvider = _credentialsProviderFactory.Create(keePassCredentialsProviderConfiguration);
        var credentials = credentialsProvider.GetCredentials();
        var credentialProfile = new CredentialProfile(request.Name, credentials);

        return Task.FromResult<CredentialProfile?>(credentialProfile);
    }
}