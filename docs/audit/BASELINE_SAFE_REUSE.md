# Baseline – Safe Reuse (Wave 0)

- Date: 2026-03-22
- Branch: `cursor/safe-reuse-verify-b322`
- HEAD at baseline start: see git history commit `baseline-pre-safe-reuse`
- Host: Linux Cloud Agent (no WPF UI run; `net10.0` tests only)
- SDK: .NET 10.0.401 installed locally (`global.json` pins 10.0.102 with latestFeature rollForward)

## Test command

```bash
dotnet test 3DGodCreator.Core.Tests/3DGodCreator.Core.Tests.csproj -c Release
```

## Results (pre-change)

| Metric | Count |
|--------|------:|
| Passed | 257 |
| Failed | 10 |
| Skipped | 17 |
| Total | 284 |

### Failure class (environment, not product regressions)

All 10 failures are worker-host tests that require a `python` executable on PATH (`WorkerProcessHostTests`, `SecurityHardeningTests` worker cases, `WorkerSmartDiagnosticsTests`). Linux agent has no Python worker runtime; Windows CI remains the authority for those cases.

### Skips

GATED / optional Anny, GarmentCode, Blender, CUDA, UE5, manual visual — unchanged honesty model.

## Product truth anchors

- `docs/audit/FINAL_RELEASE_AUDIT.md`
- Capability gates via `IFeatureAvailabilityService` / `DynamicFeatureAvailabilityService`
