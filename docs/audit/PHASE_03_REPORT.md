# PHASE 03 Report

- Phase: 03 – Legacy Blender entkoppeln
- Status: **PASS**

## Was implementiert/geändert wurde

- `BlenderService` ersetzt durch `LegacyBlenderBackend` in Infrastructure.
- Alle Prozessstarts headless: `--background`, `CreateNoWindow = true`, kein sichtbares Blender-Fenster.
- CharacterSystem spricht nur `IBlenderOperations` an, keine konkrete Runtime-Klasse.
- UI: kein MessageBox „Blender fehlt“ mehr; Settings-Texte „Legacy-Runtime“; Sculpt sendet headless Job.
- Fehlende Runtime = unavailable, App/DI bleiben stabil.
- Headless-Smoke über `blender_runtime_test.py` wenn Runtime installiert ist.

## Geänderte Dateien

- `src/ThreeDGod.Infrastructure/LegacyBlenderBackend.cs`
- `src/ThreeDGod.Infrastructure/ThreeDGodComposition.cs`
- App Settings/Sculpt/MainWindow
- `3DGodCreator.Core.Tests/LegacyBlenderBackendTests.cs` und Composition-Anpassungen

## Build-Ergebnis

`dotnet build -c Release`: **grün**.

## Test-Ergebnisse

`dotnet test -c Release`: **33 bestanden**.

## Acceptance Criteria

- [x] LegacyBlenderBackend hinter Capability-Interface
- [x] Character Core ruft keine Blender-Klasse direkt auf
- [x] nur externe Prozesse, kein sichtbares Fenster
- [x] Runtime fehlt: unavailable, App stabil
- [x] Runtime da: headless Job-Pfad existiert (Smoke)
