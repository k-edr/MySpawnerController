using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MySpawnerController.Shared;

public sealed class GridDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("position")]
    public Vector3Dto Position { get; set; }

    [JsonPropertyName("velocity")]
    public Vector3Dto Velocity { get; set; }

    [JsonPropertyName("blocks")]
    public List<BlockDto> Blocks { get; set; } = new();
}
