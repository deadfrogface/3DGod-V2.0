# Stage 14 — FLUX.1-schnell

**Status: IMPLEMENTED_GATED_HARDWARE / GATED_MODEL (weights optional download)**

## License

- Target weights: **FLUX.1-schnell** — Apache-2.0 (Black Forest Labs)
- Non-schnell FLUX variants remain **forbidden** (non-commercial / unsuitable)

## Implementation posture

- Packaging manifest `docs/packaging/workers/flux.manifest.json` (Apache-2.0, NotInstalled until checkpoint present)
- `ReferenceImageService` / `ReferenceImageRuntime` probe LocalAppData checkpoints honestly
- No fake PNG generation when checkpoint/CUDA missing
- Optional local worker path may be added under `workers/flux` once a pinned safetensors + SHA is acquired

## Hardware

FLUX.1-schnell realistically needs a CUDA GPU with substantial VRAM for interactive use.
On CPU-only agents: **IMPLEMENTED_GATED_HARDWARE** (do not claim PASS_REAL without a generated image).

## Required for PASS_REAL

1. Pinned Apache-2.0 schnell safetensors + SHA-256
2. Disk + VRAM preflight
3. Real prompt → PNG bytes validated
4. Optional chain: FLUX image → TripoSR → GLB
