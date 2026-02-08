# 3D God Creator – C#/.NET

Native C#-Version des 3D God Creator. Kein Python, kein PyTorch, kein pip.

## Anforderungen

- .NET 8 SDK
- Windows (WPF)
- Blender (optional, für Sculpting/Export)

## Build & Start

```powershell
cd 3DGodCreator
dotnet build
dotnet run --project 3DGodCreator.App
```

Beim ersten Build werden `assets/` und `blender_embed/` ins Ausgabeverzeichnis kopiert.

## Projektstruktur

- **3DGodCreator.Core** – Domain, Services, Models
- **3DGodCreator.App** – WPF-UI
- **blender_embed/** – Python-Skripte für Blender (werden von Blender ausgeführt)

## Funktionen (Phase 1–3)

- **Phase 1:** UI, CharacterSystem, Blender-Anbindung, 3D-Viewport (HelixToolkit)
- **Phase 2:** AI-Service-Layer (ONNX/HTTP-Stubs)
- **Phase 3:** System-Check, Debug-Konsole

## Blender-Pfad

Blender-Pfad in den **Einstellungen** setzen, z.B.:

`C:\Program Files\Blender Foundation\Blender 4.0\blender.exe`

## Assets

`assets/characters/male_base.glb` und `female_base.glb` liegen im Projekt und werden beim Build kopiert.
