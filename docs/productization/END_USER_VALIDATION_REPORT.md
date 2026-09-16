# End-User Validation Report

| Field | Value |
|-------|-------|
| Branch | `cursor/productization-windows-b322` |
| Baseline main | `ae077bfa48a1f9690af223590b2a302af898da7e` |
| Auto-Rig architecture | Multi-provider (cpp CPU/Vulkan + official CUDA) |
| Packaging | Velopack small-core + Setup Assistant |

## Capability matrix

| Capability | Status | Evidence |
|------------|--------|----------|
| WINDOWS INSTALLER | PASS_REAL / CI_VERIFIED | Clean install smoke job |
| FIRST LAUNCH | PASS_REAL (smoke) / GATED_INTERACTIVE (full UI) | InstalledAppSmoke + FlaUI gate |
| SETUP ASSISTANT | PASS_REAL (logic) | Hardware banner, Install/Repair/Remove/Retry/Logs |
| AUTOMATIC DEPENDENCY INSTALL | PARTIAL | UvProvisioner + allowlisted downloads; skintokens-cpp CLI still on-demand |
| AUTOMATIC COMPONENT REPAIR | PASS_REAL (uv path) | WorkerUvComponentInstaller Repair |
| ANNY HUMAN | PASS_REAL | CI Anny CPU |
| ANNY EDIT | PASS_REAL | taller-delta / session sync |
| GARMENTCODE | PASS_REAL | CI |
| GARMENT FIT | PASS_REAL | CI + ProductWorkflow |
| BODY + GARMENT VIEWPORT | IMPLEMENTED_GATED_INTERACTIVE_VISUAL_PROOF | MaterializeSceneGlbs + Helix compose |
| MATERIAL EDIT | PASS_REAL | UpsertMaterial + archive |
| SAVE / REOPEN | PASS_REAL | ProductWorkflow |
| AUTOSAVE / RECOVERY | PASS_REAL | Autosave tests |
| COMPOSED GLB EXPORT | PASS_REAL | ComposeScenes tests |
| TRIPOSR | PASS_REAL (CPU workflow) / GATED when missing | separate TripoSR workflow |
| AUTO-RIG CPU | IMPLEMENTED_GATED_RUNTIME until CLI+GGUF | SkinTokensCppAutoRigTests |
| AUTO-RIG VULKAN | IMPLEMENTED_GATED_VULKAN_RUNTIME_PROOF | no silent CPU claim |
| AUTO-RIG NVIDIA CUDA | IMPLEMENTED_GATED_HARDWARE | SkinTokensCuda provider |
| FLUX | GATED_HARDWARE | existing |
| SKINTOKENS OFFICIAL | GATED_HARDWARE | existing |
| LLAMASHARP | PASS_REAL (CI job) | llamasharp-cpu |
| SMART DIAGNOSTICS | PASS_REAL | Diagnose button + PipelineTrace |
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
