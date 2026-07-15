using System.Text.Json.Serialization;

namespace GridSpawner.Shared;

public sealed class HealthResponse
{
    [JsonPropertyName("ready")]
    public bool Ready { get; set; }
}
