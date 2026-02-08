using System.Diagnostics;
using System.Text;

namespace ThreeDGodCreator.Core.Services;

public static class DiagnosticsService
{
    public static string RunSystemCheck()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== System-Check ===\n");

        var basePath = AppDomain.CurrentDomain.BaseDirectory;

        var cfg = new ConfigService().Load();
        var blenderOk = !string.IsNullOrEmpty(cfg.BlenderPath) && File.Exists(cfg.BlenderPath);
        sb.AppendLine($"[{(blenderOk ? "OK" : "--")}] Blender: {(blenderOk ? cfg.BlenderPath : "Nicht gefunden - in Einstellungen setzen")}");

        var assetsPath = Path.Combine(basePath, "assets", "characters");
        var assetsOk = Directory.Exists(assetsPath);
        sb.AppendLine($"[{(assetsOk ? "OK" : "--")}] Assets: {(assetsOk ? "gefunden" : "nicht gefunden")}");

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var r = client.GetAsync("http://localhost:5000/").GetAwaiter().GetResult();
            var fpOk = r.IsSuccessStatusCode;
            sb.AppendLine($"[{(fpOk ? "OK" : "--")}] FauxPilot: {(fpOk ? "läuft" : "nicht erreichbar (Port 5000)")}");
        }
        catch
        {
            sb.AppendLine("[--] FauxPilot: nicht erreichbar");
        }

        return sb.ToString();
    }
}
