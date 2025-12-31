using BackupHelper.Abstractions;

namespace BackupHelper.Api.Features.Credentials.CredentialProfiles;

public record CredentialProfile(
    string Name,
    string Password,
    IReadOnlyCollection<CredentialEntry> Credentials
);
