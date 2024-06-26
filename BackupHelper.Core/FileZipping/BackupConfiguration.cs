﻿using Newtonsoft.Json;

namespace BackupHelper.Core.FileZipping
{
    public class BackupConfiguration
    {
        public ICollection<BackupDirectory> Directories { get; set; } = new List<BackupDirectory>();
        public ICollection<BackupFile> Files { get; set; } = new List<BackupFile>();
        public string? LogFilePath { get; set; }

        public static BackupConfiguration FromJson(string json)
        {
            return JsonConvert.DeserializeObject<BackupConfiguration>(json);
        }

        public static BackupConfiguration FromJsonFile(string filePath)
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
    }

    public record BackupDirectory
    {
        // Required for deserialization
        [JsonConstructor]
        public BackupDirectory()
        {
        }

        public BackupDirectory(string name)
        {
            Name = name;
        }

        public BackupDirectory(string filePath, string? name = null)
        {
            FilePath = filePath;
            Name = name;
        }

        public string? Name { get; set; }
        public string? FilePath { get; set; }
        public ICollection<BackupDirectory> Directories { get; set; } = new List<BackupDirectory>();
        public ICollection<BackupFile> Files { get; set; } = new List<BackupFile>();
    }

    public record BackupFile(string FilePath);
}
