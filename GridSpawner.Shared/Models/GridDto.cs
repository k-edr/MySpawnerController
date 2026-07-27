using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

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

    [JsonPropertyName("forward")]
    public Vector3Dto Forward { get; set; }

    [JsonPropertyName("backward")]
    public Vector3Dto Backward { get; set; }

    [JsonPropertyName("up")]
    public Vector3Dto Up { get; set; }

    [JsonPropertyName("down")]
    public Vector3Dto Down { get; set; }

    [JsonPropertyName("left")]
    public Vector3Dto Left { get; set; }

    [JsonPropertyName("right")]
    public Vector3Dto Right { get; set; }

    [JsonPropertyName("blocks")]
    public List<BlockDto> Blocks { get; set; } = new();
}
