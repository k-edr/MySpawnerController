using System;
using System.Collections.Generic;
using GridSpawner.Shared;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace GridSpawner.Plugin
{
    /// <summary>
    /// Maps <see cref="MyCubeGrid"/> entities to <see cref="GridDto"/> transport objects.
    /// </summary>
    internal static class GridDtoMapper
    {
        public static GridDto ToDto(MyCubeGrid grid)
        {
            var dto = new GridDto { Id = grid.EntityId, Name = grid.DisplayName };

            try
            {
                var p = grid.PositionComp.GetPosition();
                dto.Position = new Vector3Dto { X = p.X, Y = p.Y, Z = p.Z };
            }
            catch { }

            try
            {
                if (grid.Physics != null)
                {
                    var v = grid.Physics.LinearVelocity;
                    dto.Velocity = new Vector3Dto { X = v.X, Y = v.Y, Z = v.Z };
                }
            }
            catch { }

            MapBlocks(grid, dto);
            return dto;
        }

        private static void MapBlocks(IMyCubeGrid grid, GridDto dto)
        {
            try
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                foreach (var slim in blocks)
                {
                    if (slim == null) continue;
                    dto.Blocks.Add(MapBlock(slim));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"GridDtoMapper blocks: {ex.Message}");
            }
        }

        private static BlockDto MapBlock(IMySlimBlock slim)
        {
            var b = new BlockDto
            {
                GridPosition = new Vector3IDto
                {
                    X = slim.Position.X,
                    Y = slim.Position.Y,
                    Z = slim.Position.Z
                }
            };

            if (slim.FatBlock != null)
            {
                var fat = slim.FatBlock;
                b.Name = fat.DisplayNameText ?? fat.DefinitionDisplayNameText;
                try { b.Type = fat.BlockDefinition.ToString(); }
                catch { b.Type = fat.GetType().Name; }
            }
            else
            {
                var def = slim.BlockDefinition;
                b.Name = def?.DisplayNameText ?? "Armor";
                if (def != null)
                {
                    var id = def.Id;
                    b.Type = !string.IsNullOrEmpty(id.SubtypeName)
                        ? id.SubtypeName : "CubeBlock";
                }
                else b.Type = "CubeBlock";
            }

            return b;
        }
    }
}
