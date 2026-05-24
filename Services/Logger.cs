using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;

namespace HotspotManager.Services
{
    public enum LogLevel { Info, Warn, Error }

    public class LogEntry
    {
        public DateTime Time { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
        public string Source { get; set; } = "";

        public string LevelText => Level switch
        {
            LogLevel.Error => "ERR",
            LogLevel.Warn => "WRN",
            LogLevel.Info => "INF",
            _ => "???"
        };

        public string DisplayText => $"[{Time:HH:mm:ss}] [{LevelText}] [{Source}] {Message}";
    }

    public static class Logger
    {
        private static readonly ObservableCollection<LogEntry> _entries = new ObservableCollection<LogEntry>();
        private static readonly object _lock = new object();
        private static string _logFilePath = "";
        private static bool _initialized;

        public static ObservableCollection<LogEntry> Entries => _entries;

        private static readonly Encoding _utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                _logFilePath = Path.Combine(dir, $"hotspot_{DateTime.Now:yyyyMMdd}.log");
                if (!File.Exists(_logFilePath))
                    File.WriteAllText(_logFilePath, "", _utf8Bom);
                File.AppendAllText(_logFilePath, $"=== HotspotManager Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine, _utf8Bom);
            }
            catch { }

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_entries, _lock);
        }

        public static void Info(string source, string message) => Log(LogLevel.Info, source, message);
        public static void Warn(string source, string message) => Log(LogLevel.Warn, source, message);
        public static void Warn(string source, string message, Exception ex) =>
            Log(LogLevel.Warn, source, $"{message}: {ex.GetType().Name} - {ex.Message}");

        public static void Error(string source, string message) => Log(LogLevel.Error, source, message);
        public static void Error(string source, string message, Exception ex) =>
            Log(LogLevel.Error, source, $"{message}: {ex.GetType().Name} - {ex.Message}");

        public static void TrInfo(string source, string key, params object[] args) =>
            Log(LogLevel.Info, source, LocaleService.Format(key, args));
        public static void TrWarn(string source, string key, params object[] args) =>
            Log(LogLevel.Warn, source, LocaleService.Format(key, args));
        public static void TrWarn(string source, string key, Exception ex, params object[] args) =>
            Log(LogLevel.Warn, source, $"{LocaleService.Format(key, args)}: {ex.GetType().Name} - {ex.Message}");
        public static void TrError(string source, string key, params object[] args) =>
            Log(LogLevel.Error, source, LocaleService.Format(key, args));
        public static void TrError(string source, string key, Exception ex, params object[] args) =>
            Log(LogLevel.Error, source, $"{LocaleService.Format(key, args)}: {ex.GetType().Name} - {ex.Message}");

        private static void Log(LogLevel level, string source, string message)
        {
            var entry = new LogEntry
            {
                Time = DateTime.Now,
                Level = level,
                Source = source,
                Message = message
            };

            lock (_lock)
            {
                _entries.Add(entry);
            }

            var line = entry.DisplayText;
            System.Diagnostics.Debug.WriteLine(line);
            Console.WriteLine(line);

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, _utf8Bom);
                }
                catch { }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
            }
        }

        public static string LogFilePath => _logFilePath;
    }
}
