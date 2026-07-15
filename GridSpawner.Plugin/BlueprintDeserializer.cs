using System;
using System.Collections.Generic;
using System.IO;
using GridSpawner.Shared;
using VRage.Game;
using VRage.ObjectBuilders;

namespace GridSpawner.Plugin
{
    /// <summary>
    /// Deserializes .sbc blueprint files into <see cref="MyObjectBuilder_CubeGrid"/> builders.
    /// Enforces file size and grid count limits from <see cref="AppConfig"/>.
    /// </summary>
    internal static class BlueprintDeserializer
    {
        /// <summary>
        /// Reads and deserializes a blueprint .sbc file.
        /// Returns the list of cube grid object builders, or null on failure.
        /// </summary>
        /// <param name="bpFile">Path to the .sbc file.</param>
        /// <param name="config">App config with size/count limits.</param>
        /// <param name="error">Out: human-readable error message on failure.</param>
        public static List<MyObjectBuilder_CubeGrid> Deserialize(string bpFile, AppConfig config, out string error)
        {
            error = null;

            // ── File size check ──
            var fileInfo = new FileInfo(bpFile);
            if (!fileInfo.Exists)
            {
                error = $"Blueprint file not found: {bpFile}";
                return null;
            }

            if (fileInfo.Length > config.MaxBlueprintFileSizeBytes)
            {
                error = $"Blueprint file too large: {fileInfo.Length:N0} bytes (max {config.MaxBlueprintFileSizeBytes:N0})";
                return null;
            }

            // ── Deserialize ──
            MyObjectBuilder_Definitions definitions;
            using (var stream = File.OpenRead(bpFile))
            {
                VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                    stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                definitions = obj as MyObjectBuilder_Definitions;
            }

            if (definitions?.ShipBlueprints == null || definitions.ShipBlueprints.Length == 0)
            {
                error = "No ShipBlueprints in file";
                return null;
            }

            var shipBp = definitions.ShipBlueprints[0];
            if (shipBp.CubeGrids == null || shipBp.CubeGrids.Length == 0)
            {
                error = "No CubeGrids in blueprint";
                return null;
            }

            // ── Grid count check ──
            if (shipBp.CubeGrids.Length > config.MaxGridsPerBlueprint)
            {
                error = $"Too many grids in blueprint: {shipBp.CubeGrids.Length} (max {config.MaxGridsPerBlueprint})";
                return null;
            }

            var gridBuilders = new List<MyObjectBuilder_CubeGrid>(shipBp.CubeGrids.Length);
            foreach (var g in shipBp.CubeGrids)
            {
                if (g is MyObjectBuilder_CubeGrid cg)
                    gridBuilders.Add(cg);
            }

            if (gridBuilders.Count == 0)
            {
                error = "No valid CubeGrid builders extracted";
                return null;
            }

            return gridBuilders;
        }
    }
}
