# Stage 16 — SkinTokens decision

**Decision: DO NOT INTEGRATE for product install this cycle.**

## Gates that remain open

| Gate | Reason |
|------|--------|
| GATED_LICENSE | `pending-review` — commercial/provenance not cleared in `MODEL_LICENSES.json` |
| GATED_HARDWARE | Documented need for NVIDIA CUDA and high VRAM (≥8–14 GB class) |
| GATED_MODEL | No verified checkpoint hash / release ZIP for ComponentManager |

## What remains in tree

- `SkinTokensRigService` probes honestly (`NotInstalled` / `UnsupportedHardware`) and never writes a fake GLB
- Packaging manifest `docs/packaging/workers/skintokens.manifest.json` with null download URL
- Setup Assistant Automatic Rigging → **CanInstall = false**

Authored humanoid / freeform distance skinning paths stay available separately and are not claimed as SkinTokens.
