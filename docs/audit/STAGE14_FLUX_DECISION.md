# Stage 14 — FLUX.1-schnell

**Status: IMPLEMENTED_GATED_HARDWARE** (not PASS_REAL on CPU-only agents)

## License (re-checked)

- Weights: **FLUX.1-schnell** — Apache-2.0 (Black Forest Labs)
- HF repo: `black-forest-labs/FLUX.1-schnell`
- Pinned revision: `741f7c3ce8b383c54771c7003378a50191e9efe9`
- HF `gated:auto` = one-time ToS accept; **not** a commercial-use block
- Non-schnell FLUX variants remain **forbidden**

## Implementation

- Isolated worker: `workers/flux/` (`flux_worker.py`, `uv.lock`, manifests)
- ComponentManager install path via `localSourceHint: workers/flux`
- Model acquire: `text.toimage.acquire` / HF snapshot at pinned revision
- Disk preflight ≥40GB; VRAM preflight ≥8192MB CUDA
- Progress + cancel flag; PNG size/dimension validation
- Deterministic seed supported
- C#: `FluxService` + `ReferenceImageService.GenerateAsync` calls real worker (never fakes PNG)
- Optional E2E (gated): FLUX PNG → TripoSR GLB when both CUDA+weights present

## Classification

| Environment | Status |
|-------------|--------|
| No CUDA / no weights | **IMPLEMENTED_GATED_HARDWARE** |
| CUDA + weights + real PNG | **PASS_REAL** (required proof) |

Do **not** use vague GATED_MODEL when only hardware/weights install is missing — code path is complete.
