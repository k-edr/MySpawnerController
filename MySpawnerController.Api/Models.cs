using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VRageMath;

namespace MySpawnerController.Api
{
    public sealed class SpawnRequest
    {
        [JsonPropertyName("blueprint")]
        public string Blueprint { get; set; }

        [JsonPropertyName("position")]
        public SpawnPosition Position { get; set; } = new SpawnPosition();

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
    }

    public sealed class SpawnPosition
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }
    }

    public sealed class SpawnResponse
    {
        [JsonPropertyName("grids")]
        public List<GridDto> Grids { get; set; } = new List<GridDto>();
    }

    public sealed class GridDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("position")]
        public Vector3Dto Position { get; set; }

        [JsonPropertyName("velocity")]
        public Vector3Dto Velocity { get; set; }

        [JsonPropertyName("blocks")]
        public List<BlockDto> Blocks { get; set; } = new List<BlockDto>();
    }

    public sealed class BlockDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("gridPosition")]
        public Vector3IDto GridPosition { get; set; }
    }

    public sealed class Vector3Dto
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }
    }

    public sealed class Vector3IDto
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("z")]
        public int Z { get; set; }
    }

    public sealed class BlueprintInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("available")]
        public bool Available { get; set; }
    }

    public sealed class GridListItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("position")]
        public Vector3Dto Position { get; set; }
    }

    public sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("hint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Hint { get; set; }
    }

    public sealed class HealthResponse
    {
        [JsonPropertyName("ready")]
        public bool Ready { get; set; }
    }

    public interface ISpawnService
    {
        List<GridDto> Spawn(string blueprintName, string blueprintPath,
            Vector3D position, string displayName);

        List<BlueprintInfo> ListBlueprints(string blueprintsFolder);

        List<GridListItem> ListGrids();

        GridDto GetGrid(long id);

        bool DeleteGrid(long id);

        bool IsReady { get; }
    }
}
