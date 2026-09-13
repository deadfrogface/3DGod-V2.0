# Stage 11 — GarmentCode dependency prune

**Status: DEPENDENCIES_REQUIRED (transitive via pygarment) with PRUNED direct surface**

## Direct worker dependencies

`workers/garmentcode/pyproject.toml` depends only on:

- `pygarment>=2.0.0` (pinned via `uv.lock`)

No direct NiceGUI / CGAL / libigl / Warp dependency is declared by 3D God.

## Transitive (from pygarment lock)

Observed in `workers/garmentcode/uv.lock` (pygarment 2.0.2 tree):

| Package | Why it appears | Product posture |
|---------|----------------|-----------------|
| `cgal` | pygarment geometry backend | **DEPENDENCIES_REQUIRED** for current pygarment releases |
| `libigl` | pygarment mesh helpers | **DEPENDENCIES_REQUIRED** unless upstream drops it |
| `nicegui` | pygarment optional UI extras / transitive | **Not imported** by `garmentcode_worker.py` (headless) |
| render / matplotlib / cairosvg stack | pattern preview helpers in upstream | Not used by headless worker path |

## Prune result

- **Direct deps:** already minimal (`PRUNED` to pygarment only)
- **Transitive CGAL/libigl:** cannot be removed without forking/replacing pygarment → **DEPENDENCIES_REQUIRED**
- **NiceGUI:** unused by headless worker; remains transitive until upstream optionalizes it → document as unused, do not import

## Verification rule

After any lock change, re-run `CiRuntimeIntegrationTests.GarmentCode_CpuJacket_WritesRealGlb` before claiming PRUNED_AND_VERIFIED for runtime.
