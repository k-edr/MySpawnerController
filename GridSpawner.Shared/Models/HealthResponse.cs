using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

public sealed class HealthResponse
{
    [JsonPropertyName("ready")]
    public bool Ready { get; set; }
}
