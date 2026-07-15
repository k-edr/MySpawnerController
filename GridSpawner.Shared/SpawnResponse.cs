using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GridSpawner.Shared;

public sealed class SpawnResponse
{
    [JsonPropertyName("grids")]
    public List<GridDto> Grids { get; set; } = new();
}
