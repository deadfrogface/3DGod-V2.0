# PHASE 43 Report

- Phase: 43 – Garment Fitting
- Status: **PASS** (technischer Fit; keine Cloth-Simulation)

## Was wirklich existiert

- `geometry3Sharp` 1.0.324 (Boost) in `ThreeDGod.Mesh`: `DMeshAABBTree3`, `IsInside`, `TriangleDistance`.
- Body measurements aus Mesh (Höhe, Chest/Waist/Shoulder-Span, Torso-Center).
- Jacke kommt von **pygarment** (PHASE 42), skaliert nach Maßen, initial auf den Torso gesetzt.
- Clipping: Garment-Vertices **inside** Body → Projektion auf die Oberfläche + Inflate (bis 4 Pässe).
- JSON-Clipping-Report (`InsideBefore`/`InsideAfter`, Regionen legs/torso/head).
- Drei Anny-Human-Presets `adult-average`, `tall-slim`, `muscular-male`: jeweils Body-GLB + fitted Jacket + Report, `InsideAfter == 0`. `tall-slim` ist messbar höher als `adult-average`.
- Feature-Gate `clothing.fit` = **Experimental** wenn GarmentCode da ist. Keine Physik-Sim.

## Nicht vorhanden

- Keine Cloth-/Warp-Simulation, kein Skinning (PHASE 44), kein „perfekt sitzendes“ Kleidungsstück.

## Gates

- `dotnet build -c Release`: 0 Fehler, 0 Warnungen
- `dotnet test -c Release` (ohne `AnnyGenerate`): 190 bestanden, inkl. `GarmentFitTests` / `GarmentFitPresetTests`
