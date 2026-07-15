using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

public sealed class BlueprintInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; }
}
