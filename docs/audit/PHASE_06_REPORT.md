# PHASE 06 Report

- Phase: 06 – `.3dgod` Save/Load
- Status: **PASS**

## Was implementiert/geändert wurde

- `GodProjectArchive` speichert/lädt `ProjectBundle` als ZIP (ZIP64-fähig über `ZipArchive`) + System.Text.Json.
- `manifest.json` mit Format `3dgod`, FormatVersion, ProjectId, relativen Pfaden und SHA256.
- Atomic Save: Temp-Datei auf demselben Volume, Validierung, dann Move/`File.Replace`; vorhandene Datei wird nach `.bak` kopiert.
- `IProjectMigration` mit FromVersion/ToVersion.
- Security: keine `..`-Segmente, keine absoluten Pfade, Limits für Dateianzahl/Entry-Größe/dekomprimierte Gesamtgröße, malformed JSON und fehlendes Manifest werden abgewiesen. Keine Executables im Archiv.

## Geänderte Dateien

- `src/ThreeDGod.Core/Domain/ProjectBundle.cs`
- `src/ThreeDGod.Persistence/GodProjectArchive.cs`, `GodProjectManifest.cs`, `ArchivePathRules.cs`
- `src/ThreeDGod.Application/CapabilityInterfaces.cs` (`IProjectService` Save/Load)
- DI: `ThreeDGodComposition` registriert `IProjectService` → `GodProjectArchive`
- Tests: `GodProjectArchiveTests.cs`, Composition-Anpassungen

## Build-Ergebnis

`dotnet build -c Release`: **0 Fehler, 0 Warnungen**.

## Test-Ergebnisse

`dotnet test -c Release`: **66 bestanden, 0 fehlgeschlagen**.

## Acceptance Criteria

- [x] Save → close → load, Domain-Identität gleich
- [x] ZipSlip / absolute Pfade rejected
- [x] Oversize, zu viele Files, malformed JSON, missing manifest rejected
- [x] Atomic save + backup
- [x] Migration-Interface vorhanden
