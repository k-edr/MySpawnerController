using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Configuration;

/// <summary>
/// Application configuration stored via <see cref="AppDefaults.DefaultConfigFile"/>.
/// </summary>
public sealed class AppConfig
{
    /// <summary>HTTP host for the game API. Default <see cref="AppDefaults.DefaultHost"/>.</summary>
    [JsonPropertyName("apiScheme")]
    public string ApiScheme { get; set; } = AppDefaults.DefaultApiScheme;

    [JsonPropertyName("apiHost")]
    public string ApiHost { get; set; } = AppDefaults.DefaultHost;

    [JsonPropertyName("apiPort")]
    public int ApiPort { get; set; } = AppDefaults.DefaultApiPort;

    [JsonPropertyName("swaggerPort")]
    public int SwaggerPort { get; set; } = AppDefaults.DefaultSwaggerPort;

    [JsonPropertyName("swaggerCorsOrigin")]
    public string SwaggerCorsOrigin { get; set; } =
        $"{AppDefaults.DefaultApiScheme}://localhost:{AppDefaults.DefaultSwaggerPort}";

    /// <summary>Override blueprints folder. Empty = default %APPDATA%\SpaceEngineers\Blueprints\local.</summary>
    [JsonPropertyName("blueprintsFolder")]
    public string BlueprintsFolder { get; set; } = AppDefaults.DefaultBlueprintsFolder;

    [JsonPropertyName("maxBlueprintFileSizeBytes")]
    public long MaxBlueprintFileSizeBytes { get; set; } = AppDefaults.DefaultMaxBlueprintFileSizeBytes;

    [JsonPropertyName("maxGridsPerBlueprint")]
    public int MaxGridsPerBlueprint { get; set; } = AppDefaults.DefaultMaxGridsPerBlueprint;
}
