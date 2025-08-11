namespace BackupHelper.Api;

public record BackupProfile(string Name, string BackupPlanLocation, string BackupDirectory, string KeePassDbLocation);