namespace BackupHelper.Sinks.Abstractions.Retention;

public sealed class BackupManifestEntry
{
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedUtc { get; set; }
}
