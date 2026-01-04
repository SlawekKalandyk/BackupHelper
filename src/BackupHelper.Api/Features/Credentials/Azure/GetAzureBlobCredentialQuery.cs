using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.Azure;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.Azure;

public record GetAzureBlobCredentialQuery(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    string AccountName
) : IRequest<CredentialEntry?>;

public class GetAzureBlobCredentialQueryHandler
    : IRequestHandler<GetAzureBlobCredentialQuery, CredentialEntry?>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public GetAzureBlobCredentialQueryHandler(
        ICredentialsProviderFactory credentialsProviderFactory
    )
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<CredentialEntry?> Handle(
        GetAzureBlobCredentialQuery request,
        CancellationToken cancellationToken
    )
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        var credential = credentialsProvider.GetCredential<AzureBlobCredential>(
            request.AccountName
        );
        return Task.FromResult(credential?.ToCredentialEntry());
    }
}