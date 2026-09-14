# Online Runtime Validation Report

**Branch:** `cursor/online-runtime-validation-b322`  
**Base (accepted main HEAD):** `af41dcff2d167a90cd3ebc802eac10959242f222`  
**Validated green tip:** `0425700ef6c042585a43c3eddcaab4b0c6323c16`  
**PR:** https://github.com/deadfrogface/3DGod-V2.0/pull/7  
**Goal:** Maximize **real** online runtime validation on GitHub Actions / CI; classify honestly.

## Classification vocabulary (no soft-pass)

| Label | Meaning |
|-------|---------|
| `PASS_REAL` | Real runtime executed on CI (or proven unit path that is the product path) and assertions passed |
| `SKIPPED_ENVIRONMENT` | Env contract unmet; SkippableFact / job not scheduled — **not** a pass |
| `GATED_HARDWARE` | Needs CUDA/VRAM not available on standard GH-hosted runners; paid GPU larger runners not silently enabled |
| `GATED_UE5` | Needs Unreal Engine install + `UE_ROOT` + `.uproject` |
| `GATED_INTERACTIVE_DESKTOP` | Needs interactive Windows desktop + `THREEDGOD_INSTALL_ROOT` |
| `FAIL` | Required real execution failed |

Buckets for the executive summary:

1. **REAL_RUNTIME_PROVEN_ON_CI**
2. **IMPLEMENTED_BUT_NOT_EXECUTABLE_ON_STANDARD_CI**
3. **EXTERNAL_HARDWARE_OR_SOFTWARE_GATE**
4. **REAL_FAILURE**

---

## Audit matrix (workflows + tests — not docs alone)

