using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackupHelper.Core.BackupZipping;

[JsonConverter(typeof(BackupEntryConverter))]
public abstract class BackupEntry { }

public class BackupEntryConverter : JsonConverter<BackupEntry>
{
    public override BackupEntry? ReadJson(JsonReader reader, Type objectType, BackupEntry? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);

            // File entry as object (has "path" property)
            if (obj.ContainsKey("path"))
            {
                var filePath = obj["path"]?.ToString();
                var cronExpression = obj["cronExpression"]?.ToString();
                var timeZone = obj["timeZone"]?.ToString();

                return new BackupFileEntry
                {
                    FilePath = filePath,
                    CronExpression = cronExpression,
                    TimeZone = timeZone ?? string.Empty
                };
            }

            // Directory entry (has "name" and "items")
            if (obj.ContainsKey("name") && obj.ContainsKey("items"))
            {
                var name = obj["name"]?.ToString();
                var items = obj["items"]?.ToObject<List<BackupEntry>>(serializer);

                return new BackupDirectoryEntry
                {
                    DirectoryName = name,
                    Items = items ?? new List<BackupEntry>()
                };
            }
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, BackupEntry? value, JsonSerializer serializer)
    {
        if (value == null)
            return;

        if (value is BackupFileEntry fileEntry)
        {
            serializer.Serialize(writer, fileEntry);
        }
        else if (value is BackupDirectoryEntry dirEntry)
        {
            serializer.Serialize(writer, dirEntry);
        }
    }
}

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

    [JsonProperty("encryptHeaders")]

    public bool EncryptHeaders { get; set; } = false;

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