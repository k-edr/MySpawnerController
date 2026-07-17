using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Request to set a terminal block property value.
/// </summary>
public sealed class SetPropertyRequest
{
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
