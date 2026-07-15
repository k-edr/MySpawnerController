using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Represents a terminal action available on a functional block.
/// </summary>
public sealed class BlockActionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }
}
