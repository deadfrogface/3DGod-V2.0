# GarmentCode worker (PHASE 42)

Isolated uv environment for [pygarment](https://github.com/maria-korosteleva/GarmentCode) (MIT core).

## What this worker does

- `garment.probe` — import `Panel` / `Edge` / `BasicPattern`
- `garment.jacket` — four-panel parametric jacket → pattern JSON + OBJ (cm→m)

This is **pattern import**, not cloth simulation.

## What this worker does not do

- Warp drape / NVIDIA Warp (forked; not bundled)
- CGAL / libigl mesh generation as a product feature
- Cairo SVG visualization (`cairosvg` needs a system Cairo DLL; skipped)

## Licenses (not a release bundle)

| Package | Role | License note |
|---|---|---|
| pygarment 2.0.x | pattern DSL | MIT |
| numpy / scipy | numerics | BSD |
| cgal / libigl | transitive PyPI deps of pygarment | **not release-bundled**; incomplete for product shipping |
| Warp | simulation | **not installed / not bundled** |

`.venv/` is gitignored. Pin with `uv.lock`. Do not copy this environment into an installer until the dependency license list is complete.
