# PHASE 55 Report

- Phase: 55 – Installer / Update (Velopack)
- Status: **PASS** (implementation) + **GATED_EXTERNAL_DEPENDENCY** (Clean VM proof)
- Date: 2026-09-10

## Build Book DONE (implementation)

- Velopack NuGet + `Program.cs` entry hook (`VelopackApp.Build().Run()`)
- App version wired via `Directory.Build.props` (`2.0.0`)
- Update channel via `VelopackUpdateChannel` + env `THREEDGOD_UPDATE_CHANNEL` / `THREEDGOD_UPDATE_FEED_URL`
- Packaging script: `scripts/packaging/Build-VelopackRelease.ps1` (`dotnet publish` + `vpk pack`)
- **AI models / worker venvs NOT in base installer** (publish copies only assets/presets/blender_embed)
- Docs: `docs/packaging/INSTALLER.md`

## Gated (not verified here)

| Gate | Reason |
|------|--------|
| Clean VM install → start → project create | No fresh VM on this machine; manual checklist in INSTALLER.md |

Publish-only verified on dev machine: `artifacts/publish/win-x64/` (no `workers/` folder).

## Tests

- `PackagingAndLicenseGateTests.VelopackUpdateChannel_ReadsDefaults`
- Publish script: `Build-VelopackRelease.ps1 -SkipVpk` exit 0

## Build

- `dotnet build -c Release`: 0 Fehler, 0 Warnungen
