using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.SMB;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record GetSMBCredentialQuery(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    string Server,
    string ShareName
) : IRequest<CredentialEntry?>;

public class GetSMBCredentialQueryHandler : IRequestHandler<GetSMBCredentialQuery, CredentialEntry?>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public GetSMBCredentialQueryHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<CredentialEntry?> Handle(
        GetSMBCredentialQuery request,
        CancellationToken cancellationToken
    )
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        var credentialTitle = new SMBCredentialTitle(request.Server, request.ShareName);
        var credential = credentialsProvider.GetCredential<SMBCredential>(credentialTitle);
        return Task.FromResult(credential?.ToCredentialEntry());
    }
}