using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record CheckCredentialProfileExistsQuery(string CredentialProfileName) : IRequest<bool>;

public class CheckCredentialProfileExistsQueryHandler
    : IRequestHandler<CheckCredentialProfileExistsQuery, bool>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public CheckCredentialProfileExistsQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task<bool> Handle(
        CheckCredentialProfileExistsQuery request,
        CancellationToken cancellationToken
    )
    {
        var credentialProfilePath = Path.Combine(
            _applicationDataHandler.GetCredentialProfilesPath(),
            request.CredentialProfileName
        );
        var credentialProfileExists = File.Exists(credentialProfilePath);

        return Task.FromResult(credentialProfileExists);
    }
}
