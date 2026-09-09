using System.Windows;
using Velopack;

namespace ThreeDGodCreator.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        new App().Run();
    }
}
