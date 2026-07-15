using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

public sealed class GridListItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("position")]
    public Vector3Dto Position { get; set; }
}
