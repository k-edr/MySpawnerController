using System;
using System.Collections.Generic;
using System.IO;
using GridSpawner.Shared.Configuration;
using VRage.Game;
using VRage.ObjectBuilders;

namespace GridSpawner.Plugin.Application
{
    internal static class BlueprintDeserializer
    {
        private static readonly object _serializeLock = new();

        public static List<MyObjectBuilder_CubeGrid> Deserialize(string bpFile,
            AppConfig config, out string error)
        {
            error = null;

            if (!ValidateFile(bpFile, config, out error))
                return null;

            MyObjectBuilder_Definitions definitions;
            try
            {
                definitions = DeserializeXml(bpFile);
            }
            catch (Exception ex)
            {
                error = $"Failed to parse blueprint XML: {ex.Message}";
                return null;
            }

            if (definitions == null)
            {
                error = "Failed to deserialize blueprint XML";
                return null;
            }

            var grids = ExtractGridBuilders(definitions, config, out error);
            return grids;
        }

        private static bool ValidateFile(string bpFile, AppConfig config, out string error)
        {
            error = null;
            var fileInfo = new FileInfo(bpFile);

            if (!fileInfo.Exists)
            {
                error = $"Blueprint file not found: {bpFile}";
                return false;
            }

            if (fileInfo.Length > config.MaxBlueprintFileSizeBytes)
            {
                error = $"Blueprint file too large: {fileInfo.Length:N0} bytes " +
                        $"(max {config.MaxBlueprintFileSizeBytes:N0})";
                return false;
            }

            return true;
        }

        private static MyObjectBuilder_Definitions DeserializeXml(string bpFile)
        {
            lock (_serializeLock)
            {
                using var stream = File.OpenRead(bpFile);
                VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                    stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                return obj as MyObjectBuilder_Definitions;
            }
        }

        private static List<MyObjectBuilder_CubeGrid> ExtractGridBuilders(
            MyObjectBuilder_Definitions definitions, AppConfig config, out string error)
        {
            error = null;

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

            if (shipBp.CubeGrids.Length > config.MaxGridsPerBlueprint)
            {
                error = $"Too many grids in blueprint: {shipBp.CubeGrids.Length} " +
                        $"(max {config.MaxGridsPerBlueprint})";
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
