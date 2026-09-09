# PHASE 20 Report

- Phase: 20 – Anny Worker POC
- Status: **PASS** (Worker-Protokoll, Export, Gates) + **GATED_NOT_INSTALLED** (Live-Generate ohne uv/Runtime)

## Was wirklich existiert (PASS)

- Isolierte uv-Umgebung unter `workers/anny` (Python 3.12.14, `anny==0.6.0`, `uv.lock` gepinnt).
- Worker-Protokoll `3dgod-worker/1`: `human.catalog`, `human.generate`, `human.get_rig`, `human.export_glb`.
- Default-Topologie `anny.Anny()` (Apache-2.0 / CC0). **Kein SMPL-X.**
- `human.export_glb` bleibt **NotImplemented** im Python-Worker; GLB schreibt C# (`TriangleMeshExport.ObjToGlb`).
- UI: Datei → Anny-Human, KI-Tab-Button, Projekt öffnen/speichern `.3dgod`, Export GLB.
- Feature-Gate `human.anny` = **Experimental** wenn uv+Lock vorhanden, sonst **NotInstalled**. Kein Fake-Mesh.
- `AnnyProbe_NeverReportsSuccessWithoutRuntime` – Probe meldet nie „success“.

## GATED_NOT_INSTALLED (Runtime)

- `AnnyGenerate_WhenInstalled_WritesRealGlb` nutzt `TestGate.NotInstalled`, wenn uv/Runtime fehlt.
- Smoke **13718 Vertices** gilt nur auf Maschinen mit installiertem Anny-Worker – nicht als universeller PASS.

## Gates

- `dotnet build -c Release`: 0 Fehler
- `dotnet test -c Release`: AnnyRuntimeTests inkl. GATED-Soft-Skip

## Bewusst nicht fertig

- Keine Live-Slider / kein Undo für Phenotypen (PHASE 21)
- Kein GPU-Pfad (kein nvidia-smi / CUDA-Treiber)
- Height-Morph bleibt **NotImplemented** (kein Uniform-Scale)
