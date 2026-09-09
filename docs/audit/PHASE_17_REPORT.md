# PHASE 17 Report

- Phase: 17 – Viewport Upgrade
- Status: **PASS** (audit repair)
- Date: 2026-09-09

## Build Book DONE

- Helix rotate/pan/zoom unchanged
- Linksklick hit-test selects mesh and maps render visual → `DomainObjectId`
- DomainObjectId shown in viewport inspector (`SelectionInfo`)
- Skeleton overlay only when model is actually rigged
- `ViewportSelectionService` registered in DI and used by `MainWindow` / `HelixViewportSession`

## Build / tests

- `dotnet build -c Release`: 0 errors, 0 warnings
- ProductPhase + Composition viewport tests: see PHASE_17A
