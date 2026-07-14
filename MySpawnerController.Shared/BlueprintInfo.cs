using System.Text.Json.Serialization;

namespace MySpawnerController.Shared;

public sealed class BlueprintInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; }
}
