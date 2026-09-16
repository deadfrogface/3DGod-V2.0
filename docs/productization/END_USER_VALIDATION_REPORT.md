# End-User Validation Report

| Field | Value |
|-------|-------|
| Branch | `cursor/productization-windows-b322` |
| Tip SHA | `0c3573a2bc83e957f0e0046d768a1ca664df8184` |
| Baseline main | `ae077bfa48a1f9690af223590b2a302af898da7e` |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/11 |
| CI (PR) | run `35162936197` — **12/12 success** |
| **PRODUCT_VALIDATED** | **True** (Release Gate Summary) |
| Auto-Rig architecture | Multi-provider (cpp CPU/Vulkan + official CUDA) |
| Packaging | Velopack small-core + Setup Assistant |

## Capability matrix

| Capability | Status | Evidence |
|------------|--------|----------|
| WINDOWS INSTALLER | CI_VERIFIED / PASS_REAL | Clean Windows Installer Smoke |
| FIRST LAUNCH | PASS_REAL (smoke) / GATED_INTERACTIVE (full UI) | InstalledAppSmoke; FlaUI self-hosted |
| SETUP ASSISTANT | PASS_REAL (logic) | SetupAssistantCatalogTests / Stage12Through19HonestyTests |
| AUTOMATIC DEPENDENCY INSTALL | PARTIAL | UvProvisioner + allowlisted downloads; skintokens-cpp CLI still on-demand |
| AUTOMATIC COMPONENT REPAIR | PASS_REAL (uv path) | WorkerUvComponentInstaller Repair |
| ANNY HUMAN | PASS_REAL / CI_VERIFIED | Anny / Garment CPU Integration |
| ANNY EDIT | PASS_REAL | taller-delta / session sync |
| GARMENTCODE | PASS_REAL / CI_VERIFIED | Anny / Garment CPU Integration |
| GARMENT FIT | PASS_REAL | CI + ProductWorkflow |
| BODY + GARMENT VIEWPORT | IMPLEMENTED_GATED_INTERACTIVE_VISUAL_PROOF | MaterializeSceneGlbs + Helix compose |
| MATERIAL EDIT | PASS_REAL | UpsertMaterial + archive |
| SAVE / REOPEN | PASS_REAL | ProductWorkflow / GodProjectArchiveTests |
| AUTOSAVE / RECOVERY | PASS_REAL | Autosave tests |
| COMPOSED GLB EXPORT | PASS_REAL | ComposeScenes tests |
| TRIPOSR | PASS_REAL (CPU workflow) / GATED when missing | separate TripoSR workflow |
| AUTO-RIG SELECTOR | PASS_LOGIC_ONLY | AutoRigProviderSelectorTests (6 passed in autoroot job) |
| AUTO-RIG CPU | GATED_EXTERNAL_RUNTIME | CLI/GGUF absent on hosted runner — not soft PASS |
| AUTO-RIG VULKAN | IMPLEMENTED_GATED_VULKAN_RUNTIME_PROOF | VulkanRig skipped; no silent CPU claim |
| AUTO-RIG NVIDIA CUDA | IMPLEMENTED_GATED_HARDWARE | SkinTokensCuda provider |
| FLUX | GATED_HARDWARE | Release Gate |
| SKINTOKENS OFFICIAL | GATED_HARDWARE | Release Gate |
| LLAMASHARP | PASS_REAL | LLamaSharp CPU GGUF Proof |
| SMART DIAGNOSTICS | PASS_REAL | Diagnose / PipelineTrace tests |
| UNINSTALL | PASS_REAL (smoke) | Velopack uninstall; projects preserved by policy |
| UE5 EXPORT PREP | PARTIAL | FBX/preflight honesty |
| UE5 EDITOR IMPORT | GATED_EXTERNAL_UE5 | |
| ATTACHMENTS | NOT_IMPLEMENTED | honest |
| CREATURE CREATOR | NOT_IMPLEMENTED / backend-only | out of scope |

## UI honesty notes

- RiggingPanel uses `FeatureIds.RigAuto` + provider ComboBox (Automatic/Vulkan/CPU/CUDA).
- MetaHuman remains Unavailable/NotImplemented.
- Attachments/Creatures not expanded in this branch.

## Rejected dependencies

See [AUTORIG_PROVIDER_EVALUATION.md](AUTORIG_PROVIDER_EVALUATION.md).

## Release readiness

Core product flows (build, installer, Anny, Garment) are **PRODUCT_VALIDATED**. Optional GPU/Vulkan/FlaUI/UE5 and Auto-Rig CLI runtime remain honestly gated. **Do not merge until human review.**
