# GarmentCode / pygarment — license gate (PHASE 42)

Repo: https://github.com/maria-korosteleva/GarmentCode  
PyPI: `pygarment` (pinned in `workers/garmentcode/uv.lock`)

## Core

- **pygarment** pattern construction (`Panel`, `Edge`, `BasicPattern`): **MIT**
- Used here only as an **isolated worker**, not as a shipped runtime folder

## Transitive / not bundled as product

pygarment's PyPI install also pulls **cgal** and **libigl**. Their licenses are **not** treated as cleared for a 3D God installer.

**NVIDIA Warp** (simulation in upstream GarmentCode) is **not** a dependency of this worker. No draped garment is claimed.

## Product rule

- Experimental worker when `uv` + `uv.lock` + script exist
- No fake GLB when the worker is missing
- No release-bundling of `.venv` until CGAL/libigl (and any sim stack) are explicitly accepted
