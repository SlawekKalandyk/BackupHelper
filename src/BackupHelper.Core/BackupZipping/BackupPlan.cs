using Newtonsoft.Json;

namespace BackupHelper.Core.BackupZipping;

public abstract class BackupEntry { }

public class BackupFileEntry : BackupEntry
{
    private TimeZoneType _timeZoneType = TimeZoneType.Local;

    [JsonProperty("path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonProperty("cronExpression")]
    public string? CronExpression { get; set; } = null;

    [JsonProperty("timeZone")]
    public string TimeZone
    {
        get => _timeZoneType.ToString().ToLowerInvariant();
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _timeZoneType = TimeZoneType.Local;
                return;
            }

            var parsedSuccessfully = Enum.TryParse<TimeZoneType>(value, true, out var timeZoneType);
            if (parsedSuccessfully)
            {
                _timeZoneType = timeZoneType;
            }
            else
            {
                throw new ArgumentException($"Invalid time zone type: {value}. Expected 'local', 'utc' or no value.");
            }
        }
    }

    public TimeZoneInfo GetTimeZoneInfo()
    {
        return _timeZoneType switch
        {
            TimeZoneType.Local => TimeZoneInfo.Local,
            TimeZoneType.Utc => TimeZoneInfo.Utc,
            _ => throw new InvalidOperationException("Time zone type is not set or invalid.")
        };
    }

    private enum TimeZoneType
    {
        Local,
        Utc
    }
}

public class BackupDirectoryEntry : BackupEntry
{
    [JsonProperty("name")]
    public string DirectoryName { get; set; } = string.Empty;

    [JsonProperty("items")]
    public List<BackupEntry> Items { get; set; } = new();
}

public class BackupPlan
{
    [JsonProperty("items")]
    public List<BackupEntry> Items { get; set; } = new();

    [JsonProperty("logDirectory")]
    public string? LogDirectory { get; set; }

    public static BackupPlan FromJsonFile(string inputPath)
    {
        var json = File.ReadAllText(inputPath);
        return JsonConvert.DeserializeObject<BackupPlan>(json)!;
    }

    public void ToJsonFile(string outputPath)
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(outputPath, json);
    }
}