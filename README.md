# 3D God Creator V2.0

Local-first character & creature creator for game-ready assets. Native C# / .NET 10 / WPF.

**Authoritative capability status:** [`docs/audit/FINAL_RELEASE_AUDIT.md`](docs/audit/FINAL_RELEASE_AUDIT.md)  
**Safe-reuse matrix:** [`docs/audit/SAFE_REUSE_SUBSYSTEM_MATRIX.md`](docs/audit/SAFE_REUSE_SUBSYSTEM_MATRIX.md)

The historical feature table below is **not** the honesty source — many rows were migration aspirations. Prefer the audit.

## Requirements

- .NET 10 SDK (see `global.json`), Windows x64 for the WPF app
- Optional: Blender (FBX path), Anny / GarmentCode workers, CUDA backends — all gated, never faked

## Build & test

```powershell
dotnet build
dotnet test 3DGodCreator.Core.Tests -c Release
dotnet run --project 3DGodCreator.App
```

## What works in the foundation release

- `.3dgod` project save/load with SHA-256 manifest + autosave recovery
- Undo/redo command stack
- Diagnostics with **Copy details** Cursor report
- Feature gates + hardware probe (honest NotInstalled / UnsupportedHardware)
- GLB export/import (SharpGLTF), remesh (vertex-cluster default; optional geometry3Sharp QEM backend)
- Semantic bone map, UE5 profile preflight, LBS pose validation
- Freeform creature distance skinning, procedural attachments (shared sockets)
- Deterministic AI command allow-list (orchestration only)

## Explicitly gated / not claimed

- Live Anny / GarmentCode / CUDA generative backends
- Auto-rig neural workers (UniRig / SkinTokens) — NotInstalled stub behind `IAutoRigBackend`
- Assimp FBX/DAE import, real UE5 editor import
- Generative garment AI / local AI mesh edit (see `docs/research/`)

## License notices

See `THIRD_PARTY_NOTICES.txt` and `MODEL_LICENSES.json`.
