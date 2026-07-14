using System.Text.Json.Serialization;

namespace MySpawnerController.Shared;

public sealed class GridListItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("position")]
    public Vector3Dto Position { get; set; }
}
