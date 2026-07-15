using System;
using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.World;

namespace GridSpawner.Plugin
{
    /// <summary>
    /// Applies post-spawn fixup to a grid via reflection.
    /// Calls internal methods required for the grid to work properly:
    /// OnAddedToScene, ActivatePhysics, RegisterCubeGrid.
    /// </summary>
    internal static class PostSpawnFixup
    {
        private static readonly BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static void Apply(MyCubeGrid grid)
        {
            if (grid == null) return;
            var gridType = typeof(MyCubeGrid);

            InvokeMethod(gridType, "OnAddedToScene", grid, new object[] { grid });
            InvokeMethod(gridType, "ActivatePhysics", grid, null);

            var regMethod = MySession.Static?.GetType()?.GetMethod("RegisterCubeGrid", Flags);
            try
            {
                regMethod?.Invoke(MySession.Static, new object[] { grid });
                Logger.Info("  RegisterCubeGrid OK");
            }
            catch (Exception ex) { Logger.Warn($"  RegisterCubeGrid: {ex.Message}"); }
        }

        private static void InvokeMethod(Type type, string name, object instance, object[] args)
        {
            try
            {
                type.GetMethod(name, Flags)?.Invoke(instance, args);
                Logger.Info($"  {name} OK");
            }
            catch (Exception ex) { Logger.Warn($"  {name}: {ex.Message}"); }
        }
    }
}
