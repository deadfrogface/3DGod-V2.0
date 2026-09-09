# PHASE 26 Report

- Phase: 26 – TripoSR Worker
- Status: **GATED_NOT_INSTALLED** (Image→3D-Generierung) + **PASS** (Probe, Attach, Persistenz)

## Was wirklich existiert (PASS)

- `IImageTo3DService` / `ImageTo3DService`
- Probe `triposr` über `GatedWorkerCatalog` + Hardware – nie Available
- Bestehendes verifiziertes GLB kann ins Projekt und per `.3dgod` Save/Reload
- `Generate_WithoutRuntime_ThrowsAndWritesNoGlb`

## GATED_NOT_INSTALLED

- Kein TripoSR-Checkpoint/Runtime
- Image→3D→Viewport aus einem Foto ist **nicht** erfüllt
- UI meldet Probe-Status, kein Fake-PASS

## Gates

- `ImageTo3DTests`, `FeatureAvailabilityTests.GenerativeBackends_*`
