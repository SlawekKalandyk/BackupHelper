using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Api.Features.Credentials.CredentialProfiles;

public record CredentialProfile(
    string Name,
    SensitiveString Password,
    IReadOnlyCollection<CredentialEntry> Credentials
) : IDisposable
{
    public void Dispose()
    {
        Password.Dispose();
        foreach (var credential in Credentials)
            credential.Dispose();
    }
}
