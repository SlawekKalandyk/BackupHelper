using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Api.Features.Credentials.CredentialProfiles;

public record CredentialProfile : IDisposable
{
    public CredentialProfile(
        string name,
        SensitiveString password,
        IReadOnlyCollection<CredentialEntry> credentials
    )
    {
        Name = name;
        Password = password.Clone();
        Credentials = credentials;
    }

    public string Name { get; }
    public SensitiveString Password { get; }
    public IReadOnlyCollection<CredentialEntry> Credentials { get; }

    public void Dispose()
    {
        Password.Dispose();
        foreach (var credential in Credentials)
            credential.Dispose();
    }
}
