# PHASE 57 Report

- Phase: 57 – License Gate
- Status: **PASS**
- Date: 2026-09-10

## Build Book DONE

- `THIRD_PARTY_NOTICES.txt` at repo root (NuGet + worker + blocked list)
- `MODEL_LICENSES.json` — every shipping/experimental component has a license record
- `ReleaseLicenseGate` — release validation + blocklist
- `LicenseGate` extended — rejects blocklisted license/model ids; router already excludes `LicenseBlocked`
- Release CI script: `scripts/ci/Validate-ReleaseLicenses.ps1`

## Blocklist (enforced in code + registry)

- Hunyuan EU / `hunyuan3d-eu`
- SMPL-X without commercial (`smpl-x`, `smpl-x-noncommercial`)
- AniGen restricted / S-Lab NC (`anigen`, `s-lab-nc`)
- Unverified checkpoint licenses (`unknown`, `unverified`)
- Incompatible GPL linkage (`gpl-3.0-linked`)

## Tests

- `PackagingAndLicenseGateTests` (14 tests): JSON parse, blocklist, manifest cross-check, `ReleaseLicenseGate_ValidatesRegistryAndManifests`
- `ProductPhaseTests.LicenseGate_RequiresExplicitAccept`
- `BackendRouterTests` — license blocked excluded from routing

## Build / CI

- `dotnet test -c Release --filter PackagingAndLicenseGateTests`: 14 bestanden
- Full suite: 260 bestanden, 1 fehlgeschlagen (`FbxExportTests.WhenBlenderPresent_*` — pre-existing blender_embed path on test output, unrelated to phase 57)
