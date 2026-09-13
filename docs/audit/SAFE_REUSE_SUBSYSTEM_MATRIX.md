# Safe Reuse Subsystem Matrix

Status key: WORKING WELL | WORKING BUT FRAGILE | PARTIALLY COMPLETE | BROKEN | MISSING | DUPLICATED | UNNECESSARY CUSTOM

Decision key: KEEP | ADAPT | WRAP | REPLACE | DELETE | CUSTOM

| Subsystem | Status | Decision | Notes |
|-----------|--------|----------|-------|
| `.3dgod` save/load + autosave + SHA-256 | WORKING WELL | KEEP | `GodProjectArchive` |
| Undo/Redo `CommandStack` | WORKING WELL | KEEP | |
| Diagnostics + `CursorReportBuilder` Copy Details | WORKING WELL | KEEP | Problems panel wired |
| Feature gates / `HardwareProfiler` | WORKING WELL | KEEP | |
| GLB export/import (SharpGLTF) | WORKING WELL | KEEP | |
| Remesh vertex-cluster + `MeshValidator` | WORKING BUT FRAGILE | WRAP | `IMeshProcessor`; evaluate `geometry3Sharp` `Reducer` as optional backend |
| Spherical UV | PARTIALLY COMPLETE | WRAP | `IUvUnwrapper`; xatlas rejected for packaging cost (see research note) |
| `SemanticBoneMap` + UE5 profiles + LBS poses | WORKING WELL | KEEP | |
| Humanoid test rig / authored weights | WORKING WELL | KEEP | |
| Freeform creature rig (1 bone / vertex) | WORKING BUT FRAGILE | ADAPT | Distance-based multi-bone weights |
| Auto-rig / SkinTokens inference | MISSING / GATED | WRAP | `IAutoRigBackend` NotInstalled stub |
| Attachments (semantic + jewelry + horn/tail) | PARTIALLY COMPLETE | ADAPT | Shared socket map; no per-accessory pipelines |
| Garment templates + proximity fit | WORKING / Experimental | KEEP | geometry3Sharp already used for fit |
| Generative garment / local AI mesh edit | MISSING (intentional) | KEEP NotImplemented | Research docs |
| Materials PBR presets | WORKING WELL | KEEP | Semantic AI ops for metal/roughness |
| Anatomical height morph | BROKEN / NotImplemented | CUSTOM later | No fake uniform scale |
| Helix viewport + selection | WORKING WELL | KEEP | |
| Dual Legacy Core + V3 Domain | DUPLICATED | ADAPT | Keep `CharacterModelServiceAdapter`; no big-bang UI rewrite |
| Assimp FBX import stub | UNNECESSARY CUSTOM STUB | KEEP gated | Honest NotInstalled |
| Preview/overlay PNG placeholders | BROKEN | REPLACE | Valid minimal PNGs |
| README feature table | BROKEN (stale) | REPLACE | Point at audit |
| `MinimumAppVersion` 3.0.0 vs product 2.0.0 | BROKEN | REPLACE | Align to 2.0.0 |
| Empty `IImportService` / `IExportService` / `IRiggingService` | UNNECESSARY | DELETE/REPLACE | Replace with gated capability contracts |

## External candidates

| Need | Candidate | Verdict |
|------|-----------|---------|
| Better decimation | geometry3Sharp Reducer (already referenced) | EVALUATE as optional backend `geometry3sharp-qem` |
| meshoptimizer native | ifb-lib / meshoptimizer | REJECT for now (native complexity) |
| UV atlas | xatlas / xatlas.NET | REJECT for default path (native ship cost); spherical remains default |
| Auto-rig | UniRig / SkinTokens.cpp | WRAP gated worker only |
| Skin weights | Mesh2Motion distance solvers | ADAPT heuristic into freeform rig |
| UnityMeshSimplifier | — | REJECT (Unity-coupled) |
| Pinocchio | — | REJECT for core (LGPL / C++) |


## FINAL DECISIONS (implemented)

- Remesh default: **KEEP** `vertex-cluster`; optional **WRAP** `geometry3sharp-qem` via `IMeshProcessor` / `THREEDGOD_MESH_PROCESSOR`.
- UV: **KEEP** spherical; xatlas deferred (`docs/research/XATLAS_UV_DECISION.md`).
- Freeform skinning: **ADAPT** distance multi-influence weights.
- Auto-rig: **WRAP** `IAutoRigBackend` NotInstalled.
- Attachments: **ADAPT** `AttachmentSockets` shared binder.
- Preview PNGs: **REPLACE** with valid fictional assets.
- Manifest `MinimumAppVersion`: **2.0.0**.
- Empty import/export/rigging markers: **REPLACE** with Probe() gates registered in DI.
