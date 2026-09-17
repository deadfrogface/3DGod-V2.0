# Installer / Update (PHASE 55 – Velopack) — productization update

Base installer ships **3D God Creator app + assets + presets + worker scripts/locks (no multi-GB checkpoints)**.

AI model checkpoints, Python venvs, skin-tokens.cpp CLI binaries, and GGUF bundles are **not** included in the base installer. They install later via Setup Assistant / UvProvisioner / ModelManager.

## Build release

```powershell
cd 3DGod-V2.0
.\scripts\packaging\Build-VelopackRelease.ps1 -Version 2.0.0 -Channel stable
```

Prefer **self-contained** publish so end users do not need the .NET SDK.

Publish-only (no `vpk`):

```powershell
.\scripts\packaging\Build-VelopackRelease.ps1 -SkipVpk
```

## Update channel

| Variable | Default | Purpose |
|----------|---------|---------|
| `THREEDGOD_UPDATE_CHANNEL` | `stable` | Release channel (`stable`, `beta`, `dev`) |
| `THREEDGOD_UPDATE_FEED_URL` | _(unset)_ | Velopack feed URL when hosting updates |

Artifacts land in `artifacts/releases/<channel>/`.

## Uninstall policy

- Uninstall removes application binaries and Start Menu / Desktop shortcuts.
- **User `.3dgod` projects are never deleted.**
- `%LocalAppData%/3DGod/Models` and `Components` caches may remain; optional cleanup via Setup Assistant Remove.
- `%LocalAppData%/3DGod/Recovery` autosave snapshots are preserved unless the user clears them.

## Clean install gate

Hosted Windows CI runs `scripts/ci/Invoke-CleanInstallSmoke.ps1` (install → launch smoke → uninstall). Interactive FlaUI end-user flows remain `GATED_INTERACTIVE` without a self-hosted desktop runner.
