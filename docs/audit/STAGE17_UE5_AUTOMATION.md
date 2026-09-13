# Stage 17 — Unreal Engine 5 real editor import

**Status: IMPLEMENTED_GATED_UE5_RUNTIME**

## Kept

- UE5 preflight (`UnrealEngine5ExportProfile`)
- GLB export + Blender headless FBX fallback
- Skeleton profiles

## Automation entrypoints

| Path | Role |
|------|------|
| `scripts/ue5/Invoke-Ue5ImportSmoke.ps1` | Host-side gate + UnrealEditor-Cmd launcher |
| `scripts/ue5/python/ue5_import_asset.py` | In-editor Python Interchange/AssetTools import |

### Exit codes (`Invoke-Ue5ImportSmoke.ps1`)

| Code | Meaning |
|------|---------|
| 0 | **PASS_REAL** — log contains `3DGOD_UE5_IMPORT_OK` (optional `3DGOD_UE5_SKELETON_OK` / `MATERIALS_OK` / `MORPH_OK` / `LOD_OK`) |
| 2 | **GATED_UE5_RUNTIME** — `UE_ROOT` missing / editor / project missing |
| 3 | UE ran but log file missing |
| 4 | UE ran without required success markers |

## Classification rules

| Condition | Status |
|-----------|--------|
| Script + preflight exist, UE not installed | **IMPLEMENTED_GATED_UE5_RUNTIME** |
| UE installed and import log proves asset (+ optional skeleton/materials) | **PASS_REAL** |
| FBX header-only checks | Never sufficient for PASS_REAL |

## CI

Stock GitHub-hosted runners do not ship Unreal → gate remains honest skip `GATED_UE5`.
Without `UE_ROOT`, the smoke script exits **2** and does not claim PASS_REAL.
