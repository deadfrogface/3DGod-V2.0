# Stage 13 — SF3D / SPAR3D re-evaluation (post TripoSR PASS_REAL)

**Decision: KEEP REJECTED_WITH_EVIDENCE for product install this cycle.**

## Evidence after Stage 12 TripoSR PASS_REAL

Local TripoSR CPU path produced a real GLB (`chair.png` → ~97KB, 2454 verts, finite bounds).
That establishes a working Image→3D baseline without Stability Community License UX.

| Criterion | TripoSR (now PASS_REAL) | SF3D | SPAR3D |
|-----------|-------------------------|------|--------|
| Commercial license clarity | MIT | Stability Community (accept + attribution) | Stability Community |
| Real product inference in-tree | Yes (CPU SUPPORTED_BUT_SLOW) | No worker | No worker |
| Extra VRAM / CUDA requirement | Optional | Strongly preferred ≥8GB | ≥6–12GB profiles |
| Setup / maintenance cost | One WRAP worker | Second stack + license gate UX | Second stack |
| Measured quality win vs TripoSR | Baseline | Not measured on target HW | Not measured |

## Why still reject

1. No side-by-side quality/VRAM benchmark shows a product win worth a second Image→3D backend.
2. Stability Community License is still not cleared for default Setup Assistant install.
3. Adding SF3D/SPAR3D now would duplicate packaging/ops without replacing TripoSR.

## What remains

- `CanInstall=false` for `sf3d` / `spar3d` in Setup Assistant (`InstallRejectedIds`)
- Packaging manifests with null download URLs
- Honest probes only — no fake meshes

## Revisit when

Signed commercial clearance **and** pinned release ZIP+SHA **and** measured win over TripoSR on target hardware.
