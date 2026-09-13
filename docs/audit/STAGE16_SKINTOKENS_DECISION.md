# Stage 16 — SkinTokens re-audit

**Status: IMPLEMENTED_GATED_HARDWARE (license cleared; NotInstalled until CUDA + checkpoints + upstream)**

## Upstream

- Code: https://github.com/VAST-AI-Research/SkinTokens — **MIT**
- Weights: https://huggingface.co/VAST-AI/SkinTokens — **MIT**
- Pinned revision: `79736cad0fd84de384d5eede659b4ebd24effe33`
- Checkpoints (both required):
  - `experiments/articulation_xl_quantization_256_token_4/grpo_1400.ckpt`
  - `experiments/skin_vae_2_10_32768/last.ckpt`
- Min VRAM: **14000 MB**, requires CUDA

## Classification

| Layer | Status |
|-------|--------|
| License (code + published HF weights) | **Cleared (MIT)** — `MODEL_LICENSES.json` verified |
| Product install / Setup Assistant | Optional download when `workers/skintokens` packaged (`LocalSourceHint`) |
| Runtime PASS_REAL | **IMPLEMENTED_GATED_HARDWARE** until CUDA≥14GB + acquire + upstream inference writes a real skinned GLB |
| Fake outputs | **Forbidden** — worker and `SkinTokensRigService` never write a placeholder rigged GLB |

## Worker

`workers/skintokens/skintokens_worker.py` (`3dgod-worker/1`):

- `mesh.autoroot.probe` / `skintokens.probe`
- `mesh.autoroot.acquire` / `skintokens.acquire` → `huggingface_hub.snapshot_download` at pinned revision
- `mesh.autoroot.rig` / `skintokens.rig` → NotInstalled / UnsupportedHardware / real upstream only

Upstream must live under `workers/skintokens/upstream` or be an importable `skintokens` package.

## Residual note

Training-data mix (ArticulationXL / VRoid Hub / ModelsResource) is provenance context only; published MIT weights remain redistributable. Authored humanoid / freeform distance skinning must not be labeled SkinTokens.
