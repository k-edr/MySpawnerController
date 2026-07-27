using System;
using System.Collections.Generic;
using GridSpawner.Plugin.Infrastructure;
using GridSpawner.Shared.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRageMath;

namespace GridSpawner.Plugin.Application;

/// <summary>
/// Provides interaction with functional (FatBlock) terminal blocks on grids.
/// Must be called from the game main thread.
/// </summary>
internal static class TerminalBlockService
{
    public static List<TerminalBlockDto> GetGridBlocks(MyCubeGrid grid)
    {
        var result = new List<TerminalBlockDto>();

        try
        {
            var blocks = new List<IMySlimBlock>();
            ((IMyCubeGrid)grid).GetBlocks(blocks, null);

            foreach (var slim in blocks)
            {
                if (slim?.FatBlock == null)
                    continue;

                result.Add(MapToDto(slim, slim.FatBlock, includeDetails: false));
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"TerminalBlockService.GetGridBlocks: {ex.Message}");
        }

        return result;
    }

    public static TerminalBlockDto GetBlockDetail(MyCubeGrid grid, Vector3I position)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            if (slim?.FatBlock == null)
                return null;

            return MapToDto(slim, slim.FatBlock, includeDetails: true);
        }
        catch (Exception ex)
        {
            Logger.Error($"TerminalBlockService.GetBlockDetail: {ex.Message}");
            return null;
        }
    }

    public static bool ExecuteAction(MyCubeGrid grid, Vector3I position, string actionId)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            if (slim?.FatBlock == null)
                return false;

            var terminal = slim.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            if (terminal == null)
                return false;

            var action = terminal.GetActionWithName(actionId);
            if (action == null || !action.IsEnabled(terminal))
                return false;

            action.Apply(terminal);
            Logger.Info($"Action '{actionId}' executed on block at {position}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"TerminalBlockService.ExecuteAction: {ex.Message}");
            return false;
        }
    }

    public static string GetProperty(MyCubeGrid grid, Vector3I position, string propertyId)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            if (slim?.FatBlock == null)
                return null;

            var terminal = slim.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            if (terminal == null)
                return null;

            // Direct IMyTerminalBlock properties (not exposed via terminal system)
            if (propertyId == "CustomData")
                return terminal.CustomData ?? "";
            if (propertyId == "CustomName")
                return terminal.CustomName ?? "";

            var prop = terminal.GetProperty(propertyId);
            if (prop == null)
                return null;

            // Use TerminalPropertyExtensions.As<T> to get typed value
            // Fall back to generic As<string> or use GetValue
            try
            {
                return prop.As<string>().GetValue(terminal);
            }
            catch
            {
                try
                {
                    // Try common types
                    var typeName = prop.TypeName;
                    switch (typeName)
                    {
                        case "Boolean":
                            return prop.AsBool().GetValue(terminal).ToString();
                        case "Single":
                            return prop.AsFloat().GetValue(terminal)
                                .ToString(System.Globalization.CultureInfo.InvariantCulture);
                        case "Color":
                            var c = prop.AsColor().GetValue(terminal);
                            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                        case "StringBuilder":
                            return prop.As<System.Text.StringBuilder>().GetValue(terminal).ToString();
                        case "Int64":
                            return prop.As<long>().GetValue(terminal).ToString();
                        case "Int32":
                            return prop.As<int>().GetValue(terminal).ToString();
                    }
                }
                catch { }
            }

            return "?";
        }
        catch (Exception ex)
        {
            Logger.Error($"TerminalBlockService.GetProperty: {ex.Message}");
            return null;
        }
    }

    public static bool SetProperty(MyCubeGrid grid, Vector3I position,
        string propertyId, string value)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            if (slim?.FatBlock == null)
                return false;

            var terminal = slim.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            if (terminal == null)
                return false;

            // Direct IMyTerminalBlock properties (not exposed via terminal system)
            if (propertyId == "CustomData")
            {
                terminal.CustomData = value;
                Logger.Info($"CustomData set on block at {position}");
                return true;
            }
            if (propertyId == "CustomName")
            {
                terminal.CustomName = value;
                Logger.Info($"CustomName set to '{value}' on block at {position}");
                return true;
            }

            var prop = terminal.GetProperty(propertyId);
            if (prop == null)
                return false;

            var typeName = prop.TypeName;
            switch (typeName)
            {
                case "Boolean":
                    if (bool.TryParse(value, out bool bVal))
                    {
                        prop.AsBool().SetValue(terminal, bVal);
                        Logger.Info($"Property '{propertyId}' = {bVal} on block at {position}");
                        return true;
                    }
                    break;

                case "Single":
                    if (float.TryParse(value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float fVal))
                    {
                        prop.AsFloat().SetValue(terminal, fVal);
                        Logger.Info($"Property '{propertyId}' = {fVal} on block at {position}");
                        return true;
                    }
                    break;

                case "Color":
                    {
                        var color = ParseColor(value);
                        if (color.HasValue)
                        {
                            prop.AsColor().SetValue(terminal, color.Value);
                            Logger.Info($"Property '{propertyId}' = {value} on block at {position}");
                            return true;
                        }
                    }
                    break;

                case "Int64":
                    if (long.TryParse(value, out long lVal))
                    {
                        prop.As<long>().SetValue(terminal, lVal);
                        Logger.Info($"Property '{propertyId}' = {lVal} on block at {position}");
                        return true;
                    }
                    break;

                case "Int32":
                    if (int.TryParse(value, out int iVal))
                    {
                        prop.As<int>().SetValue(terminal, iVal);
                        Logger.Info($"Property '{propertyId}' = {iVal} on block at {position}");
                        return true;
                    }
                    break;

                case "String":
                    {
                        prop.As<string>().SetValue(terminal, value);
                        Logger.Info($"Property '{propertyId}' = '{value}' on block at {position}");
                        return true;
                    }

                case "StringBuilder":
                    {
                        var sb = new System.Text.StringBuilder(value);
                        prop.As<System.Text.StringBuilder>().SetValue(terminal, sb);
                        Logger.Info($"Property '{propertyId}' = '{value}' on block at {position}");
                        return true;
                    }

                default:
                    // Generic set using As<T> + SetValue
                    try
                    {
                        var strProp = prop.As<string>();
                        if (strProp != null)
                        {
                            strProp.SetValue(terminal, value);
                            Logger.Info($"Property '{propertyId}' = '{value}' (generic) on block at {position}");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Generic SetProperty error for '{propertyId}': {ex.Message}");
                    }
                    Logger.Warn($"SetProperty: unhandled type '{typeName}' for '{propertyId}'");
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"TerminalBlockService.SetProperty: {ex.Message}");
            return false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    // ── Direct block-type access (not terminal actions/properties) ──

    public static bool SetProgramCode(MyCubeGrid grid, Vector3I position, string code)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            var fat = slim?.FatBlock;
            if (fat == null) return false;

            // Preferred: official ModAPI interface
            if (fat is Sandbox.ModAPI.IMyProgrammableBlock modApiPb)
            {
                modApiPb.ProgramData = code;
                Logger.Info($"PB code set via ModAPI ({code.Length} chars)");
                return true;
            }

            // Fallback: reflection-based for non-standard PB implementations
            var type = fat.GetType();
            var flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic;

            var candidates = new[] {
                "SetProgram", "SendChangeProgram", "UploadProgram",
                "SetProgramCode", "SendProgram", "WriteProgram",
                "set_ProgramData", "set_Program"
            };

            foreach (var name in candidates)
            {
                var method = type.GetMethod(name, flags, null, new[] { typeof(string) }, null);
                if (method != null)
                {
                    method.Invoke(fat, new object[] { code });
                    Logger.Info($"PB code set via {name} ({code.Length} chars)");
                    return true;
                }
            }

            foreach (var prop in type.GetProperties(flags))
            {
                if (prop.PropertyType == typeof(string)
                    && prop.Name.IndexOf("Program", StringComparison.OrdinalIgnoreCase) >= 0
                    && prop.CanWrite)
                {
                    prop.SetValue(fat, code);
                    Logger.Info($"PB code set via property {prop.Name} ({code.Length} chars)");
                    return true;
                }
            }

            var allMethods = new System.Text.StringBuilder();
            foreach (var m in type.GetMethods(flags))
            {
                var pars = m.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                    allMethods.Append(m.Name).Append(',');
            }
            Logger.Error($"SetProgramCode: no setter found. Available string methods on {type.Name}: {allMethods}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"SetProgramCode: {ex}");
            return false;
        }
    }

    public static bool WriteTextPanel(MyCubeGrid grid, Vector3I position, string text)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            var fat = slim?.FatBlock;
            if (fat == null) return false;

            // Preferred: IMyTextPanel (dedicated LCD blocks)
            if (fat is Sandbox.ModAPI.Ingame.IMyTextPanel panel)
            {
                panel.WriteText(text);
                Logger.Info($"Text written to panel at {position} ({text.Length} chars)");
                return true;
            }

            // Fallback: IMyTextSurfaceProvider (cockpits, PBs, cryo, buttons, etc.)
            if (fat is Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider provider)
            {
                var surface = provider.GetSurface(0);
                surface?.WriteText(text);
                Logger.Info($"Text written to surface[0] at {position} ({text.Length} chars)");
                return true;
            }

            Logger.Warn($"WriteTextPanel: block at {position} has no text surface");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"WriteTextPanel: {ex.Message}");
            return false;
        }
    }

    public static bool RunProgram(MyCubeGrid grid, Vector3I position, string argument)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            if (slim?.FatBlock is Sandbox.ModAPI.Ingame.IMyProgrammableBlock pb)
            {
                bool ok = pb.TryRun(argument ?? "");
                Logger.Info($"PB Run(\"{argument}\") at {position}: {(ok ? "OK" : "FAIL")}");
                return ok;
            }
            Logger.Warn($"RunProgram: block at {position} is not a programmable block");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"RunProgram: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Read text from a ProgrammableBlock's built-in LCD surface.
    /// Uses IMyTextSurfaceProvider (which IMyProgrammableBlock inherits).
    /// </summary>
    public static string GetPbSurfaceText(MyCubeGrid grid, Vector3I position)
    {
        try
        {
            var slim = grid.GetCubeBlock(position);
            if (slim?.FatBlock is Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider provider)
            {
                var text = provider.GetSurface(0)?.GetText() ?? "";
                Logger.Info($"PB surface text read at {position} ({text.Length} chars)");
                return text;
            }
            Logger.Warn($"GetPbSurfaceText: block at {position} has no text surface");
            return "";
        }
        catch (Exception ex)
        {
            Logger.Error($"GetPbSurfaceText: {ex.Message}");
            return "";
        }
    }

    // ── DTO mapping ──

    private static TerminalBlockDto MapToDto(IMySlimBlock slim, IMyCubeBlock fat,
        bool includeDetails)
    {
        string blockType = "Unknown";
        try { blockType = fat.BlockDefinition.ToString(); }
        catch { blockType = fat.GetType().Name; }

        var terminal = fat as Sandbox.ModAPI.Ingame.IMyTerminalBlock;

        var dto = new TerminalBlockDto
        {
            GridPosition = new Vector3IDto
            {
                X = slim.Position.X,
                Y = slim.Position.Y,
                Z = slim.Position.Z
            },
            Name = fat.DisplayNameText
                ?? fat.DefinitionDisplayNameText
                ?? fat.GetType().Name,
            Type = blockType,
            EntityId = fat.EntityId,
            IsFunctional = terminal?.IsFunctional ?? false,
            IsWorking = terminal?.IsWorking ?? false,
            Definition = blockType,
        };

        if (includeDetails && terminal != null)
        {
            dto.Actions = ExtractActions(terminal);
            dto.Properties = ExtractProperties(terminal);
        }

        return dto;
    }

    private static List<BlockActionDto> ExtractActions(
        Sandbox.ModAPI.Ingame.IMyTerminalBlock terminal)
    {
        var actions = new List<BlockActionDto>();

        try
        {
            var list = new List<ITerminalAction>();
            terminal.GetActions(list, null);
            foreach (var a in list)
            {
                if (a == null) continue;
                actions.Add(new BlockActionDto
                {
                    Id = a.Id,
                    Name = a.Name?.ToString() ?? "",
                    IsEnabled = a.IsEnabled(terminal),
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"ExtractActions: {ex.Message}");
        }

        return actions;
    }

    private static List<BlockPropertyDto> ExtractProperties(
        Sandbox.ModAPI.Ingame.IMyTerminalBlock terminal)
    {
        var props = new List<BlockPropertyDto>();

        try
        {
            var list = new List<ITerminalProperty>();
            terminal.GetProperties(list, null);
            foreach (var p in list)
            {
                if (p == null) continue;

                // Try to get value via typed As<>
                string val = "?";
                try
                {
                    val = p.As<string>().GetValue(terminal);
                }
                catch
                {
                    try
                    {
                        switch (p.TypeName)
                        {
                            case "Boolean":
                                val = p.AsBool().GetValue(terminal).ToString();
                                break;
                            case "Single":
                                val = p.AsFloat().GetValue(terminal)
                                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            case "Color":
                                var c = p.AsColor().GetValue(terminal);
                                val = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                                break;
                            case "Int64":
                                val = p.As<long>().GetValue(terminal).ToString();
                                break;
                            case "Int32":
                                val = p.As<int>().GetValue(terminal).ToString();
                                break;
                        }
                    }
                    catch { }
                }

                props.Add(new BlockPropertyDto
                {
                    Id = p.Id,
                    Name = p.Id,
                    Value = val,
                    PropertyType = p.TypeName,
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"ExtractProperties: {ex.Message}");
        }

        return props;
    }

    private static Color? ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Try "R G B" format
        var parts = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 &&
            int.TryParse(parts[0].TrimStart('#'), out int r) &&
            int.TryParse(parts[1].TrimStart('#'), out int g) &&
            int.TryParse(parts[2].TrimStart('#'), out int b))
        {
            return new Color(r, g, b);
        }

        // Try "#RRGGBB" hex
        if (value.StartsWith("#") && value.Length == 7)
        {
            try
            {
                r = Convert.ToInt32(value.Substring(1, 2), 16);
                g = Convert.ToInt32(value.Substring(3, 2), 16);
                b = Convert.ToInt32(value.Substring(5, 2), 16);
                return new Color(r, g, b);
            }
            catch { }
        }

        return null;
    }

    // ── PB/LCD convenience (find-first-by-type) ──────────────

    public static bool UploadScript(MyCubeGrid grid, string code)
    {
        try
        {
            var blocks = new List<IMySlimBlock>();
            ((IMyCubeGrid)grid).GetBlocks(blocks, null);

            foreach (var slim in blocks)
            {
                if (slim?.FatBlock is Sandbox.ModAPI.IMyProgrammableBlock pb)
                {
                    pb.ProgramData = code;
                    Logger.Info($"UploadScript: {code.Length} chars to PB {pb.EntityId}");
                    return true;
                }
            }

            Logger.Warn("UploadScript: no programmable block found on grid");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"UploadScript: {ex.Message}");
            return false;
        }
    }

    public static ScriptRunResult RunScript(MyCubeGrid grid, string argument)
    {
        try
        {
            var blocks = new List<IMySlimBlock>();
            ((IMyCubeGrid)grid).GetBlocks(blocks, null);

            foreach (var slim in blocks)
            {
                if (slim?.FatBlock is Sandbox.ModAPI.Ingame.IMyProgrammableBlock pb)
                {
                    bool ok = pb.TryRun(argument ?? "");
                    string echo = "";
                    try { echo = GetPbEcho(pb); }
                    catch (Exception ex) { Logger.Warn($"RunScript echo: {ex.Message}"); }

                    Logger.Info($"RunScript: arg=\"{argument}\" ok={ok} echo={echo.Length} chars");
                    return new ScriptRunResult { Echo = echo, Output = echo, Success = ok };
                }
            }

            Logger.Warn("RunScript: no programmable block found on grid");
            return new ScriptRunResult { Echo = "", Success = false };
        }
        catch (Exception ex)
        {
            Logger.Error($"RunScript: {ex.Message}");
            return new ScriptRunResult { Echo = "", Success = false };
        }
    }

    public static string GetLcdContent(MyCubeGrid grid)
    {
        try
        {
            var blocks = new List<IMySlimBlock>();
            ((IMyCubeGrid)grid).GetBlocks(blocks, null);

            // First pass: dedicated LCD panels (not PBs)
            foreach (var slim in blocks)
            {
                var fat = slim?.FatBlock;
                if (fat == null) continue;

                // Dedicated LCD panel
                if (fat is Sandbox.ModAPI.Ingame.IMyTextPanel panel)
                {
                    var text = panel.GetText();
                    if (!string.IsNullOrEmpty(text)) return text;
                }
            }

            // Second pass: programmable block surfaces (where PB scripts write their output)
            foreach (var slim in blocks)
            {
                if (slim?.FatBlock is Sandbox.ModAPI.IMyProgrammableBlock pb)
                {
                    // PB's built-in display surface — scripts can write here via Me.GetSurface(0)
                    try
                    {
                        if (pb is Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider sp)
                        {
                            var text = sp.GetSurface(0)?.GetText();
                            if (!string.IsNullOrEmpty(text)) return text;
                        }
                    }
                    catch { }
                }
            }

            // Third pass: other text surface providers
            foreach (var slim in blocks)
            {
                var fat = slim?.FatBlock;
                if (fat == null) continue;
                if (fat is Sandbox.ModAPI.IMyProgrammableBlock) continue; // already checked above
                if (fat is Sandbox.ModAPI.Ingame.IMyTextPanel) continue;   // already checked above

                if (fat is Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider provider)
                {
                    var text = provider.GetSurface(0)?.GetText();
                    if (!string.IsNullOrEmpty(text)) return text;
                }
            }

            return "";
        }
        catch (Exception ex)
        {
            Logger.Error($"GetLcdContent: {ex.Message}");
            return "";
        }
    }

    private static string GetPbEcho(Sandbox.ModAPI.Ingame.IMyProgrammableBlock pb)
    {
        try
        {
            var type = pb.GetType();

            // Try multiple field/property names (public, non-public, instance)
            var flags = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic;

            foreach (var name in new[] { "Echo", "GetEcho", "LastEcho", "m_echo", "_echo", "EchoText", "m_echoStringBuilder", "EchoOutput" })
            {
                // Try field
                var field = type.GetField(name, flags);
                if (field != null)
                {
                    var val = field.GetValue(pb);
                    if (val is string s && !string.IsNullOrEmpty(s)) return s;
                    if (val is System.Text.StringBuilder sb && sb.Length > 0) return sb.ToString();
                }

                // Try property
                var prop = type.GetProperty(name, flags);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var val = prop.GetValue(pb) as string;
                    if (!string.IsNullOrEmpty(val)) return val;
                }

                // Try parameterless method
                var method = type.GetMethod(name, flags, null, Type.EmptyTypes, null);
                if (method != null && method.ReturnType == typeof(string))
                {
                    var val = method.Invoke(pb, null) as string;
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
        }
        catch { }

        return "";
    }
}
