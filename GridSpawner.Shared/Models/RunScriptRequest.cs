using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Request to run a programmable block with an optional argument.
/// </summary>
public sealed class RunScriptRequest
{
    [JsonPropertyName("argument")]
    public string Argument { get; set; }
}
