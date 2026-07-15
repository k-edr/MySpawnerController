using System;
using VRage.Game.ModAPI;

namespace GridSpawner.Plugin.Application
{
    /// <summary>
    /// Extracts info from armor / structural blocks (no <see cref="IMySlimBlock.FatBlock"/>).
    /// </summary>
    internal sealed class SlimBlockInfoExtractor : IBlockInfoExtractor
    {
        public string GetName(IMySlimBlock slim)
        {
            return slim.BlockDefinition?.DisplayNameText ?? "Armor";
        }

        public string GetTypeName(IMySlimBlock slim)
        {
            var def = slim.BlockDefinition;
            if (def == null)
                return "CubeBlock";

            var id = def.Id;
            return !string.IsNullOrEmpty(id.SubtypeName)
                ? id.SubtypeName
                : "CubeBlock";
        }
    }
}
