using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Infrastructure;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppLogger.Initialize();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

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
        AppLogger.SetShutdownReason($"CRASH: {e.Exception.GetType().Name}: {e.Exception.Message}");
        DebugLog.Write($"[FATAL] Unbehandelte Exception: {e.Exception.Message}");
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppLogger.LogException(ex, "UnhandledException");
            AppLogger.SetShutdownReason($"CRASH: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
