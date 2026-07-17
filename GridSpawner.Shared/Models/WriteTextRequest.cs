using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Request to write text to an LCD / text panel.
/// </summary>
public sealed class WriteTextRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
