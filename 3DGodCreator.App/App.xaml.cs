using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Infrastructure;
using ThreeDGod.Infrastructure.Logging;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppLogger.Initialize();
        GodLog.Initialize();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var services = new ServiceCollection();
        services.AddThreeDGodCoreServices();
        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLogger.LogException(e.Exception, "DispatcherUnhandledException");
        GodLog.LogCrash(e.Exception, "DispatcherUnhandledException");
        TryCapture(e.Exception, "DispatcherUnhandledException");
        AppLogger.SetShutdownReason($"CRASH: {e.Exception.GetType().Name}: {e.Exception.Message}");
        DebugLog.Write($"[FATAL] Unbehandelte Exception: {e.Exception.Message}");
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppLogger.LogException(ex, "UnhandledException");
            GodLog.LogCrash(ex, "UnhandledException");
            TryCapture(ex, "UnhandledException");
            AppLogger.SetShutdownReason($"CRASH: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        GodLog.LogCrash(e.Exception, "UnobservedTaskException");
        TryCapture(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }

    private static void TryCapture(Exception exception, string source)
    {
        try
        {
            if (Current is App app)
                app.Services?.GetService<ThreeDGod.Core.Diagnostics.IDiagnosticService>()?.Capture(exception, source);
        }
        catch (Exception captureEx)
        {
            GodLog.Write($"Diagnostic capture failed: {captureEx.Message}");
        }
    }
}
