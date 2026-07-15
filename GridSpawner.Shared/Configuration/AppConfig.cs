using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Configuration;

/// <summary>
/// Application configuration stored via <see cref="AppDefaults.DefaultConfigFile"/>.
/// </summary>
public sealed class AppConfig
{
    [JsonPropertyName("apiScheme")]
    public string ApiScheme { get; set; } = AppDefaults.DefaultApiScheme;

    /// <summary>HTTP host for listening. Default <c>+</c> (all interfaces).</summary>
    [JsonPropertyName("apiHost")]
    public string ApiHost { get; set; } = AppDefaults.DefaultHost;

    /// <summary>Host for display URLs. <c>+</c>/<c>*</c> → <c>localhost</c>.</summary>
    [JsonIgnore]
    public string DisplayHost => ApiHost == "+" || ApiHost == "*" ? "localhost" : ApiHost;

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
