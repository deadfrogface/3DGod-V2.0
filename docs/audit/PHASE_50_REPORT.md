# PHASE_50 Report

- Phase: 50 – FBX / UE5 Backend
- Status: **PASS** (implementation complete; live export **GATED_NOT_INSTALLED** when Blender missing)

## Summary

Robust export path: **Viewport/Character GLB → headless Blender (`export_fbx.py`) → FBX** with post-export sanity checks. Assimp FBX import remains **NotInstalled**. No claim of real UE5 editor import success; manual UE import stays out of scope.

## Delivered

| Item | Result |
|------|--------|
| `export_fbx.py` GLB import + UE-friendly FBX export | PASS |
| `FbxExportService` + `FbxSanity` (size, Kaydara header) | PASS |
| `LegacyBlenderBackend.TryExportGlbToFbx` (sync, CreateNoWindow) | PASS |
| ExportPanel / CharacterSystem prefer viewport GLB | PASS |
| PipelineTrace `Export.Preflight` / `Export.Write` breadcrumbs | PASS |
| Optional UE5 GLB preflight before FBX job | PASS |
| Tests: gated NotInstalled, live export when Blender present, header unit test | PASS |
| UE5 editor import claimed | **Never** |

## Runtime gates

- **Blender installed:** GLB→FBX export runs headless; FBX file existence + sanity PASS.
- **Blender missing:** `GATED_NOT_INSTALLED` – no fake FBX, tests use `TestGate.NotInstalled`.

## Build / tests

Run: `dotnet build -c Release` and `dotnet test --filter "FullyQualifiedName~FbxExport|FullyQualifiedName~FbxSanity|FullyQualifiedName~UnrealEngine5|FullyQualifiedName~GlbExport|FullyQualifiedName~LegacyBlender"`.

Release build: **0 errors**. Full suite: **237 passed**. Filtered export/FBX/Blender tests: **21 passed**.

Blender on build machine: **not installed** (live GLB→FBX tests use `TestGate.NotInstalled`; `GATED_NOT_INSTALLED` path verified).
