using System.Text.Json.Serialization;

namespace GridSpawner.Shared;

public sealed class SpawnRequest
{
    [JsonPropertyName("blueprint")]
    public string Blueprint { get; set; }

    [JsonPropertyName("position")]
    public SpawnPosition Position { get; set; } = new();

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }
}
