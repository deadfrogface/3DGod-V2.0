# Final Product Release Gate

## PRODUCT_VALIDATED contract

A build may be labeled **PRODUCT_VALIDATED** only when all **core** (non-hardware-optional) flows pass with real evidence:

| Core requirement | Evidence |
|------------------|----------|
| Release build + unit/integration tests | CI Build and Test SUCCESS |
| Clean Windows installer smoke | Velopack Setup.exe install/launch/uninstall |
| Anny CPU real | runtime-integration PASS_REAL |
| GarmentCode + Fit real | runtime-integration PASS_REAL |
| Project save/reopen + composed export | ProductWorkflow / RealClothing / Installed E2E |
| Auto-Rig CPU (skin-tokens.cpp) | **PASS_REAL** — CLI+GGUF provisioned and inference executed |
| Installed-product E2E | **PASS_REAL** — Velopack install + component provision + core flows |

Optional hardware features may remain gated without blocking PRODUCT_VALIDATED:

- Auto-Rig Vulkan
- Official SkinTokens CUDA
- FLUX
- FlaUI interactive visual (`GATED_INTERACTIVE_WINDOWS` on hosted)
- UE5 editor import

**Not acceptable as “external gate”:** missing skin-tokens.cpp CLI/model that the product is responsible for installing/managing.

## Latest evaluated tip

| Field | Value |
|-------|-------|
| SHA | `0c3573a2bc83e957f0e0046d768a1ca664df8184` |
| PR CI run | `35162936197` |
| PRODUCT_VALIDATED | **True** |
| Auto-Rig CPU job | `GATED_EXTERNAL_RUNTIME` (CLI/GGUF not on runner) |

## Machine-readable

`artifacts/gate/release-gate-classification.json` includes `productValidated: true|false`.

## Human summary

See CI job **Release Gate Summary** and [END_USER_VALIDATION_REPORT.md](END_USER_VALIDATION_REPORT.md).
