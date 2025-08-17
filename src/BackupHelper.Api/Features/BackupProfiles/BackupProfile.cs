namespace BackupHelper.Api.Features.BackupProfiles;

public record BackupProfile(string Name, string BackupPlanLocation, string BackupDirectory, string CredentialProfileName);