using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Request to upload code to a programmable block.
/// Used by both pb-test convenience endpoints and block-level endpoints.
/// </summary>
public sealed class UploadCodeRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; }
}
