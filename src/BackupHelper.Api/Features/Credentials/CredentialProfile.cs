using BackupHelper.Abstractions;

namespace BackupHelper.Api.Features.Credentials;

public record CredentialProfile(string Name, IReadOnlyCollection<CredentialEntry> Credentials);