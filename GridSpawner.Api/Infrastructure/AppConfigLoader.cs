using System;
using System.IO;
using System.Text.Json;
using GridSpawner.Shared;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Loads/saves <see cref="AppConfig"/> from %APPDATA%\SpaceEngineers\GridSpawner.json.
/// </summary>
public static class AppConfigLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string ConfigPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpaceEngineers", "GridSpawner.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            // Corrupt config — fall back to defaults
            System.Diagnostics.Debug.WriteLine($"[AppConfig] Failed to load: {ex.Message}");
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, JsonOpts);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppConfig] Failed to save: {ex.Message}");
        }
    }

    /// <summary>Creates default config file if it doesn't exist.</summary>
    public static AppConfig LoadOrCreate()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }
        return Load();
    }
}
