# Stage 16 — SkinTokens re-audit

**Status: GATED_LICENSE (+ GATED_HARDWARE) — DO NOT INTEGRATE for commercial product install**

## Upstream

- Repo: https://github.com/VAST-AI-Research/SkinTokens
- Code license: typically Apache-2.0 for research code (verify current LICENSE on tip)
- **Weights / checkpoints**: provenance and redistribution for commercial desktop products remain **uncleared** in `MODEL_LICENSES.json` (`pending-review`)

## Exact product blocker

Until a written commercial-use / redistribution clearance exists for the **checkpoint weights** (not only the training code), Setup Assistant must keep `CanInstall=false`.

Classification:

- Backend probe/stub code: present (`SkinTokensRigService`) — never writes a fake rigged GLB
- Product install: **GATED_LICENSE**
- Typical runtime: CUDA + high VRAM → also **GATED_HARDWARE** when a cleared checkpoint appears

## Not claimed

Authored humanoid / freeform distance skinning is separate and must not be labeled SkinTokens.
