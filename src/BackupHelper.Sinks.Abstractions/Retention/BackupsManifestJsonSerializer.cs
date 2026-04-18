using System.Text.Json;

namespace BackupHelper.Sinks.Abstractions.Retention;

public static class BackupsManifestJsonSerializer
{
    private static readonly JsonSerializerOptions ManifestJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static BackupsManifest DeserializeOrDefault(string? manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            return new BackupsManifest();
        }

        try
        {
            return JsonSerializer.Deserialize<BackupsManifest>(
                    manifestJson,
                    ManifestJsonSerializerOptions
                ) ?? new BackupsManifest();
        }
        catch (JsonException)
        {
            return new BackupsManifest();
        }
    }

    public static string Serialize(BackupsManifest manifest)
    {
        return JsonSerializer.Serialize(manifest, ManifestJsonSerializerOptions);
    }
}
