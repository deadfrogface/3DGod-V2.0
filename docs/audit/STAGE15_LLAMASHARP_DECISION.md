# Stage 15 — LLamaSharp free-form → AiEditPlan

**Status: PASS_REAL (local CPU GGUF) with validator hard-gate**

## Model

| Field | Value |
|-------|-------|
| Model | Qwen2.5-0.5B-Instruct GGUF Q4_K_M |
| Source | https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF |
| License | Apache-2.0 |
| Install dir | `%LocalAppData%/3DGod/Models/llama` or `$XDG_DATA_HOME/3DGod/Models/llama` |
| Sidecar | `license.json` with `accepted=true`, `licenseId=apache-2.0` |

## Architecture

1. Deterministic parser always runs first (unchanged)
2. If unsupported → `LlamaSharpProvider.Interpret`
3. LLamaSharp CPU backend loads licensed GGUF
4. Model must emit JSON matching `3dgod-ai-edit/1`
5. `AiEditPlanValidator` allow-lists operations; malformed output → Unsupported (never executes)

## Local proof (this agent)

- `make him taller` → `status=valid`, `operation=morph.height`
- `give him broader shoulders` → `status=valid`, `operation=morph.local`
- Invalid JSON from model → rejected (honest)

## Security

No shell, URLs, filesystem, reflection, or plugin ops in allow-list.
