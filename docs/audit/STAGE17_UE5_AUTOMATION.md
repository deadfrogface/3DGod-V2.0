# Stage 17 — Unreal Engine 5 real editor import

**Status: IMPLEMENTED_GATED_UE5_RUNTIME**

## Kept

- UE5 preflight (`UnrealEngine5ExportProfile`)
- GLB export + Blender headless FBX fallback
- Skeleton profiles

## Added automation entrypoint

`scripts/ue5/Invoke-Ue5ImportSmoke.ps1`

Runs only when `UE_ROOT` (or `-UeRoot`) points at a real Unreal Engine install.
Uses Unreal Editor command-line / Python automation hooks when present.

## Classification rules

| Condition | Status |
|-----------|--------|
| Script + preflight exist, UE not installed | **IMPLEMENTED_GATED_UE5_RUNTIME** |
| UE installed and import log proves asset + skeleton | **PASS_REAL** |
| FBX header-only checks | Never sufficient for PASS_REAL |

## CI

Stock GitHub-hosted runners do not ship Unreal → gate remains honest skip `GATED_UE5`.
