using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Result of running a programmable block script.
/// </summary>
public sealed class ScriptRunResult
{
    [JsonPropertyName("echo")]
    public string Echo { get; set; } = "";

    [JsonPropertyName("output")]
    public string Output { get; set; } = "";

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
