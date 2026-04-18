using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Azure;
using BackupHelper.Sinks.FileSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackupHelper.Core.BackupZipping;

[JsonConverter(typeof(BackupEntryConverter))]
public abstract class BackupEntry { }

public class BackupFileEntry : BackupEntry
{
    private TimeZoneType _timeZoneType = TimeZoneType.Local;

    [JsonProperty("path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonProperty("cronExpression")]
    public string? CronExpression { get; set; } = null;

    [JsonProperty("compressionLevel")]
    public int? CompressionLevel { get; set; }

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
                throw new ArgumentException(
                    $"Invalid time zone type: {value}. Expected 'local', 'utc' or no value."
                );
            }
        }
    }

    public TimeZoneInfo GetTimeZoneInfo()
    {
        return _timeZoneType switch
        {
            TimeZoneType.Local => TimeZoneInfo.Local,
            TimeZoneType.Utc => TimeZoneInfo.Utc,
            _ => throw new InvalidOperationException("Time zone type is not set or invalid."),
        };
    }

    private enum TimeZoneType
    {
        Local,
        Utc,
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

    [JsonProperty("sinks")]
    public List<ISinkDestination> Sinks { get; set; } = new();

    [JsonProperty("logDirectory")]
    public string? LogDirectory { get; set; }

    [JsonProperty("encryptHeaders")]
    public bool EncryptHeaders { get; set; } = false;

    [JsonProperty("threadLimit")]
    public int? ThreadLimit { get; set; }

    [JsonProperty("memoryLimitMB")]
    public int? MemoryLimitMB { get; set; }

    [JsonProperty("compressionLevel")]
    public int? CompressionLevel { get; set; }

    [JsonProperty("zipFileNameSuffix")]
    public string? ZipFileNameSuffix { get; set; }

    [JsonProperty("sinkUploadParallelism")]
    public int? SinkUploadParallelism { get; set; }

    public static async Task<BackupPlan> FromJsonFileAsync(
        string inputPath,
        CancellationToken cancellationToken = default
    )
    {
        var json = await File.ReadAllTextAsync(inputPath, cancellationToken);

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new SinkDestinationConverter());

        return JsonConvert.DeserializeObject<BackupPlan>(json, settings)!;
    }

    public async Task ToJsonFileAsync(
        string outputPath,
        CancellationToken cancellationToken = default
    )
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new SinkDestinationConverter() },
        };

        var json = JsonConvert.SerializeObject(this, settings);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }
}

public class BackupEntryConverter : JsonConverter<BackupEntry>
{
    public override BackupEntry? ReadJson(
        JsonReader reader,
        Type objectType,
        BackupEntry? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);

            // File entry as object (has "path" property)
            if (obj.ContainsKey("path"))
            {
                var filePath = obj["path"]?.ToString();
                var cronExpression = obj["cronExpression"]?.ToString();
                var compressionLevel = obj["compressionLevel"]?.ToObject<int?>();
                var timeZone = obj["timeZone"]?.ToString();

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new JsonSerializationException(
                        "Backup file entry must contain a non-empty 'path' value."
                    );
                }

                return new BackupFileEntry
                {
                    FilePath = filePath,
                    CronExpression = cronExpression,
                    CompressionLevel = compressionLevel,
                    TimeZone = timeZone ?? string.Empty,
                };
            }

            // Directory entry (has "name" and "items")
            if (obj.ContainsKey("name") && obj.ContainsKey("items"))
            {
                var name = obj["name"]?.ToString();
                var items = obj["items"]?.ToObject<List<BackupEntry>>(serializer);

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new JsonSerializationException(
                        "Backup directory entry must contain a non-empty 'name' value."
                    );
                }

                return new BackupDirectoryEntry
                {
                    DirectoryName = name,
                    Items = items ?? new List<BackupEntry>(),
                };
            }
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, BackupEntry? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        switch (value)
        {
            case BackupFileEntry fileEntry:
                writer.WritePropertyName("path");
                writer.WriteValue(fileEntry.FilePath);

                writer.WritePropertyName("cronExpression");
                writer.WriteValue(fileEntry.CronExpression);

                writer.WritePropertyName("compressionLevel");
                writer.WriteValue(fileEntry.CompressionLevel);

                writer.WritePropertyName("timeZone");
                writer.WriteValue(fileEntry.TimeZone);
                break;

            case BackupDirectoryEntry dirEntry:
                writer.WritePropertyName("name");
                writer.WriteValue(dirEntry.DirectoryName);

                writer.WritePropertyName("items");
                serializer.Serialize(writer, dirEntry.Items);
                break;

            default:
                throw new JsonSerializationException(
                    $"Unsupported backup entry type for serialization: {value.GetType().FullName}"
                );
        }

        writer.WriteEndObject();
    }
}

public class SinkDestinationConverter : JsonConverter<ISinkDestination>
{
    public override ISinkDestination? ReadJson(
        JsonReader reader,
        Type objectType,
        ISinkDestination? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var obj = JObject.Load(reader);
        var name = obj["kind"]?.ToString();

        return name switch
        {
            FileSystemSinkDestination.SinkKind => obj.ToObject<FileSystemSinkDestination>(),
            AzureBlobStorageSinkDestination.SinkKind =>
                obj.ToObject<AzureBlobStorageSinkDestination>(),
            _ => throw new JsonSerializationException($"Unknown sink type: {name}"),
        };
    }

    public override void WriteJson(
        JsonWriter writer,
        ISinkDestination? value,
        JsonSerializer serializer
    )
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        switch (value)
        {
            case FileSystemSinkDestination fileSystemDestination:
                writer.WritePropertyName("kind");
                writer.WriteValue(FileSystemSinkDestination.SinkKind);

                writer.WritePropertyName("destinationDirectory");
                writer.WriteValue(fileSystemDestination.DestinationDirectory);
                break;

            case AzureBlobStorageSinkDestination azureDestination:
                writer.WritePropertyName("kind");
                writer.WriteValue(AzureBlobStorageSinkDestination.SinkKind);

                writer.WritePropertyName("accountName");
                writer.WriteValue(azureDestination.AccountName);

                writer.WritePropertyName("container");
                writer.WriteValue(azureDestination.Container);
                break;

            default:
                throw new JsonSerializationException(
                    $"Unsupported sink destination type for serialization: {value.GetType().FullName}"
                );
        }

        writer.WriteEndObject();
    }
}