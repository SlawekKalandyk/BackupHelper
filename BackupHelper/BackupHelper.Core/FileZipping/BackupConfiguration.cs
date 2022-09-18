using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackupHelper.Core.FileZipping
{
    public class BackupConfiguration : IZipConfiguration
    {
        public List<IZipConfigurationNode> Nodes { get; } = new();

        public static IZipConfiguration FromJson(string json)
        {
            var backupConfiguration = new BackupConfiguration();
            var backupConfigurationJObject = JObject.Parse(json);
            var nodesJObject = backupConfigurationJObject.SelectToken(nameof(Nodes)) as JArray;

            foreach (JObject jObject in nodesJObject)
            {
                backupConfiguration.Nodes.Add(DeserializeZipConfigurationNode(jObject));
            }

            return backupConfiguration;
        }

        public static IZipConfiguration FromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void ToJsonFile(string filePath)
        {
            var json = ToJson();
            File.WriteAllText(filePath, json);
        }

        private static IZipConfigurationNode DeserializeZipConfigurationNode(JObject jObject)
        {
            bool isBackupZipDirectory = false;
            foreach (var pair in jObject)
            {
                isBackupZipDirectory = pair.Key == nameof(BackupZipDirectory.Nodes);
                if (isBackupZipDirectory)
                    break;
            }

            if (isBackupZipDirectory)
            {
                return DeserializeBackupZipDirectory(jObject);
            }

            return jObject.ToObject<BackupZipFile>();
        }

        private static BackupZipDirectory DeserializeBackupZipDirectory(JObject jObject)
        {
            var nameToken = jObject.SelectToken(nameof(BackupZipDirectory.Name));
            var nodesToken = jObject.SelectToken(nameof(BackupZipDirectory.Nodes)) as JArray;
            var backupZipDirectory = new BackupZipDirectory(nameToken.Value<string>());
            foreach (JObject token in nodesToken)
            {
                backupZipDirectory.Nodes.Add(DeserializeZipConfigurationNode(token));
            }

            return backupZipDirectory;
        }
    }

    public record BackupZipDirectory(string Name) : IZipConfigurationNode
    {
        public List<IZipConfigurationNode> Nodes { get; } = new();
    }

    public record BackupZipFile(string FilePath) : IZipConfigurationNode;
}
