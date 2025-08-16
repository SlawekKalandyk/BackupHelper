using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record GetCredentialProfileNamesQuery : IRequest<IReadOnlyList<string>>;

public class GetCredentialProfileNamesQueryHandler : IRequestHandler<GetCredentialProfileNamesQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public GetCredentialProfileNamesQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task<IReadOnlyList<string>> Handle(GetCredentialProfileNamesQuery request, CancellationToken cancellationToken)
    {
        var credentialProfilesPath = _applicationDataHandler.GetCredentialProfilesPath();
        var credentialProfiles = Directory.GetFiles(credentialProfilesPath).Select(Path.GetFileName);

        return Task.FromResult<IReadOnlyList<string>>(credentialProfiles.ToList());
    }
}