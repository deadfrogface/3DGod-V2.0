# Final remaining-work report (FLUX / SkinTokens / UE5 / FlaUI)

**Branch tip:** `fb35b06afd23ff9e09f13a053133c3256b95e362`  
**Base (`main`):** `8eeaffff32a1e223d5c236e2230f959824514f5b`  
**PR:** https://github.com/deadfrogface/3DGod-V2.0/pull/6

## Commits on this branch

1. `6f4b7c3` — SkinTokens MIT worker, UE5 import smoke, FlaUI skip gates  
2. `0267bd0` — Wire FLUX/SkinTokens into services + Setup Assistant honesty labels  
3. `082f480` — FLUX live/E2E gated tests + final audit report  
4. `fb35b06` — Record tip SHA in final report

## Local verification (this agent)

- Release build: Infrastructure + App (`EnableWindowsTargeting`) **OK**
- Honesty + FluxLive + SkinTokens + ReferenceImage filter: **16 passed / 2 skipped** (FLUX live PNG + FLUX→TripoSR E2E gated on CUDA/checkpoint)
- Agent host: **no NVIDIA CUDA** → cannot claim PASS_REAL for FLUX/SkinTokens here

## Classifications (authoritative)

| Item | Status | Code complete? | User action / env for PASS_REAL |
|------|--------|----------------|----------------------------------|
| **FLUX** | **IMPLEMENTED_GATED_HARDWARE** | Yes | NVIDIA CUDA ≥8GB VRAM; HF accept `black-forest-labs/FLUX.1-schnell`@`741f7c3ce8b383c54771c7003378a50191e9efe9`; install `workers/flux` via Setup Assistant; run `FluxLiveTests.Generate_WhenReady_*` |
| **FLUX→TripoSR** | **IMPLEMENTED_GATED_HARDWARE** | Yes | Same as FLUX + TripoSR `model.ckpt`; `FluxLiveTests.FluxThenTripoSr_WhenBothReady_WritesRealGlb` |
| **SkinTokens** | **IMPLEMENTED_GATED_HARDWARE** | Yes (MIT cleared — not LICENSE) | CUDA ≥14GB; HF `VAST-AI/SkinTokens`; upstream under `workers/skintokens/upstream`; never fakes GLB |
| **UE5 import** | **IMPLEMENTED_GATED_UE5_RUNTIME** | Yes | `UE_ROOT` + `.uproject`; `scripts/ue5/Invoke-Ue5ImportSmoke.ps1` until log has `3DGOD_UE5_IMPORT_OK` |
| **FlaUI** | **IMPLEMENTED_GATED_EXTERNAL_RUNNER** | Yes | Interactive Windows desktop; `$env:THREEDGOD_INSTALL_ROOT`; `dotnet test 3DGodCreator.UiTests` — missing install = **Skip**, broken UI with install set = **Fail** |
| **Setup Assistant** | Honest user-facing labels | Yes | READY / OPTIONAL / NOT INSTALLED / INSTALLING / UPDATE AVAILABLE / HARDWARE UNSUPPORTED / LICENSE BLOCKED / BROKEN / DOWNLOAD UNAVAILABLE; Install only when source exists |

## Soft-pass audit

- FlaUI: silent `return` replaced with `Skip.If(..., "GATED_EXTERNAL_RUNNER …")`
- FLUX/SkinTokens: throw NotInstalled/UnsupportedHardware; never write fake PNG/GLB
- Live suites use `Xunit.SkippableFact` — skipped ≠ passed

## Commands for final runtime proof

```powershell
# FLUX (Windows + CUDA ≥8GB)
# 1) Setup Assistant → install Text → Character (flux)
# 2) Accept HF ToS for black-forest-labs/FLUX.1-schnell @ 741f7c3…
# 3) dotnet test .\3DGodCreator.Core.Tests -c Release --filter FullyQualifiedName~FluxLiveTests

# SkinTokens (CUDA ≥14GB)
# Acquire VAST-AI/SkinTokens checkpoints + clone upstream → workers/skintokens/upstream
# Then invoke Automatic Rigging / SkinTokensRigService.RigGlbAsync

# UE5
$env:UE_ROOT = 'C:\Program Files\Epic Games\UE_5.x'
.\scripts\ue5\Invoke-Ue5ImportSmoke.ps1 -AssetPath .\path\to\asset.fbx -ProjectPath .\MyProj.uproject

# FlaUI
$env:THREEDGOD_INSTALL_ROOT = 'C:\Program Files\3DGod'
dotnet test .\3DGodCreator.UiTests\3DGodCreator.UiTests.csproj -c Release
```

## Notes

- Do **not** use vague `GATED_MODEL` when only hardware is missing — classifications above are intentional.
- SF3D/SPAR3D remain **REJECTED** (InstallRejectedIds); SkinTokens removed from license reject list after MIT audit.
