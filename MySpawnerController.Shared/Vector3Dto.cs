using System.Text.Json.Serialization;

namespace MySpawnerController.Shared;

public sealed class Vector3Dto
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
}
