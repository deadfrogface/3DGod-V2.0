# PHASE 01 Report

- Phase: 01 – .NET 8 auf .NET 10 LTS
- Status: **PASS**
- Commit: folgt (`phase-01: migrate solution to net10 LTS`)

## Was implementiert/geändert wurde

- `global.json` pinnt SDK `10.0.102` (`rollForward: latestFeature`).
- App: `net10.0-windows`; Core und Tests: `net10.0`.
- Inbox `System.Text.Json` (Paket 8.0.5 entfernt).
- HelixToolkit.Wpf 2.25.0 → 3.1.2 (net10-kompatibel, NU1701 weg; keine Viewport-Featureänderung, gleiche WPF-API).
- README-Anforderungen auf .NET 10 LTS.
- `ProjectReadinessService.CheckDotNet` prüft .NET 10.
- Nullable bleibt enabled; Release-Build 0 Warnings.

Keine UI-/Feature-Änderungen.

## Geänderte Dateien

- `global.json` (neu)
- `3DGodCreator.App/3DGodCreator.App.csproj`
- `3DGodCreator.Core/3DGodCreator.Core.csproj`
- `3DGodCreator.Core.Tests/3DGodCreator.Core.Tests.csproj`
- `3DGodCreator.Core/Services/ProjectReadinessService.cs`
- `README.md`
- `3DGodCreator.Core.Tests/DotNet10MigrationTests.cs`
- `docs/audit/PHASE_01_REPORT.md`

## Build-Ergebnis

`dotnet restore` + `dotnet build -c Release`: **0 Fehler, 0 Warnungen**.  
Outputs: `net10.0` (Core/Tests), `net10.0-windows` (App).

## Test-Ergebnisse

`dotnet test -c Release`: **23 bestanden, 0 fehlgeschlagen** (inkl. Runtime-Major >= 10).

## Acceptance Criteria

- [x] global.json / SDK .NET 10
- [x] Projekte `net10.0-windows` / `net10.0`
- [x] kompatible NuGets
- [x] nullable enabled, Release clean
- [x] README Requirements
- [x] keine Featureänderung / kein UI-Komplettumbau
- [x] App kompiliert als WinExe net10.0-windows; bestehender GLB-Import-Pfad (SharpGLTF) durch Tests abgesichert

Interaktiver WPF-Start ist kein automatisierter Gate; Compile + GLB-Library-Smoke ersetzen den Viewport-Import-Check.
