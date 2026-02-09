using System.Text;

namespace ThreeDGodCreator.Core.Services;

/// <summary>
/// Runs system diagnostics and integrates with ProjectReadinessService.
/// </summary>
public static class DiagnosticsService
{
    /// <summary>
    /// Runs full readiness check and returns a formatted report.
    /// Also logs to error_log.txt via StartupLogger.
    /// </summary>
    public static string RunSystemCheck()
    {
        var result = ProjectReadinessService.RunFullCheck();
        var sb = new StringBuilder();
        sb.AppendLine("=== Project Readiness Check ===");
        sb.AppendLine();
        foreach (var line in result.SummaryLines)
            sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine($"Blender: {(result.Blender.Success ? result.Blender.Message : result.Blender.Message + " - " + result.Blender.SuggestedFix)}");
        sb.AppendLine($"Assets: {(result.Assets.Success ? "OK" : result.Assets.Message)}");
        sb.AppendLine();

        sb.AppendLine("FauxPilot (optional):");
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var r = client.GetAsync("http://localhost:5000/").GetAwaiter().GetResult();
            sb.AppendLine(r.IsSuccessStatusCode ? "[OK] FauxPilot läuft auf Port 5000" : "[--] FauxPilot nicht erreichbar");
        }
        catch
        {
            sb.AppendLine("[--] FauxPilot nicht erreichbar (optional für KI-Features)");
        }

        sb.AppendLine();
        sb.AppendLine($"Log-Datei: {StartupLogger.GetLogFilePath()}");

        return sb.ToString();
    }
}
