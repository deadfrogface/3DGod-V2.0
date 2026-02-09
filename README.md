# 3D God Creator V2.0 – C#/.NET

Native C#-Migration des 3D God Creator (Python V1.2). Kein Python-Runtime, kein PyTorch.

## Anforderungen

- .NET 8 SDK
- Windows (WPF)
- Blender (optional, für Sculpting/Export)

## Build & Start

```powershell
cd 3DGod-V2.0
dotnet build
dotnet run --project 3DGodCreator.App
```

Beim ersten Build werden `assets/` und `blender_embed/` ins Ausgabeverzeichnis kopiert.

## Funktionen (vollständige Migration)

| Tab | Funktionen |
|-----|------------|
| **Form** | Körperform-Slider (body_parameters.json), männlich/weiblich |
| **Sculpt** | Symmetrie, Blender-Sculpting starten |
| **NSFW** | Anatomie-Layer (Haut, Fett, Muskeln, Knochen, Organe), NSFW-Layer |
| **Kleidung** | Kleidung, Piercings, Tattoos laden |
| **Physik** | Brustphysik, Stoffsimulation, Piercing-Schwingung |
| **Material** | Material-Farben (skin, clothes, piercings, tattoos) |
| **Presets** | Preset-Liste, laden, Screenshot speichern |
| **Rigging** | Auto-Rig, Metahuman-Export |
| **Export** | Preset speichern, FBX exportieren, Unreal-Export |
| **Einstellungen** | Blender-Pfad, Theme (Dark/Light/Cyberpunk), NSFW, Controller |
| **KI** | Text-/Bildbasierte Erzeugung (Phase 2: Stub) |

**F12** – Debug-Konsole ein-/ausblenden

## Blender-Pfad

In **Einstellungen** setzen: `C:\Program Files\Blender Foundation\Blender 4.0\blender.exe`

## Assets

- `assets/characters/male_base.glb`, `female_base.glb` – Basis-Modelle
- `assets/view_preview/` – Anatomie-Vorschau (HelixToolkit unterstützt kein GLB)
