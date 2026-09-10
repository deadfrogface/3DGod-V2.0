using Velopack;

namespace ThreeDGodCreator.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Production Velopack hook — always runs before UI or smoke path.
        VelopackApp.Build().Run();

        if (InstalledAppSmoke.IsRequested(args))
        {
            Environment.ExitCode = InstalledAppSmoke.Run(args);
            return;
        }

        new App().Run();
    }
}
