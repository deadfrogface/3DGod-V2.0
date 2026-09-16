# Final Product Release Gate

## PRODUCT_VALIDATED contract

A build may be labeled **PRODUCT_VALIDATED** only when all **core** (non-hardware-optional) flows pass with real evidence:

| Core requirement | Evidence |
|------------------|----------|
| Release build + unit/integration tests | CI Build and Test SUCCESS |
| Clean Windows installer smoke | Velopack Setup.exe install/launch/uninstall |
| Anny CPU real | runtime-integration PASS_REAL |
| GarmentCode + Fit real | runtime-integration PASS_REAL |
| Project save/reopen + composed export | ProductWorkflow / RealClothing tests |
| Auto-Rig provider architecture | AutoRigProviderSelectorTests PASS; CPU runtime PASS_REAL **or** honest GATED_EXTERNAL_RUNTIME |

Optional hardware features may remain gated without blocking PRODUCT_VALIDATED:

- Auto-Rig Vulkan
- Official SkinTokens CUDA
- FLUX
- FlaUI interactive visual
- UE5 editor import

## Machine-readable

`artifacts/gate/release-gate-classification.json` includes `productValidated: true|false`.

## Human summary

See CI job **Release Gate Summary** and [END_USER_VALIDATION_REPORT.md](END_USER_VALIDATION_REPORT.md).
