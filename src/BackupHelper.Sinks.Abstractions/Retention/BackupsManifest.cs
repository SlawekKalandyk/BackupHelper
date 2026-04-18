namespace BackupHelper.Sinks.Abstractions.Retention;

public sealed class BackupsManifest
{
    public int Version { get; set; } = 1;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<BackupManifestEntry> Backups { get; set; } = [];
}
