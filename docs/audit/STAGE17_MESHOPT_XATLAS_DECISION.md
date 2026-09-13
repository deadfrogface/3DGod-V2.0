# Stage 17 — meshoptimizer / xatlas decision

**Decision: KEEP current in-process remesh + spherical UVs. Do not ship native meshoptimizer or xatlas this cycle.**

## Evidence already on tree

- Remesh: vertex-cluster (meshoptimizer-sloppy-inspired) + optional `geometry3sharp-qem` behind env/DI — see `docs/research/GEOMETRY3SHARP_REMESH_DECISION.md`
- UVs: spherical default; xatlas **REJECT** — see `docs/research/XATLAS_UV_DECISION.md`

## Why not integrate natives now

1. No measured deficit that fails product remesh/UV gates on CI corpora.
2. Native RID packaging (win-x64 + Linux agents) exceeds benefit.
3. Optional QEM already available without new native deps.

## Honesty tests

Core tests assert no `PackageReference` to `meshoptimizer` / `xatlas` / `xatlas.NET` in shipped projects.
