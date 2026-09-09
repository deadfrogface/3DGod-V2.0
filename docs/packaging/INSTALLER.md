# Installer / Update (PHASE 55 – Velopack)

Base installer ships **3D God Creator app + assets + presets + optional blender_embed stub only**.

AI model checkpoints and Python worker venvs are **not** included in the base installer. They install later via ModelManager (PHASE 56).

## Build release

```powershell
cd 3DGod-V2.0
.\scripts\packaging\Build-VelopackRelease.ps1 -Version 2.0.0 -Channel stable
```

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

## Clean VM gate — GATED_EXTERNAL_DEPENDENCY

**Not verified on the dev/build machine in this phase.**

Manual proof checklist:

1. Fresh Windows 10/11 x64 VM (no .NET SDK required if self-contained publish is enabled later).
2. Run `Setup.exe` from `artifacts/releases/stable/`.
3. Launch **3D God Creator** from Start Menu.
4. Create a new project — must open without Python/Conda/Blender preinstalled.
5. Confirm heavy workers show **NotInstalled** until ModelManager packages are added.

Record result in `docs/audit/PHASE_55_REPORT.md` when executed.
