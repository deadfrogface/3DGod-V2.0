using System.Windows;
using System.Windows.Threading;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppLogger.Initialize();

        // Prevent silent crashes - log all unhandled exceptions
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLogger.LogException(e.Exception, "DispatcherUnhandledException");
        DebugLog.Write($"[FATAL] Unbehandelte Exception: {e.Exception.Message}");
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            AppLogger.LogException(ex, "UnhandledException");
    }
}
