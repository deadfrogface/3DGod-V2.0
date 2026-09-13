# Stage 15 — LLamaSharp free-form → AiEditPlan

**Status: PASS_REAL (local CPU GGUF) with validator hard-gate**

## Model

| Field | Value |
|-------|-------|
| Model | Qwen2.5-0.5B-Instruct GGUF Q4_K_M |
| Source | https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF |
| File | `qwen2.5-0.5b-instruct-q4_k_m.gguf` |
| Revision | `9217f5db79a29953eb74d5343926648285ec7e67` |
| SHA-256 | `74a4da8c9fdbcd15bd1f6d01d621410d31c6fc00986f5eb687824e7b93d7a9db` |
| Size | 491400032 bytes |
| License | Apache-2.0 |
| Install dir | `%LocalAppData%/3DGod/Models/llama` or `$XDG_DATA_HOME/3DGod/Models/llama` |
| Sidecar | `license.json` with `accepted=true`, `licenseId=apache-2.0` |
| CI acquire | `scripts/ci/Invoke-AcquireLlamaGguf.ps1` + job `llamasharp-cpu` in `.github/workflows/ci.yml` |

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

## Packaging note

`LLamaSharp.Backend.Cpu` ships AVX variants under `runtimes/<rid>/native/{noavx,avx,avx2,avx512}/`.
`Directory.Build.targets` rewrites publish `RelativePath` so Velopack/`dotnet publish` keeps that hierarchy
(avoids `NETSDK1152` from flattened duplicate DLL names).

## Security

No shell, URLs, filesystem, reflection, or plugin ops in allow-list.
