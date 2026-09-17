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
| SHA | `9155e3722312df6fa08ecaf4d4c85cf9afb22331` (docs tip follows on same branch) |
| PR CI run | [`35169173164`](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/35169173164) |
| PRODUCT_VALIDATED | **True** |
| Auto-Rig CPU | **PASS_REAL** (`autoroot-cpu-proof.json`, durationMs=134474) |
| Installed Product E2E | **PASS_REAL** (`meshCount=2`, `garmentCount=1`) |

## Machine-readable

`artifacts/gate/release-gate-classification.json` includes `productValidated: true|false`.

## Human summary

See CI job **Release Gate Summary**, [FINAL_VALIDATION_PR11.md](FINAL_VALIDATION_PR11.md), and [END_USER_VALIDATION_REPORT.md](END_USER_VALIDATION_REPORT.md).
