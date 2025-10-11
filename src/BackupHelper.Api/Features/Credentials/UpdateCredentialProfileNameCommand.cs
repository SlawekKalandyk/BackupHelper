using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record UpdateCredentialProfileNameCommand(
    CredentialProfile CredentialProfile,
    string NewName
) : IRequest;

public class UpdateCredentialProfileNameCommandHandler
    : IRequestHandler<UpdateCredentialProfileNameCommand>
{
    private readonly IApplicationDataHandler _applicationDataHandler;
    private readonly IMediator _mediator;

    public UpdateCredentialProfileNameCommandHandler(
        IApplicationDataHandler applicationDataHandler,
        IMediator mediator
    )
    {
        _applicationDataHandler = applicationDataHandler;
        _mediator = mediator;
    }

    public async Task Handle(
        UpdateCredentialProfileNameCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
        {
            throw new ArgumentException("New name cannot be empty.", nameof(request.NewName));
        }

        var profileExists = await _mediator.Send(
            new CheckCredentialProfileExistsQuery(request.NewName),
            cancellationToken
        );

        if (profileExists)
        {
            throw new InvalidOperationException(
                $"Credential profile with name '{request.NewName}' already exists."
            );
        }

        var credentialProfilePath = Path.Combine(
            _applicationDataHandler.GetCredentialProfilesPath(),
            request.CredentialProfile.Name
        );
        var newCredentialProfilePath = Path.Combine(
            _applicationDataHandler.GetCredentialProfilesPath(),
            request.NewName
        );

        File.Move(credentialProfilePath, newCredentialProfilePath);
    }
}
