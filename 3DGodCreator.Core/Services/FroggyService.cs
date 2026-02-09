namespace ThreeDGodCreator.Core.Services;

public record FroggyResult(
    string Problem,
    string Cause,
    string Suggestion,
    bool CanFix,
    string? FixAction = null
);

public static class FroggyService
{
    public static FroggyResult AnalyzeLog(string logText)
    {
        var lower = logText.ToLowerInvariant();

        if (lower.Contains("blender") && (lower.Contains("nicht gefunden") || lower.Contains("nicht konfiguriert") || lower.Contains("datei nicht finden")))
            return new FroggyResult(
                "Blender wurde nicht gefunden.",
                "Der Blender-Pfad ist nicht gesetzt oder zeigt auf eine nicht vorhandene Datei.",
                "Gehe zu Einstellungen und setze den Blender-Pfad (z.B. C:\\Program Files\\Blender Foundation\\Blender 4.0\\blender.exe).",
                true,
                "OpenSettings"
            );

        if (lower.Contains("export_fbx") && lower.Contains("nicht gefunden"))
            return new FroggyResult(
                "FBX-Export-Skript fehlt.",
                "Die Datei blender_embed/scripts/export_fbx.py wurde nicht gefunden.",
                "Stelle sicher, dass das Projekt vollständig gebaut wurde. Die blender_embed-Ordner werden beim Build kopiert.",
                false
            );

        if (lower.Contains("sculpt") && (lower.Contains("nicht gefunden") || lower.Contains("skript")))
            return new FroggyResult(
                "Sculpt-Skript fehlt.",
                "apply_sculpt_standalone.py wurde nicht gefunden.",
                "Prüfe ob blender_embed/apply_sculpt_standalone.py im Ausgabeverzeichnis existiert.",
                false
            );

        if (lower.Contains("glb") && (lower.Contains("fehlgeschlagen") || lower.Contains("nicht unterstützt")))
            return new FroggyResult(
                "GLB-Anzeige fehlgeschlagen.",
                "Das GLB-Modell konnte nicht geladen werden (SharpGLTF-Fehler oder defekte Datei).",
                "Prüfe ob die GLB-Datei valide ist. Fallback: Anatomie-Vorschau-Bilder werden angezeigt.",
                false
            );

        if (lower.Contains("pfad nicht gefunden") || lower.Contains("nicht gefunden:"))
            return new FroggyResult(
                "Datei oder Ordner nicht gefunden.",
                "Ein angegebener Pfad existiert nicht.",
                "Prüfe ob assets/ und blender_embed/ ins Ausgabeverzeichnis kopiert wurden (Build ausführen).",
                false
            );

        if (lower.Contains("fauxpilot") || lower.Contains("port 5000"))
            return new FroggyResult(
                "FauxPilot nicht erreichbar.",
                "Der FauxPilot-AI-Service läuft nicht auf Port 5000.",
                "FauxPilot ist optional. Für KI-Features starte den Python-FauxPilot-Server separat.",
                false
            );

        return new FroggyResult(
            "Kein spezifisches Problem erkannt.",
            "Froggy hat die Logs analysiert, aber kein bekanntes Muster gefunden.",
            "Prüfe die Log-Zeilen oben. Bei Blender-Problemen: Einstellungen → Blender-Pfad setzen.",
            false
        );
    }

    public static string AnswerQuestion(string question, string logText)
    {
        var lower = question.ToLowerInvariant();
        if (lower.Contains("blender") || (lower.Contains("warum") && lower.Contains("export")))
            return "Blender muss installiert sein und der Pfad in den Einstellungen gesetzt werden. Öffne Einstellungen und wähle blender.exe aus.";
        if ((lower.Contains("kopfüber") || lower.Contains("kopf über") || lower.Contains("upside down") || lower.Contains("steht") && lower.Contains("figur")) ||
            (lower.Contains("3d") && (lower.Contains("falsch") || lower.Contains("orientierung") || lower.Contains("drehen"))))
            return "Die 3D-Figur sollte jetzt richtig stehen (180° um X gedreht). Wenn sie noch kopfüber ist, liegt es an der GLB-Datei – prüfe das Modell in Blender.";
        if (lower.Contains("3d") && (lower.Contains("vorschau") || lower.Contains("anzeige")))
            return "Die 3D-Vorschau lädt GLB-Modelle mit SharpGLTF. Wenn das fehlschlägt, werden Anatomie-Bilder angezeigt. Prüfe ob male_base.glb und female_base.glb in assets/characters/ liegen.";
        if (lower.Contains("steuerung") || lower.Contains("bedienung") || (lower.Contains("drehen") && lower.Contains("wie")))
            return "Rechtsklick ziehen = Drehen · Shift+Rechtsklick = Verschieben · Mausrad = Zoom. Die Hinweise stehen über der 3D-Vorschau.";
        if (lower.Contains("preset") || lower.Contains("slider") && (lower.Contains("verschwindet") || lower.Contains("weg")))
            return "Das Preset bleibt jetzt beim Slider-Bewegen erhalten – die 3D-Vorschau wird nicht mehr durch Anatomie-Bilder ersetzt.";
        if (lower.Contains("froggy") || lower.Contains("hilf"))
            return "Klick auf 'Froggy fragen' um die Logs analysieren zu lassen. Froggy erkennt z.B. fehlenden Blender-Pfad und gibt Hinweise.";
        if (lower.Contains("rigging") || lower.Contains("auto rig"))
            return "Auto-Rigging und 3D-Generierung benötigen Blender. Setze den Blender-Pfad in den Einstellungen. Die Python-Skripte liegen in blender_embed/.";
        return "Froggy hat deine Frage gelesen. Versuch z.B.: 'Warum steht die 3D-Figur kopfüber?', 'Wie bediene ich die 3D-Vorschau?' oder nutze den Froggy-Button für eine Log-Analyse.";
    }
}
