# Dependency and Component Architecture

## Strategy

**Small core Velopack installer** + **in-app optional component downloads**.

End users must not manually install Python, Git, Visual Studio, CMake, CUDA toolkit, or clone repos.

## Roots ([InstallLayout.cs](../../src/ThreeDGod.Infrastructure/InstallLayout.cs))

| Root | Path |
|------|------|
| AppBase | Velopack publish directory (ships `workers/*/pyproject.toml` + locks when packaged) |
| DataRoot | `%LocalAppData%/3DGod` |
| ModelsRoot | `%LocalAppData%/3DGod/Models` |
| ComponentsRoot | `%LocalAppData%/3DGod/Components` |
| UvRoot | `%LocalAppData%/3DGod/Tools/uv` |
| LogsRoot | `%LocalAppData%/3DGod/Logs` |
| RecoveryRoot | `%LocalAppData%/3DGod/Recovery` |

`ResolveContentRoot()` prefers AppBase `workers/`, then walks up for a developer checkout.

## Component matrix

| Id | Ship | Install | Hardware | License |
|----|------|---------|----------|---------|
| uv | on-demand via UvProvisioner | pinned binary | none | MIT (Astral) |
| anny | scripts+lock in app content | `uv sync --frozen` | CPU | Apache-2.0 |
| garmentcode | scripts+lock | `uv sync --frozen` | CPU | research/deps per STAGE11 |
| triposr | scripts+lock | uv + ckpt acquire | CPU slow / GPU | MIT |
| skintokens-cpp | CLI on-demand | CLI + GGUF F16 | CPU / Vulkan | Apache-2.0 + MIT GGUF |
| skintokens | scripts+lock | uv + HF ckpts | CUDA ≥14GB | MIT |
| flux | scripts+lock | uv + weights | CUDA ≥8GB | FLUX license |
| llamasharp | NuGet in app | GGUF on-demand | CPU | MIT |

## States

NotInstalled → Installing → Ready | Unavailable | HardwareUnsupported | LicenseBlocked | Broken | UpdateAvailable

## Downloads

[ComponentDownloadService](../../src/ThreeDGod.Infrastructure/Components/ComponentDownloadService.cs): http(s) only, SHA-256, resume, progress, timeout. Sources must be allowlisted (Setup / UvProvisioner / HF pins).

## Offline

After components+models installed once, Anny / Garment / TripoSR CPU / skin-tokens.cpp CPU / save-reopen / export remain local. No paid cloud API.
