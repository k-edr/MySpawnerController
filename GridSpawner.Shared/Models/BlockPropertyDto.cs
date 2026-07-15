using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Represents a terminal property of a functional block.
/// </summary>
public sealed class BlockPropertyDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("propertyType")]
    public string PropertyType { get; set; }
}
