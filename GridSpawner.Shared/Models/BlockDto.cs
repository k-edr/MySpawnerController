using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

public sealed class BlockDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("gridPosition")]
    public Vector3IDto GridPosition { get; set; }
}
