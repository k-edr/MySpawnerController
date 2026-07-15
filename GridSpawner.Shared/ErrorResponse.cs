using System.Text.Json.Serialization;

namespace GridSpawner.Shared;

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Hint { get; set; }
}
