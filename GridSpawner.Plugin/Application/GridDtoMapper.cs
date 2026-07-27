using System;
using System.Collections.Generic;
using GridSpawner.Plugin.Infrastructure;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace GridSpawner.Plugin.Application
{
    /// <summary>
    /// Maps <see cref="MyCubeGrid"/> entities to <see cref="GridDto"/> transport objects.
    /// Uses <see cref="IBlockInfoExtractor"/> strategy for fat vs slim blocks.
    /// </summary>
    internal static class GridDtoMapper
    {
        private static readonly IBlockInfoExtractor FatExtractor = new FatBlockInfoExtractor();
        private static readonly IBlockInfoExtractor SlimExtractor = new SlimBlockInfoExtractor();

        public static GridDto ToDto(MyCubeGrid grid)
        {
            var dto = new GridDto { Id = grid.EntityId, Name = grid.DisplayName };

            try
            {
                var p = grid.PositionComp.GetPosition();
                dto.Position = new Vector3Dto { X = p.X, Y = p.Y, Z = p.Z };
            }
            catch (Exception ex) { Logger.Warn($"GridDtoMapper position: {ex.Message}"); }

            try
            {
                if (grid.Physics != null)
                {
                    var v = grid.Physics.LinearVelocity;
                    dto.Velocity = new Vector3Dto { X = v.X, Y = v.Y, Z = v.Z };
                }
            }
            catch (Exception ex) { Logger.Warn($"GridDtoMapper velocity: {ex.Message}"); }

            try
            {
                var forward = grid.WorldMatrix.Forward;
                dto.Forward = new Vector3Dto { X = forward.X, Y = forward.Y, Z = forward.Z };
            }
            catch (Exception ex) { Logger.Warn($"GridDtoMapper forward: {ex.Message}"); }

            try
            {
                var up = grid.WorldMatrix.Up;
                dto.Up = new Vector3Dto { X = up.X, Y = up.Y, Z = up.Z };
            }
            catch (Exception ex) { Logger.Warn($"GridDtoMapper up: {ex.Message}"); }

            MapBlocks(grid, dto);
            return dto;
        }

        private static void MapBlocks(IMyCubeGrid grid, GridDto dto)
        {
            try
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, null);
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
            var extractor = slim.FatBlock != null ? FatExtractor : SlimExtractor;

            return new BlockDto
            {
                GridPosition = new Vector3IDto
                {
                    X = slim.Position.X,
                    Y = slim.Position.Y,
                    Z = slim.Position.Z
                },
                Name = extractor.GetName(slim),
                Type = extractor.GetTypeName(slim)
            };
        }
    }
}
