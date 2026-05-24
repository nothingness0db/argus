using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HotspotManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                var msg = ex?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown";
                CrashLog(msg);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                CrashLog(args.Exception.ToString());
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                CrashLog(args.Exception.ToString());
                args.SetObserved();
            };
        }

        private static void CrashLog(string message)
        {
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.WriteAllText(path,
                    $"=== Crash Report {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\r\n{message}");
            }
            catch { }
        }
    }
}
