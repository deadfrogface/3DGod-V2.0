# Final remaining-work report (FLUX / SkinTokens / UE5 / FlaUI)

**Branch tip:** `0267bd079c9e99f4e16340c6db3e8e91618ac14c`  
**Base:** `main` @ `8eeaffff32a1e223d5c236e2230f959824514f5b`

## Commits

1. `6f4b7c3` — SkinTokens MIT worker, UE5 import smoke, FlaUI skip gates  
2. `0267bd0` — Wire FLUX/SkinTokens into services + Setup Assistant honesty labels  

## Local verification (this agent)

- Release build: Infrastructure + App (`EnableWindowsTargeting`) **OK**
- Honesty filter tests (Stage12 / ReferenceImage / SkinTokens / SetupAssistant): **18 passed**
- Full Core.Tests: 300 passed / 11 skipped / 13 failed — failures are **pre-existing env issues** (LLamaSharp GGUF present → “valid”; TripoSR/worker host quirks), also failing on `main` samples; not introduced by this branch’s honesty suite

## Classifications

| Item | Status | Code complete? | User action / env for PASS_REAL |
|------|--------|----------------|----------------------------------|
| **FLUX** | **IMPLEMENTED_GATED_HARDWARE** | Yes | NVIDIA CUDA ≥8GB VRAM; HF accept `black-forest-labs/FLUX.1-schnell`@`741f7c3…`; Setup Assistant install flux; generate PNG |
| **FLUX→TripoSR** | **IMPLEMENTED_GATED_HARDWARE** | Yes (chain uses existing TripoSR PASS_REAL) | Same as FLUX + TripoSR checkpoint |
| **SkinTokens** | **IMPLEMENTED_GATED_HARDWARE** | Yes (MIT cleared) | CUDA ≥14GB; acquire HF `VAST-AI/SkinTokens`@pinned rev; upstream under `workers/skintokens/upstream` |
| **UE5 import** | **IMPLEMENTED_GATED_UE5_RUNTIME** | Yes | Set `UE_ROOT` + `.uproject`; run `scripts/ue5/Invoke-Ue5ImportSmoke.ps1` until log has `3DGOD_UE5_IMPORT_OK` |
| **FlaUI** | **IMPLEMENTED_GATED_EXTERNAL_RUNNER** | Yes | Interactive Windows desktop; `THREEDGOD_INSTALL_ROOT` to installed app; `dotnet test 3DGodCreator.UiTests` |
| Setup Assistant | Honest labels + install flags | Yes | Reflects READY / NOT INSTALLED / HARDWARE UNSUPPORTED / LICENSE BLOCKED / DOWNLOAD UNAVAILABLE / … |

## Commands for final runtime proof

```powershell
# FLUX (Windows + CUDA)
# 1) Install component via Setup Assistant (or uv sync workers/flux)
# 2) Acquire weights (HF token / gated:auto accepted)
# 3) Generate from app Text→Character / ReferenceImageService

# UE5
$env:UE_ROOT = 'C:\Program Files\Epic Games\UE_5.x'
.\scripts\ue5\Invoke-Ue5ImportSmoke.ps1 -AssetPath .\path\to\asset.fbx -ProjectPath .\MyProj.uproject

# FlaUI
$env:THREEDGOD_INSTALL_ROOT = 'C:\Program Files\3DGod'
dotnet test .\3DGodCreator.UiTests\3DGodCreator.UiTests.csproj -c Release
```
