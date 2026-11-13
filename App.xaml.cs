using System;
using System.IO;
using System.Threading;
using System.Windows;
using ScreanX.Services;

namespace ScreanX
{
    public partial class App : System.Windows.Application
    {
        public static bool StartInTray { get; private set; } = false;

        private const string MutexName = @"Global\ScreanX_SingleInstance_Mutex_9921";
        private const string EventName = @"Global\ScreanX_RestoreEvent_9921";
        private static Mutex? _instanceMutex;
        private static EventWaitHandle? _restoreEvent;
        private static RegisteredWaitHandle? _registeredWait;

        private static readonly string LogFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreanX", "app_startup.log"
        );

        public static void Log(string msg)
        {
            try
            {
                string dir = Path.GetDirectoryName(LogFile)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}\n");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log($"UnhandledException: {args.ExceptionObject}");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Log($"DispatcherUnhandledException: {args.Exception}");
                args.Handled = true;
            };

            Log("App OnStartup beginning");

            // 1. Single-Instance Check
            bool isNewInstance;
            try
            {
                _instanceMutex = new Mutex(true, MutexName, out isNewInstance);
            }
            catch
            {
                isNewInstance = false;
            }

            if (!isNewInstance)
            {
                Log("Duplicate instance detected. Signaling main instance to restore and exiting.");
                try
                {
                    if (EventWaitHandle.TryOpenExisting(EventName, out var existingEvent))
                    {
                        existingEvent.Set();
                    }
                }
                catch { }

                Environment.Exit(0);
                return;
            }

            // 2. First instance: listen for restore signal from subsequent launches
            try
            {
                _restoreEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
                _registeredWait = ThreadPool.RegisterWaitForSingleObject(_restoreEvent, (state, timedOut) =>
                {
                    Current?.Dispatcher?.Invoke(() =>
                    {
                        if (Current.MainWindow is Views.MainWindow mainWindow)
                        {
                            mainWindow.ShowAndRestore();
                        }
                    });
                }, null, -1, false);
            }
            catch { }

            // 3. Process arguments
            foreach (var arg in e.Args)
            {
                if (arg.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-tray", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                {
                    StartInTray = true;
                    Log("App launched with --tray flag");
                }
            }

            base.OnStartup(e);

            SettingsService.Load();
            HistoryService.Load();

            // Apply saved M3 theme
            var settings = SettingsService.Current;
            ThemeService.ApplyTheme(settings.ThemeMode, settings.ColorPalette);
            Log("App OnStartup finished");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log($"App OnExit with code {e.ApplicationExitCode}");
            try
            {
                _registeredWait?.Unregister(null);
                _restoreEvent?.Dispose();
                _instanceMutex?.ReleaseMutex();
                _instanceMutex?.Dispose();
            }
            catch { }

            SettingsService.Save();
            HistoryService.Save();
            base.OnExit(e);
        }
    }
}
