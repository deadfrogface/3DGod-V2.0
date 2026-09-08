# PHASE 07 Report

- Phase: 07 – Autosave und Recovery
- Status: **PASS**

## Was implementiert/geändert wurde

- `AutosaveService` schreibt nur in einen separaten Recovery-Ordner, nie still auf die Hauptdatei.
- Dirty-State, Debounce (`ScheduleAutosaveAsync`) und explizites `FlushAsync`.
- Startup-Recovery: `ListRecoveries`, `RestoreAsync`, `Discard`.
- Crash-Simulation: Autosave, neuer Service auf demselben Ordner, Recovery ist ladbar; Hauptdatei unverändert.

## Geänderte Dateien

- `src/ThreeDGod.Persistence/AutosaveService.cs`
- `src/ThreeDGod.Infrastructure/ThreeDGodComposition.cs`
- `3DGodCreator.Core.Tests/AutosaveServiceTests.cs`
- `docs/audit/PHASE_07_REPORT.md`

## Build-Ergebnis

`dotnet build -c Release`: **0 Fehler, 0 Warnungen**.

## Test-Ergebnisse

`dotnet test -c Release`: **69 bestanden, 0 fehlgeschlagen**.

## Acceptance Criteria

- [x] Recovery-Ordner, Hauptdatei nicht still überschrieben
- [x] Dirty + Debounce
- [x] Simulierter Crash nach Autosave: Recovery erkannt und ladbar
- [x] Restore / Discard
