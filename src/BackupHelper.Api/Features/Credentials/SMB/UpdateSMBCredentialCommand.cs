using BackupHelper.Abstractions;
using BackupHelper.Sources.SMB;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record UpdateSMBCredentialCommand(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    SMBCredential ExistingCredential,
    SMBCredential NewCredential)
    : IRequest;

public class UpdateSMBCredentialCommandHandler : IRequestHandler<UpdateSMBCredentialCommand>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public UpdateSMBCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(UpdateSMBCredentialCommand request, CancellationToken cancellationToken)
    {
        var existingTitle = SMBCredentialHelper.GetSMBCredentialTitle(request.ExistingCredential.Server, request.ExistingCredential.ShareName);
        var newTitle = SMBCredentialHelper.GetSMBCredentialTitle(request.NewCredential.Server, request.NewCredential.ShareName);

        if (existingTitle != newTitle)
        {
            throw new InvalidOperationException(
                $"Cannot update SMB credential: provided server or share name are different. " +
                $"Existing: '{existingTitle}', New: '{newTitle}'.");
        }

        using var credentialsProvider = _credentialsProviderFactory.Create(request.CredentialsProviderConfiguration);
        var credentials = credentialsProvider.GetCredentials();
        var credential = credentials.FirstOrDefault(c => c.Title == newTitle);

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"SMB credential for server '{request.NewCredential.Server}' and share name '{request.NewCredential.ShareName}' not found.");
        }

        if (credential.Password != request.ExistingCredential.Password)
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        credentialsProvider.UpdateCredential(request.NewCredential.ToCredentialEntry());

        return Task.CompletedTask;
    }
}