using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GridSpawner.Shared.Models;

/// <summary>
/// Extended block DTO for functional (FatBlock) blocks with actions and properties.
/// </summary>
public sealed class TerminalBlockDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("gridPosition")]
    public Vector3IDto GridPosition { get; set; }

    [JsonPropertyName("entityId")]
    public long EntityId { get; set; }

    [JsonPropertyName("isFunctional")]
    public bool IsFunctional { get; set; }

    [JsonPropertyName("isWorking")]
    public bool IsWorking { get; set; }

    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    /// <summary>Actions available on this block (populated on demand).</summary>
    [JsonPropertyName("actions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<BlockActionDto> Actions { get; set; }

    /// <summary>Properties of this block (populated on demand).</summary>
    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<BlockPropertyDto> Properties { get; set; }
}
