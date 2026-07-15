using System;
using System.IO;

namespace GridSpawner.Shared.Configuration;

/// <summary>
/// Centralised default values and path constants.
/// All magic strings live here — single place to change.
/// </summary>
public static class AppDefaults
{
    // ── Paths ──

    public static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpaceEngineers");

    public static readonly string DefaultBlueprintsFolder = Path.Combine(
        AppDataRoot, "Blueprints", "local");

    public static readonly string DefaultConfigFile = Path.Combine(
        AppDataRoot, "GridSpawner.json");

    public static readonly string DefaultLogFile = Path.Combine(
        AppDataRoot, "GridSpawner.log");

    public const string BlueprintExtension = "bp.sbc";

    // ── Networking ──

    public const string DefaultApiScheme = "http";
    public const string DefaultHost = "localhost";
    public const int DefaultApiPort = 9997;
    public const int DefaultSwaggerPort = 9998;

    // ── Limits ──

    public const long DefaultMaxBlueprintFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    public const int DefaultMaxGridsPerBlueprint = 50;
}
