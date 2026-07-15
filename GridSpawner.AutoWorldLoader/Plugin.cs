using System;
using System.IO;
using System.Reflection;
using VRage.Plugins;

namespace GridSpawner.AutoWorldLoader
{
    /// <summary>
    /// Auto-loads a saved world when the game reaches the main menu.
    /// Configure via "autoLoadWorld" in %APPDATA%\SpaceEngineers\GridSpawner.json
    /// </summary>
    public class Plugin : IPlugin
    {
        private bool _loaded;
        private int _frameCount;
        private string _worldName;
        private string _savePath;

        private static readonly BindingFlags Flags =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public void Init(object gameInstance)
        {
            try
            {
                Log("=== AutoWorldLoader Init ===");

                _worldName = ReadWorldName();
                if (string.IsNullOrEmpty(_worldName))
                {
                    Log("autoLoadWorld not configured — disabled");
                    return;
                }

                var savesRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SpaceEngineers", "Saves", "76561199017018740");

                _savePath = Path.Combine(savesRoot, _worldName);

                if (!Directory.Exists(_savePath))
                {
                    Log($"ERROR: Save not found: {_savePath}");
                    return;
                }

                Log($"Ready. World: {_worldName}, Path: {_savePath}");
            }
            catch (Exception ex)
            {
                Log($"Init error: {ex}");
            }
        }

        public void Update()
        {
            if (_loaded || string.IsNullOrEmpty(_savePath))
                return;

            _frameCount++;

            // Wait 180 frames (~3 sec @ 60fps) for the game to settle
            if (_frameCount < 180)
                return;

            // Check every 60 frames
            if (_frameCount % 60 != 0)
                return;

            try
            {
                var atMainMenu = IsAtMainMenu();
                Log($"Frame {_frameCount}: atMainMenu={atMainMenu}");

                if (atMainMenu)
                {
                    Log($"Loading world: {_worldName}");
                    LoadWorld(_savePath);
                    _loaded = true;
                }
            }
            catch (Exception ex)
            {
                Log($"Update error: {ex}");
                _loaded = true;
            }
        }

        public void Dispose() { }

        // ── Detection ────────────────────────────────────────

        private static bool IsAtMainMenu()
        {
            // The simplest reliable check: MySession.Static is null when no world loaded
            var sessionType = Type.GetType("Sandbox.Game.World.MySession, Sandbox.Game");
            if (sessionType == null)
            {
                Log("  MySession type not found");
                return false;
            }

            var staticProp = sessionType.GetProperty("Static", Flags);
            if (staticProp == null)
            {
                Log("  MySession.Static property not found");
                return false;
            }

            var session = staticProp.GetValue(null);
            return session == null;
        }

        // ── World loading ────────────────────────────────────

        private static void LoadWorld(string savePath)
        {
            var loaderType = Type.GetType("Sandbox.Game.World.MySessionLoader, Sandbox.Game");
            if (loaderType == null)
            {
                Log("ERROR: MySessionLoader type not found");
                return;
            }

            Log("MySessionLoader found.");

            // Prefer LoadSessionByPath (single string parameter)
            var method = loaderType.GetMethod("LoadSessionByPath", Flags);
            if (method != null)
            {
                Log($"Calling {method.Name}({savePath})");
                method.Invoke(null, new object[] { savePath });
                return;
            }

            // Fallback: LoadLastSession (Continue button equivalent)
            method = loaderType.GetMethod("LoadLastSession", Flags);
            if (method != null)
            {
                Log($"Calling {method.Name}()");
                method.Invoke(null, null);
                return;
            }

            Log("ERROR: No suitable method found");
        }

        // ── Config ───────────────────────────────────────────

        private static string ReadWorldName()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "GridSpawner.json");

            if (!File.Exists(configPath))
            {
                Log("GridSpawner.json not found");
                return null;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var key = "\"autoLoadWorld\"";
                var idx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    Log("autoLoadWorld key not found in config");
                    return null;
                }

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
                Log($"Config read error: {ex}");
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
