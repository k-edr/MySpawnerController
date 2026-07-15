using System;
using VRage.Game.ModAPI;

namespace GridSpawner.Plugin.Application
{
    /// <summary>
    /// Extracts info from blocks that have a <see cref="IMySlimBlock.FatBlock"/> (functional blocks).
    /// </summary>
    internal sealed class FatBlockInfoExtractor : IBlockInfoExtractor
    {
        public string GetName(IMySlimBlock slim)
        {
            var fat = slim.FatBlock;
            return fat.DisplayNameText
                ?? fat.DefinitionDisplayNameText
                ?? fat.GetType().Name;
        }

        public string GetTypeName(IMySlimBlock slim)
        {
            try
            {
                return slim.FatBlock.BlockDefinition.ToString();
            }
            catch (Exception ex)
            {
                Infrastructure.Logger.Warn($"FatBlockInfoExtractor.GetTypeName: {ex.Message}");
                return slim.FatBlock?.GetType().Name ?? "Unknown";
            }
        }
    }
}
