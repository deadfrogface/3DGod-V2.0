# Stage 13 — SF3D / SPAR3D decision

**Decision: REJECT for product install this cycle.**

## Comparison vs TripoSR (Stage 12)

| Criterion | TripoSR | SF3D | SPAR3D |
|-----------|---------|------|--------|
| License clarity for commercial shipping | MIT (clear) | Stability Community (accept + attribution; product risk) | Stability Community |
| Typical VRAM | CPU-capable path documented | ≥8 GB CUDA | ≥6–12 GB CUDA profiles |
| Windows packaging cost | High (checkpoint + worker) | Higher (license UX + CUDA) | Higher |
| Product win over TripoSR | Baseline WRAP candidate | Unclear quality/ops win | Unclear quality/ops win |

## Why reject now

1. No measured product win (quality / runtime / support cost) that justifies a second Image→3D stack.
2. Stability Community License requires accept-flow + attribution; not cleared for default Setup Assistant install.
3. CUDA/VRAM gates would hide the feature on most CI and many end-user machines.

## What remains in tree

- Honest `NotInstalled` / license / hardware probes in `ImageTo3DService`
- Packaging manifests under `docs/packaging/workers/{sf3d,spar3d}.manifest.json` with **no download URL**
- Setup Assistant **CanInstall = false** for `sf3d` / `spar3d`

## Revisit when

- Commercial license path is signed off, **and**
- Side-by-side quality/VRAM benchmarks beat TripoSR on target hardware, **and**
- A pinned release ZIP + SHA exists for ComponentManager.
