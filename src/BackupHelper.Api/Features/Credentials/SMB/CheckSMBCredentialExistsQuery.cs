using BackupHelper.Abstractions;
using BackupHelper.Sources.SMB;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record CheckSMBCredentialExistsQuery(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    string Server,
    string ShareName
) : IRequest<bool>;

public class CheckSMBCredentialExistsQueryHandler
    : IRequestHandler<CheckSMBCredentialExistsQuery, bool>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public CheckSMBCredentialExistsQueryHandler(
        ICredentialsProviderFactory credentialsProviderFactory
    )
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<bool> Handle(
        CheckSMBCredentialExistsQuery request,
        CancellationToken cancellationToken
    )
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        var title = SMBCredentialHelper.GetSMBCredentialTitle(request.Server, request.ShareName);
        var credential = credentialsProvider.GetCredential(title);
        return Task.FromResult(credential != null);
    }
}
