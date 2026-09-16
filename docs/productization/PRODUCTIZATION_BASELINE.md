# Productization Baseline

| Field | Value |
|-------|-------|
| **MAIN_SHA** | `ae077bfa48a1f9690af223590b2a302af898da7e` |
| **Branch** | `cursor/productization-windows-b322` |
| **Date** | 2026-09-16 |
| **Host** | Linux cloud agent (Windows targeting build) |

## Build / Test (pre-change)

| Check | Result |
|-------|--------|
| `dotnet build 3DGodCreator.sln -c Release -p:EnableWindowsTargeting=true` | **PASS** (0 warn / 0 err) |
| Targeted Core.Tests (ProductWorkflow / RealClothing / ComponentManager / SkinTokens / GarmentFit / GarmentCode / Composition) | **30 passed**, 1 skipped |

## Existing capabilities (post PR #10)

| Area | Status |
|------|--------|
| ActiveProjectSession / `.3dgod` MeshBytes | PASS_REAL |
| Anny / GarmentCode / GarmentFit UI + headless | PASS_REAL when workers present |
| Composed BODY+GARMENT viewport data path | IMPLEMENTED; visual `GATED_INTERACTIVE` |
| Composed GLB export | PASS_REAL |
| Autosave / recovery | PASS_REAL |
| SkinTokens Auto-Rig | CUDA-only `ISkinTokensRigService`; `IAutoRigBackend` NotInstalled stub |
| Setup Assistant | Manifest install via uv; honesty copy present |
| Velopack clean install smoke | CI_VERIFIED on Windows hosted |
| FlaUI installed-app | GATED_INTERACTIVE (self-hosted) |

## External gates at baseline

- Auto-Rig = NVIDIA CUDA ≥14GB + SkinTokens checkpoints (no CPU/Vulkan product path yet)
- Workers require repo `workers/*` + uv (not zero-manual from packaged app alone)
- FLUX / SkinTokens GPU self-hosted
- FlaUI interactive desktop
- UE5 editor import
- Helix BODY+JACKET visual pixels

## Bundled / installed components (base installer)

Per `docs/packaging/INSTALLER.md`: base Velopack ships **app + assets + presets**; AI workers/checkpoints **not** in base installer (`includedInBaseInstaller: false`).

## Installer behavior

- `scripts/packaging/Build-VelopackRelease.ps1` → Setup.exe
- `scripts/ci/Invoke-CleanInstallSmoke.ps1` → install/launch/uninstall smoke on Windows CI
- End-user still expected to use Setup Assistant for Anny/Garment/TripoSR/SkinTokens

## Goal of this branch

Multi-backend Auto-Rig (skin-tokens.cpp CPU/Vulkan + optional official CUDA), zero-manual-dependency component install from packaged app, honest PRODUCT_VALIDATED release gate, end-user validation evidence.
