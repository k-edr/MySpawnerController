using System;
using System.IO;
using GridSpawner.Shared.Configuration;

namespace GridSpawner.Plugin.Infrastructure
{
    public static class Logger
    {
        private static readonly string LogFilePath = AppDefaults.DefaultLogFile;
        private static readonly object _lock = new();
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                string dir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(LogFilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] GridSpawner.Plugin plugin started{Environment.NewLine}");
            }
            catch { }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Error(string message, Exception ex) =>
            Write("ERROR", $"{message}: {ex}");

        private static void Write(string level, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

            lock (_lock)
            {
                try { File.AppendAllText(LogFilePath, line + Environment.NewLine); }
                catch { }
            }

            try { VRage.Utils.MyLog.Default?.WriteLine($"[GridSpawner.Plugin] {line}"); }
            catch { }
        }
    }
}
