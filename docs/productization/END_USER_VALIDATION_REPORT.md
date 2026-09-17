# End-User Validation Report

| Field | Value |
|-------|-------|
| Branch | `cursor/productization-windows-b322` |
| Tip SHA (evidence) | `9155e3722312df6fa08ecaf4d4c85cf9afb22331` (docs tip follows on same branch) |
| Baseline main | `ae077bfa48a1f9690af223590b2a302af898da7e` |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/11 |
| CI (PR) | run [`35169173164`](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/35169173164) — **success** |
| **PRODUCT_VALIDATED** | **True** (Release Gate Summary; Auto-Rig CPU + Installed E2E PASS_REAL) |
| Auto-Rig architecture | Multi-provider (cpp CPU/Vulkan + official CUDA) |
| Packaging | Velopack small-core + Setup Assistant |

## Capability matrix

| Capability | Status | Evidence |
|------------|--------|----------|
| WINDOWS INSTALLER | PASS_REAL | Clean Windows Installer Smoke |
| FIRST LAUNCH | PASS_REAL (smoke) / GATED_INTERACTIVE (full UI) | InstalledAppSmoke; FlaUI self-hosted |
| SETUP ASSISTANT | PASS_REAL (logic) | SetupAssistantCatalogTests / Stage12Through19HonestyTests |
| AUTOMATIC DEPENDENCY INSTALL | PASS_REAL | UvProvisioner Anny/Garment + SkinTokensCppProvisioner (CLI+GGUF) |
| AUTOMATIC COMPONENT REPAIR | PASS_REAL (uv path) | WorkerUvComponentInstaller Repair |
| ANNY HUMAN | PASS_REAL / CI_VERIFIED | Anny / Garment CPU Integration + Installed E2E |
| ANNY EDIT | PASS_REAL | taller-delta / session sync / installed morph |
| GARMENTCODE | PASS_REAL / CI_VERIFIED | Anny / Garment CPU Integration + Installed E2E |
| GARMENT FIT | PASS_REAL | CI + ProductWorkflow + Installed E2E |
| BODY + GARMENT VIEWPORT | PASS_REAL (state/export); GATED_INTERACTIVE (visual) | meshCount=2 + FlaUI gate |
| MATERIAL EDIT | PASS_REAL | UpsertMaterial + archive |
| SAVE / REOPEN | PASS_REAL | ProductWorkflow / GodProjectArchiveTests / Installed E2E |
| AUTOSAVE / RECOVERY | PASS_REAL | Autosave tests |
| COMPOSED GLB EXPORT | PASS_REAL | ComposeScenes + `installed-composed.glb` |
| TRIPOSR | PASS_REAL (CPU workflow) / GATED when missing | separate TripoSR workflow |
| AUTO-RIG SELECTOR | PASS_LOGIC_ONLY | AutoRigProviderSelectorTests (6 passed in autoroot job) |
| AUTO-RIG CPU | **PASS_REAL** | skin-tokens.cpp@43e885af; provenance `device=cpu`; durationMs=134474 |
| AUTO-RIG VULKAN | GATED_EXTERNAL_HARDWARE | VulkanRig skipped; no silent CPU claim |
| AUTO-RIG NVIDIA CUDA | GATED_EXTERNAL_HARDWARE | SkinTokensCuda provider |
| FLUX | GATED_EXTERNAL_HARDWARE | Release Gate |
| SKINTOKENS OFFICIAL | GATED_EXTERNAL_HARDWARE | Release Gate |
| LLAMASHARP | PASS_REAL | LLamaSharp CPU GGUF Proof |
| SMART DIAGNOSTICS | PASS_REAL | Diagnose / PipelineTrace tests |
| UNINSTALL | PASS_REAL | Velopack uninstall (smoke + installed-e2e) |
| UE5 EXPORT PREP | PASS_LOGIC_ONLY / PARTIAL | FBX/preflight honesty |
| UE5 EDITOR IMPORT | GATED_EXTERNAL_HARDWARE | |
| FLAUI / INSTALLED GUI | GATED_INTERACTIVE_WINDOWS | hosted runner has no interactive desktop |
| ATTACHMENTS | NOT_IMPLEMENTED | honest |
| CREATURE CREATOR | NOT_IMPLEMENTED / backend-only | out of scope |

## UI honesty notes

- RiggingPanel uses `FeatureIds.RigAuto` + provider ComboBox (Automatic/Vulkan/CPU/CUDA).
- MetaHuman remains Unavailable/NotImplemented.
- Attachments/Creatures not expanded in this branch.

## Rejected dependencies

See [AUTORIG_PROVIDER_EVALUATION.md](AUTORIG_PROVIDER_EVALUATION.md).

## Release readiness

Core product paths including **real skin-tokens.cpp CPU Auto-Rig** and **Velopack installed-product E2E** are **PRODUCT_VALIDATED**. Optional GPU/Vulkan/FlaUI/UE5 remain honestly gated. Full detail: [FINAL_VALIDATION_PR11.md](FINAL_VALIDATION_PR11.md). **Do not merge until human review.**