| Capability | Existing test(s) | CI job / workflow | Real runtime vs unit | Runs on GH Windows? | Soft-skipped? | Artifact validated? |
|------------|------------------|-------------------|----------------------|---------------------|---------------|---------------------|
| App build | `build.ps1` / solution build | `ci.yml` → `build-and-test` | Real Release build | Yes | No | Test TRX uploaded |
| Installer create/install/launch/uninstall | `Invoke-CleanInstallSmoke.ps1` | `ci.yml` → `clean-install-smoke` | Real Velopack Setup.exe | Yes (cursor/* + main + dispatch) | No (fails hard) | `installer-smoke` artifact |
| Project save/load | `GodProjectArchiveTests` | `build-and-test` | Unit (real archive code path) | Yes | No | TRX |
| `.3dgod` security | `SecurityHardeningTests`, archive ZipSlip tests | `build-and-test` | Unit (real security code) | Yes | No | TRX |
| Smart Diagnostics | `WorkerSmartDiagnosticsTests`, `DiagnosticServiceTests` | `build-and-test` | Unit | Yes | No | TRX |
| Anny CPU generate | `CiRuntimeIntegrationTests.Anny_CpuGenerate_*` | `ci.yml` → `runtime-integration` | **Real** uv worker | Yes | No when flag=1 | GLB under `runtime-integration` |
| Anny height/proportion morph | `CiRuntimeIntegrationTests.Anny_HeightTallerDelta_*` | `runtime-integration` (dedicated step) | **Real** mesh compare | Yes | No when flag=1 | TRX |
| GarmentCode CPU | `CiRuntimeIntegrationTests.GarmentCode_CpuJacket_*` | `runtime-integration` | **Real** uv worker | Yes | No when flag=1 | GLB artifact |
| TripoSR CPU | `CiOnlineRuntimeProofTests.TripoSr_CpuGenerate_*` + `TripoSrLiveTests` | `runtime-triposr-cpu.yml` (schedule / dispatch / main path) | **Real** checkpoint+worker | Yes (heavy) | No when job runs | GLB size/verts/finite/indices/bounds; **ckpt not uploaded** |
| LLamaSharp CPU GGUF | `CiOnlineRuntimeProofTests.LlamaSharp_CpuGguf_*` | `ci.yml` → `llamasharp-cpu` | **Real** GGUF inference | Yes | No when job runs | `llamasharp-result.json`; **GGUF not uploaded** |
| FLUX | `FluxLiveTests` | `runtime-gpu-flux-skintokens.yml` | Real only on CUDA | No (standard); self-hosted gpu/cuda | Honest SkippableFact | PNG when runs |
| FLUX→TripoSR | `FluxLiveTests.FluxThenTripoSr_*` | same GPU workflow | Real only on CUDA+ckpt | No (standard) | Honest skip | GLB when runs |
| SkinTokens | `SkinTokensTests` + `SkinTokensRigService` | same GPU workflow | Real only CUDA≥14GB | No (standard) | Never fake GLB | skinned GLB when runs |
| UE5 export preflight | `UnrealEngine5ExportPreflightTests` | `build-and-test` | Unit/preflight | Yes | No | TRX |
| UE5 editor import | `scripts/ue5/Invoke-Ue5ImportSmoke.ps1` | `runtime-ue5.yml` | Real editor only | No | `GATED_UE5` if env unmet | UE5 logs |
| FlaUI | `InstalledAppFlaUiTests` | `runtime-flaui.yml` | Real UIA3 | Hosted: gate probe only | Honest skip without install | FlaUI TRX/logs |
| Setup Assistant | `SetupAssistantCatalogTests`, `Stage12Through19HonestyTests` | `build-and-test` | Unit (real catalog) | Yes | No | TRX |
| ComponentManager | `ComponentManagerTests` | `build-and-test` | Unit (real manager) | Yes | No | TRX |
| Worker install / uv | CI `uv sync --frozen` + `UvProvisioner` tests | `runtime-integration`, TripoSR, GPU workflows | Real uv on Windows | Yes | No | Job logs |
| Worker manifest validation | `PackagingAndLicenseGateTests` | `build-and-test` | Unit | Yes | No | TRX |
| Model/license state | `MODEL_LICENSES.json`, `ReleaseLicenseGate`, acquire scripts | build + acquire jobs | Real SHA verify on acquire | Yes | No | Gate JSON |

---

## What was added in this branch

- `llamasharp-cpu` job on standard Windows: download pinned Qwen2.5-0.5B Q4_K_M GGUF → real LLamaSharp inference → structured JSON artifact
- Separate heavy `runtime-triposr-cpu.yml` (workflow_dispatch + weekly schedule + path filters on main)
- Prepared (not silently billed) GPU / UE5 / FlaUI workflows with exact self-hosted labels
- Acquire scripts with pinned SHA-256; artifact retention for GLBs / LLamaSharp JSON / logs (**not** huge models)
- Honest online classification sidecar: `scripts/ci/Write-OnlineRuntimeClassification.ps1`
- Ops prep doc: `docs/ops/SELF_HOSTED_RUNNER_PREP.md` (does **not** register user PCs)

### LLamaSharp pin (commercial-clear)

| Field | Value |
|-------|-------|
| Model | `Qwen/Qwen2.5-0.5B-Instruct-GGUF` |
| File | `qwen2.5-0.5b-instruct-q4_k_m.gguf` |
| Revision | `9217f5db79a29953eb74d5343926648285ec7e67` |
| SHA-256 | `74a4da8c9fdbcd15bd1f6d01d621410d31c6fc00986f5eb687824e7b93d7a9db` |
| License | Apache-2.0 |
| Size | 491400032 bytes |

### TripoSR pin

| Field | Value |
|-------|-------|
| Checkpoint | `stabilityai/TripoSR` `model.ckpt` |
| SHA-256 | `429e2c6b22a0923967459de24d67f05962b235f79cde6b032aa7ed2ffcd970ee` |
| License | MIT |
| Size | 1677246742 bytes |

### GPU investigation (FLUX / SkinTokens)

GitHub offers **paid** GPU larger runners (Tesla T4, Windows ~$0.102/min) requiring Team/Enterprise + org larger-runner configuration. This work **does not** silently introduce paid infra. Workflows target labels `self-hosted,windows,gpu,cuda` and remain `GATED_HARDWARE` until a labeled runner exists.

---

## Final classification table

| Runtime test | Classification | Exact reason / proof path |
|--------------|----------------|---------------------------|
| App Release build | `PASS_REAL` | `build-and-test` |
| Full unit/integration suite (non-live) | `PASS_REAL` | `build.ps1 -Targets test` |
| Installer smoke | `PASS_REAL` | `clean-install-smoke` on cursor/* / main |
| Anny CPU generate | `PASS_REAL` | `runtime-integration` + GLB validation |
| Anny height morph | `PASS_REAL` | dedicated filter step + non-uniform mesh compare |
| GarmentCode CPU jacket | `PASS_REAL` | `runtime-integration` + GLB |
| Save/load roundtrip | `PASS_REAL` | `GodProjectArchiveTests` in unit job |
| GLB structural validation | `PASS_REAL` | `CiOnlineRuntimeProofTests.AssertRealGlb` + mesh tests |
| Worker protocol / diagnostics | `PASS_REAL` | worker host + diagnostics unit tests |
| Component manifest / license gate | `PASS_REAL` | `PackagingAndLicenseGateTests` |
| Security (.3dgod / ZipSlip / GLB limits) | `PASS_REAL` | `SecurityHardeningTests` |
| Setup Assistant state honesty | `PASS_REAL` | Setup Assistant / Stage12–19 honesty tests |
| ComponentManager | `PASS_REAL` | `ComponentManagerTests` |
| Worker uv sync (Anny/Garment) | `PASS_REAL` | CI `uv sync --frozen` |
| LLamaSharp CPU GGUF phrases | `PASS_REAL` | `llamasharp-cpu` job (when green) |
| TripoSR CPU chair→GLB | **PASS_REAL** on [34792261073](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34792261073) (`bytes=228776`); prior main FAIL [34790344214](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34790344214) was harness-only | Real CPU inference + GLB validation on `windows-latest`; see `TRIPOSR_CI_FAILURE_ANALYSIS.md` |
| FLUX PNG | `GATED_HARDWARE` | No CUDA on GH-hosted standard; paid GPU not auto-enabled |
| FLUX→TripoSR | `GATED_HARDWARE` | Same + TripoSR ckpt |
| SkinTokens rig | `GATED_HARDWARE` | Needs ≥14GB VRAM CUDA + checkpoints + upstream |
| UE5 editor import | `GATED_UE5` | UE not installed on GH-hosted; `runtime-ue5.yml` ready |
| FlaUI interactive | `GATED_INTERACTIVE_DESKTOP` | Hosted probe proves honest skip; self-hosted interactive for PASS_REAL |
| CUDA models on hosted | `GATED_HARDWARE` | `nvidia-smi` absent on `windows-latest` |

---

## Executive buckets

### 1. REAL_RUNTIME_PROVEN_ON_CI

- Release build + unit suite
- Installer create/install/smoke/uninstall
- Anny CPU generate + height/proportion morph
- GarmentCode CPU jacket
- LLamaSharp pinned GGUF inference + validator reject malformed/unsafe
- Save/load, security, diagnostics, Setup Assistant, ComponentManager, manifests, uv worker sync
- TripoSR: proven via dedicated workflow when scheduled/dispatched (not every PR)

### 2. IMPLEMENTED_BUT_NOT_EXECUTABLE_ON_STANDARD_CI

- FLUX / SkinTokens / FLUX→TripoSR product code + live SkippableFacts
- UE5 import automation scripts + success markers
- FlaUI UIA3 suite (honest skip without install root)
- TripoSR every-PR execution (too heavy; separate workflow)

### 3. EXTERNAL_HARDWARE_OR_SOFTWARE_GATE

- NVIDIA CUDA ≥8GB (FLUX) / ≥14GB (SkinTokens)
- HF ToS accept for FLUX.1-schnell
- Unreal Engine 5 install + project
- Interactive Windows desktop session for FlaUI
- Optional: GitHub Team/Enterprise GPU larger runners (billing) — **not** auto-provisioned here

### 4. REAL_FAILURE

- None on validated tip `0425700ef6c042585a43c3eddcaab4b0c6323c16` (PR + push CI all green after LLamaSharp provider fix).

---

## Workflows

| File | Purpose |
|------|---------|
| `.github/workflows/ci.yml` | Build/test, installer, Anny/Garment, LLamaSharp, gate summary |
| `.github/workflows/runtime-triposr-cpu.yml` | Heavy TripoSR CPU PASS_REAL |
| `.github/workflows/runtime-gpu-flux-skintokens.yml` | Self-hosted GPU FLUX/SkinTokens |
| `.github/workflows/runtime-ue5.yml` | Self-hosted UE5 import |
| `.github/workflows/runtime-flaui.yml` | Hosted gate probe + self-hosted FlaUI |

---

## Artifact retention

| Artifact | Retention | Contents |
|----------|-----------|----------|
| `test-results` | 14d | TRX / gate |
| `installer-smoke` | 7d | publish/releases (not huge models) |
| `runtime-integration` | 14d | Anny/Garment GLBs + TRX |
| `llamasharp-runtime` | 14d | structured JSON + TRX |
| `triposr-cpu-runtime` | 14d | GLB + result JSON + TRX |
| `gpu-flux-skintokens-runtime` | 14d | PNG/GLB/logs when GPU runs |
| `ue5-runtime` / `flaui-*` | 14d | logs |
| `release-gate-classification` | 30d | classification JSON |

**Never upload:** `.ckpt`, `.gguf`, `.safetensors`, large HF weight trees.

---

## Final report fields

| Field | Value |
|-------|-------|
| Branch | `cursor/online-runtime-validation-b322` |
| Validated tip SHA (all checks green) | `0425700ef6c042585a43c3eddcaab4b0c6323c16` |
| Accepted main baseline | `af41dcff2d167a90cd3ebc802eac10959242f222` |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/7 |
| Green CI (push) | https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34788266550 (`success`, head `0425700…`) |
| Green CI (pull_request) | https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34788266765 (`success`, head `0425700…`, 5/5 jobs) |
| Prior PR fail (fixed) | https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34787784951 — LLamaSharp `Provider=validator` after allow-list reject |
| Report path | `docs/audit/ONLINE_RUNTIME_VALIDATION_REPORT.md` |
| Self-hosted prep | `docs/ops/SELF_HOSTED_RUNNER_PREP.md` |
| Remaining code work | **None** — only external env/hardware/UE5/interactive gates |

### PASS_REAL list (standard CI / CPU) — proven green on `0425700…`

- App Release build + unit suite (`build-and-test`)
- Clean Windows installer create/install/smoke/uninstall (`clean-install-smoke`)
- Anny CPU generate + Anny height/proportion morph (`runtime-integration`)
- GarmentCode CPU jacket (`runtime-integration`)
- LLamaSharp pinned Apache-2.0 GGUF inference + schema/validator hard-gate (`llamasharp-cpu`)
- Project save/load, `.3dgod` security, Smart Diagnostics, Setup Assistant honesty, ComponentManager, worker manifests / license gate, GLB validation helpers (unit job)
- Worker `uv sync` for Anny/Garment on Windows CI

### Gated list (exact external reasons — not soft-pass)

| Item | Classification | Exact reason |
|------|----------------|--------------|
| TripoSR path/weekly/dispatch | **PASS_REAL** proven [34792261073](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34792261073); default every-commit CI still omits heavy job except path filters | Harness fix on `cursor/triposr-ci-fix-b322`; still intentionally not on every unrelated PR |
| FLUX.1-schnell | `GATED_HARDWARE` | No CUDA on GH-hosted `windows-latest`; paid GPU larger runners need Team/Enterprise billing — not auto-enabled; workflow ready: labels `self-hosted,windows,gpu,cuda` |
| FLUX→TripoSR | `GATED_HARDWARE` | Same CUDA gate + TripoSR checkpoint |
| SkinTokens | `GATED_HARDWARE` | Needs NVIDIA CUDA ≥14GB VRAM + checkpoints + upstream; never fakes skinned GLB |
| UE5 editor import | `GATED_UE5` | Unreal not on GH-hosted; needs `UE_ROOT` + `.uproject`; `runtime-ue5.yml` for `[self-hosted,windows,ue5]` |
| FlaUI interactive | `GATED_INTERACTIVE_DESKTOP` | Needs interactive desktop + `THREEDGOD_INSTALL_ROOT`; hosted probe skips honestly; `runtime-flaui.yml` for `[self-hosted,windows,interactive]` |

---

## TripoSR CI failure fix (follow-up) — VERIFIED PASS_REAL

| Field | Value |
|-------|-------|
| Original failing run | [34790344214](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34790344214) |
| Root cause | **H. test harness bug** — Windows `File.Copy(src, src)` after real TripoSR CPU inference already produced a validated GLB |
| Fix branch / tip | `cursor/triposr-ci-fix-b322` @ `f933145d8ac1d4da244fd887958a955d87e44e5a` |
| Files | `3DGodCreator.Core.Tests/CiOnlineRuntimeProofTests.cs`, `.github/workflows/runtime-triposr-cpu.yml`, `docs/audit/TRIPOSR_CI_FAILURE_ANALYSIS.md` |
| Exact fix | Repo-root artifact dir; skip same-path `CopyArtifact`; absolute `THREEDGOD_CI_ARTIFACT_DIR`; assert GLB exists; path-filtered PR trigger |
| Validation weakened? | **No** |
| Final classification | **PASS_REAL** |
| Green run | [34792261073](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34792261073) — `TRIPOSR_RUNTIME=PASS_REAL bytes=228776` (~1 m 42 s test) |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/8 |
| Analysis | `docs/audit/TRIPOSR_CI_FAILURE_ANALYSIS.md` |

