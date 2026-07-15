using System;
using System.IO;
using System.Reflection;
using VRage.Plugins;

namespace GridSpawner.AutoWorldLoader
{
    /// <summary>
    /// Auto-loads a saved world when the game reaches the main menu.
    /// Configure world name in %APPDATA%\SpaceEngineers\GridSpawner.json
    /// under "autoLoadWorld" key.
    ///
    /// Works by calling MySessionLoader via reflection — the same API
    /// the game itself uses when clicking "Load Game" in the menu.
    /// </summary>
    public class Plugin : IPlugin
    {
        private bool _loaded;
        private string _worldName;
        private string _savePath;

        private static readonly BindingFlags Flags =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public void Init(object gameInstance)
        {
            try
            {
                _worldName = ReadWorldName();
                if (string.IsNullOrEmpty(_worldName))
                {
                    // No world configured — silently exit
                    return;
                }

                var savesRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SpaceEngineers", "Saves", "76561199017018740");

                _savePath = Path.Combine(savesRoot, _worldName);

                if (!Directory.Exists(_savePath))
                {
                    Log($"Save not found: {_savePath}");
                    return;
                }

                Log($"AutoWorldLoader ready. Will load: {_worldName}");
            }
            catch (Exception ex)
            {
                Log($"Init error: {ex.Message}");
            }
        }

        public void Update()
        {
            if (_loaded || string.IsNullOrEmpty(_savePath))
                return;

            try
            {
                // Only proceed if game is at main menu (no session loaded)
                if (IsMainMenu())
                {
                    Log($"Main menu detected, loading world: {_worldName}");
                    LoadWorld(_savePath);
                    _loaded = true;
                }
            }
            catch (Exception ex)
            {
                Log($"Update error: {ex.Message}");
                _loaded = true; // Don't retry on error
            }
        }

        public void Dispose() { }

        // ── Detection ────────────────────────────────────────

        private static bool IsMainMenu()
        {
            // Game is at main menu when the session is null and the game
            // has finished initializing (MySandboxGame exists).
            // Also check that we're not in the middle of loading.
            var gameType = Type.GetType("Sandbox.MySandboxGame, Sandbox.Game");
            if (gameType == null) return false;

            var staticProp = gameType.GetProperty("Static", Flags);
            var game = staticProp?.GetValue(null);
            if (game == null) return false;

            // Check IsLoaded or similar
            var isLoadedProp = gameType.GetProperty("IsLoaded", Flags);
            bool isLoaded = isLoadedProp != null && (bool)isLoadedProp.GetValue(game);
            if (!isLoaded) return false;

            // Session.Static is null when at main menu
            var sessionType = Type.GetType("Sandbox.Game.World.MySession, Sandbox.Game");
            var sessionProp = sessionType?.GetProperty("Static", Flags);
            var session = sessionProp?.GetValue(null);
            return session == null;
        }

        // ── World loading ────────────────────────────────────

        private static void LoadWorld(string savePath)
        {
            // MySessionLoader is in Sandbox.Game.World
            var loaderType = Type.GetType("Sandbox.Game.World.MySessionLoader, Sandbox.Game");
            if (loaderType == null)
            {
                Log("ERROR: MySessionLoader type not found");
                return;
            }

            // Try method: LoadSingleplayerSession(string sessionPath)
            var method = loaderType.GetMethod("LoadSingleplayerSession", Flags);
            if (method != null)
            {
                Log($"Calling LoadSingleplayerSession({savePath})");
                method.Invoke(null, new object[] { savePath });
                return;
            }

            // Fallback: try LoadSession or StartSession
            foreach (var m in loaderType.GetMethods(Flags))
            {
                var par = m.GetParameters();
                if (par.Length == 1 && par[0].ParameterType == typeof(string))
                {
                    Log($"Trying fallback method: {m.Name}");
                    m.Invoke(null, new object[] { savePath });
                    return;
                }
            }

            Log("ERROR: No suitable LoadSession method found");
        }

        // ── Config ───────────────────────────────────────────

        private static string ReadWorldName()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "GridSpawner.json");

            if (!File.Exists(configPath))
            {
                Log("GridSpawner.json not found — auto-load disabled");
                return null;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                // Simple JSON parsing (no dependency on System.Text.Json)
                var key = "\"autoLoadWorld\"";
                var idx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;

                var colonIdx = json.IndexOf(':', idx + key.Length);
                if (colonIdx < 0) return null;

                var openQuote = json.IndexOf('"', colonIdx + 1);
                if (openQuote < 0) return null;

                var closeQuote = json.IndexOf('"', openQuote + 1);
                if (closeQuote < 0) return null;

                return json.Substring(openQuote + 1, closeQuote - openQuote - 1);
            }
            catch (Exception ex)
            {
                Log($"Config read error: {ex.Message}");
                return null;
            }
        }

        private static void Log(string msg)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "AutoWorldLoader.log");

            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
