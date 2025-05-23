using Newtonsoft.Json;

namespace BackupHelper.Core.FileZipping
{
    [JsonConverter(typeof(BackupEntryConverter))]
    public abstract class BackupEntry { }

    public class BackupFileEntry : BackupEntry
    {
        public string FilePath { get; set; } = string.Empty;
    }

    public class BackupDirectoryEntry : BackupEntry
    {
        public string DirectoryName { get; set; } = string.Empty;
        public List<BackupEntry> Items { get; set; } = new();
    }

    public class BackupEntryConverter : JsonConverter<BackupEntry>
    {
        public override BackupEntry? ReadJson(JsonReader reader, Type objectType, BackupEntry? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return new BackupFileEntry { FilePath = (string)reader.Value! };
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = serializer.Deserialize<Dictionary<string, object>>(reader);

                if (obj != null && obj.Count == 1)
                {
                    var dirName = obj.Keys.First();
                    var itemsToken = obj[dirName];
                    var items = itemsToken is Newtonsoft.Json.Linq.JArray arr
                                    ? arr.ToObject<List<BackupEntry>>(serializer)
                                    : new List<BackupEntry>();

                    return new BackupDirectoryEntry
                    {
                        DirectoryName = dirName,
                        Items = items
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
                writer.WriteValue(fileEntry.FilePath);
            }
            else if (value is BackupDirectoryEntry dirEntry)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(dirEntry.DirectoryName);
                serializer.Serialize(writer, dirEntry.Items);
                writer.WriteEndObject();
            }
        }
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
}