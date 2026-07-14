using System.Text.Json.Serialization;

namespace MySpawnerController.Shared;

public sealed class HealthResponse
{
    [JsonPropertyName("ready")]
    public bool Ready { get; set; }
}
