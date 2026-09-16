# Auto-Rig Provider Evaluation

## Decision summary

| Provider | Classification | Action |
|----------|----------------|--------|
| **skin-tokens.cpp** (localai-org) | **ADAPT / WRAP** | Product CPU + Vulkan Auto-Rig provider |
| **Official SkinTokens** (VAST-AI) | **KEEP / WRAP** | Optional NVIDIA CUDA provider |
| UniRig (vltrx/UniRig) | **REJECTED_WITH_EVIDENCE** | No product win vs skin-tokens.cpp |
| RigAnything | **REJECTED_WITH_EVIDENCE** | License = Other/NOASSERTION; research stack |
| Make-It-Animatable | **REJECTED_WITH_EVIDENCE** | Python research; overlaps SkinTokens scope |
| Pinocchio (pmolodo) | **REJECTED_WITH_EVIDENCE** | LGPL-heritage geometric auto-rig; no license file on fork; superseded by learned weights |

---

## skin-tokens.cpp — KEEP path

| Field | Evidence |
|-------|----------|
| Repo | https://github.com/localai-org/skin-tokens.cpp |
| Pin (main tip at eval) | `43e885af2eadee9c40aa85849b71528d1c958293` |
| Code license | **Apache-2.0** (GitHub SPDX) |
| Upstream SkinTokens | MIT code+weights (VAST-AI); NOTICE retained |
| Models | Hugging Face `LocalAI-io/SkinTokens-GGUF` F16 (recommended) |
| Devices | `--device cpu` / `vulkan` / `auto` |
| Windows | CMake C++23; Vulkan optional `-DSKINTOKENS_ENABLE_VULKAN=OFF` |
| Product integration | Isolated CLI `skintokens-cli rig MODEL in.glb out.glb --device …` via `SkinTokensCppRuntime` |
| Redistribution | Apache-2.0 binary + MIT GGUF with attribution; not in base installer (on-demand) |

**Vulkan classification:** `IMPLEMENTED_GATED_VULKAN_RUNTIME_PROOF` until a runner executes Vulkan inference without CPU fallback claiming Vulkan.

**CPU classification:** `IMPLEMENTED_GATED_RUNTIME` until CLI+GGUF present on the machine; then `PASS_REAL` via integration test.

---

## Official SkinTokens CUDA — KEEP

Existing `SkinTokensRigService` / `workers/skintokens` (MIT). Optional when CUDA ≥14GB + checkpoints. Wrapped as `SkinTokensCudaAutoRigProvider`. Must not be required for AMD/CPU users.

---

## Rejected dependencies

### UniRig (`vltrx/UniRig`)
- License: MIT
- Stars: 0; sparse product docs
- Python stack overlapping SkinTokens-class capability
- **Reject:** no incremental vendor-neutral value beyond skin-tokens.cpp CPU/Vulkan already chosen

### RigAnything (`Isabella98Liu/RigAnything`)
- License: **Other / NOASSERTION** — redistribution unclear for commercial product
- Research SIGGRAPH TOG 2025 Python stack
- **Reject:** license + dependency bloat

### Make-It-Animatable (`jasongzy/Make-It-Animatable`)
- License: MIT
- Large Python research codebase; GPU-oriented authoring pipeline
- **Reject:** duplicates SkinTokens/skin-tokens.cpp product scope; heavy install surface

### Pinocchio (`pmolodo/Pinocchio`)
- Classic geometric auto-rig; **no SPDX license** on this fork (upstream historically LGPL-adjacent)
- Last push 2015; skin-tokens.cpp already references Pinocchio only as IK inspiration, not as dependency
- **Reject:** license ambiguity + obsolete vs learned GGUF path

---

## Product selector policy

Automatic order: **Vulkan (cpp) → CPU (cpp) → NVIDIA CUDA (official)**.

Never claim Vulkan PASS while executing CPU. Preference Vulkan with unavailable Vulkan provider throws (no silent fallback).
