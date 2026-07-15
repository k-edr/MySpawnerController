using System;
using System.IO;
using VRage.Utils;

namespace GridSpawner.Plugin
{
    /// <summary>
    /// Simple file-based logger for debugging plugin behavior.
    /// Writes to %AppData%/SpaceEngineers/GridSpawner.log.
    /// Uses MyLog.Default when available (after game init), falls back to file.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath;
        private static bool _initialized;

        static Logger()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            LogFilePath = Path.Combine(appData, "SpaceEngineers", "GridSpawner.log");
        }

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                string dir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Start with a clean log each session
                File.WriteAllText(LogFilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] GridSpawner.Plugin plugin started{Environment.NewLine}");
            }
            catch { /* Can't log if file I/O fails */ }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", $"{message}: {ex}");
        }

        private static void Write(string level, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

            try
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch { }

            // Also write to game log if available
            try
            {
                MyLog.Default?.WriteLine($"[GridSpawner.Plugin] {line}");
            }
            catch { }
        }
    }
}
