# PHASE 56 Report

- Phase: 56 – Worker/Model Packaging
- Status: **PASS** (honest manifests + ModelManager; release ZIPs not built)
- Date: 2026-09-10

## Build Book DONE

- Worker manifests with pinned version, uv lock refs, license, requirements, `includedInBaseInstaller: false`
- Schema: `docs/packaging/worker-package-manifest.schema.json`
- Reader/validator: `WorkerPackageManifestReader` + `WorkerPackageManifest.cs`
- **Dev workers:** `workers/anny`, `workers/garmentcode`, `workers/echo` — status **Experimental** / **Available** (demo)
- **Gated backends:** `docs/packaging/workers/*.manifest.json` — **NotInstalled** with honest license fields
- ModelManager ZIP+SHA256 install path unchanged (`echo` demo)
- User must not manually install Python/Conda for product path; uv ships in worker release packages when built

## Not done (honest)

| Item | Status |
|------|--------|
| Production release ZIPs with pinned model hash/size | Not built — `releasePackage.sha256` null |
| Bundled uv runtime in installer | Experimental dev path only (`uv` on PATH) |
| Full worker toolchain (Blender embed) in one-click package | Blender remains optional / separate gate |

## Tests

- `PackagingAndLicenseGateTests.WorkerManifests_MatchSchemaAndExcludeModelsFromBaseInstaller`
- `PackagingAndLicenseGateTests.AnnyAndGarmentManifests_AreExperimentalWithUvLock`
- `HardwareAndModelTests.ModelManager_*` (echo demo)

## Build

- `dotnet build -c Release`: 0 Fehler
