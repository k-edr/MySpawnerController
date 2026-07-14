using System.Text.Json.Serialization;

namespace MySpawnerController.Shared;

public sealed class Vector3IDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("z")]
    public int Z { get; set; }
}
