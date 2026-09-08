# PHASE 20 Report

- Phase: 20 – Anny Worker POC
- Status: **PASS**

## Was wirklich existiert

- Isolierte uv-Umgebung unter `workers/anny` (Python 3.12.14, `anny==0.6.0`, `uv.lock` gepinnt).
- Worker-Protokoll `3dgod-worker/1`: `human.catalog`, `human.generate`, `human.get_rig`, `human.export_glb`.
- Default-Topologie `anny.Anny()` (Apache-2.0 / CC0). **Kein SMPL-X.**
- Smoke `human.generate`: **13718 Vertices, 27420 Dreiecke**, OBJ auf Platte, danach SharpGLTF-GLB.
- Phenotyp-Keys der Default-Topologie: gender, age, muscle, weight, height, proportions.
- `human.get_rig` liefert echte `bone_labels` (kein Fake-Skeleton-Mesh).
- `human.export_glb` bleibt **NotImplemented** im Python-Worker; GLB schreibt C# (`TriangleMeshExport.ObjToGlb`).
- UI: Datei → Anny-Human, KI-Tab-Button, Projekt öffnen/speichern `.3dgod`, Export GLB.
- Feature-Gate `human.anny` = **Experimental** wenn uv+Lock vorhanden, sonst **NotInstalled**. Kein Fake-Mesh.

## Gates

- `dotnet build -c Release`: 0 Fehler, 0 Warnungen
- `dotnet test -c Release`: 118 bestanden, inkl. `AnnyGenerate_WhenInstalled_WritesRealGlb` (VertexCount > 1000)
- Kein Blender-GUI, kein Terminal-Fenster (`CreateNoWindow`)

## Bewusst nicht fertig

- Keine Live-Slider / kein Undo für Phenotypen (PHASE 21)
- Kein GPU-Pfad (kein nvidia-smi / CUDA-Treiber)
- Height-Morph bleibt **NotImplemented** (kein Uniform-Scale)
- Prompt-Person / Asset-KI bleiben **NotImplemented**
