using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

public sealed class SpawnResponse
{
    [JsonPropertyName("grids")]
    public IReadOnlyList<GridDto> Grids { get; set; } = new List<GridDto>();
}
