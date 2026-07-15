using System;
using System.Collections.Generic;
using System.IO;
using VRage.Game;
using VRage.ObjectBuilders;

namespace GridSpawner.Plugin
{
    /// <summary>
    /// Deserializes .sbc blueprint files into <see cref="MyObjectBuilder_CubeGrid"/> builders.
    /// </summary>
    internal static class BlueprintDeserializer
    {
        /// <summary>
        /// Reads and deserializes a blueprint .sbc file.
        /// Returns the list of cube grid object builders, or null on failure.
        /// </summary>
        public static List<MyObjectBuilder_CubeGrid> Deserialize(string bpFile)
        {
            MyObjectBuilder_Definitions definitions;
            using (var stream = File.OpenRead(bpFile))
            {
                VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                    stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                definitions = obj as MyObjectBuilder_Definitions;
            }

            if (definitions?.ShipBlueprints == null || definitions.ShipBlueprints.Length == 0)
                return null;

            var shipBp = definitions.ShipBlueprints[0];
            if (shipBp.CubeGrids == null || shipBp.CubeGrids.Length == 0)
                return null;

            var gridBuilders = new List<MyObjectBuilder_CubeGrid>();
            foreach (var g in shipBp.CubeGrids)
            {
                if (g is MyObjectBuilder_CubeGrid cg)
                    gridBuilders.Add(cg);
            }

            return gridBuilders.Count == 0 ? null : gridBuilders;
        }
    }
}
