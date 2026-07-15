using VRage.Game.ModAPI;

namespace GridSpawner.Plugin
{
    /// <summary>
    /// Strategy for extracting name and type from <see cref="IMySlimBlock"/>.
    /// </summary>
    internal interface IBlockInfoExtractor
    {
        /// <summary>Human-readable block name. Never null.</summary>
        string GetName(IMySlimBlock slim);

        /// <summary>Block type identifier. Never null.</summary>
        string GetTypeName(IMySlimBlock slim);
    }
}
