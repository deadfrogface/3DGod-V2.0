# PHASE 01 Report

- Phase: 01 – .NET 8 auf .NET 10 LTS
- Status: PASS
- Datum: 2026-09-08

## Was implementiert/geändert wurde

- `global.json` pinnt SDK `10.0.102` (`rollForward: latestFeature`)
- App: `net10.0-windows`
- Core + Tests: `net10.0` (kein WPF in Core, gemäß Technical Spec)
- `System.Text.Json` PackageReference entfernt (Framework-provided)
- HelixToolkit.Wpf **2.25.0 → 3.1.2** (net8.0-windows/net10.0-windows kompatibel; kein SharpDX, das bleibt PHASE 17)
- SharpGLTF.Toolkit 1.0.6 beibehalten
- Nullable-Warnung CS8625: `PanGesture2 = MouseAction.None` statt `null` (Mausrad bleibt Zoom)
- README-Anforderungen auf .NET 10; Hinweis auf Feature-Audit
- `ProjectReadinessService.CheckDotNet` zielt auf .NET 10
- Kein UI-Komplettumbau, keine Featureänderung

## Geänderte Dateien

- `global.json` (neu)
- `3DGodCreator.App/3DGodCreator.App.csproj`
- `3DGodCreator.App/MainWindow.xaml.cs`
- `3DGodCreator.Core/3DGodCreator.Core.csproj`
- `3DGodCreator.Core/Services/ProjectReadinessService.cs`
- `3DGodCreator.Core.Tests/3DGodCreator.Core.Tests.csproj`
- `README.md`
- `docs/audit/PHASE_01_REPORT.md` (neu)

## Build-Ergebnis

`dotnet restore` OK  
`dotnet build -c Release`: 0 Fehler, 0 Warnungen (NU1701 entfallen)

## Test-Ergebnisse

21/21 bestanden, Testhost `net10.0`.

App-Start-Smoke: `3DGodCreator.App.exe` lief unter .NET 10.0.102, Readiness loggte `.NET SDK OK (Version 10.0.102)` und `NuGet packages OK (HelixToolkit.Wpf, SharpGLTF.Toolkit)`. Prozess danach beendet.

## Acceptance-Criteria-Ergebnis

| Kriterium | Ergebnis |
|-----------|----------|
| global.json / SDK .NET 10 | PASS |
| Projekte auf .NET 10 | PASS |
| kompatible NuGets | PASS |
| nullable sauber (Release 0 Warnungen) | PASS |
| README Requirements | PASS |
| keine Featureänderung / kein UI-Komplettumbau | PASS |
| App startet, bestehender GLB-Testimport | PASS |

## Neue/aktualisierte Dependency

- Name: HelixToolkit.Wpf
- Version: 3.1.2
- URL: https://github.com/helix-toolkit/helix-toolkit
- Lizenz: MIT
- Zweck: bestehender WPF-Viewport (nicht SharpDX)

## Commit

`phase-01: migrate solution to .NET 10 LTS`
