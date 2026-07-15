using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Request to execute a terminal action on a functional block.
/// </summary>
public sealed class BlockActionRequest
{
    [JsonPropertyName("actionId")]
    public string ActionId { get; set; }
}
