using System.Text.Json.Serialization;

namespace GridSpawner.Shared;

/// <summary>
/// Application configuration stored in %APPDATA%\SpaceEngineers\GridSpawner.json
/// </summary>
public sealed class AppConfig
{
    [JsonPropertyName("apiPort")]
    public int ApiPort { get; set; } = 9997;

    [JsonPropertyName("swaggerPort")]
    public int SwaggerPort { get; set; } = 9998;

    [JsonPropertyName("swaggerCorsOrigin")]
    public string SwaggerCorsOrigin { get; set; } = "http://localhost:9998";

    /// <summary>Override blueprints folder. null = default %APPDATA%\SpaceEngineers\Blueprints\local</summary>
    [JsonPropertyName("blueprintsFolder")]
    public string BlueprintsFolder { get; set; }

    /// <summary>API key for protected endpoints. null = no auth.</summary>
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; }
}
