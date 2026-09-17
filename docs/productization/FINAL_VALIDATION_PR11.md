# PR #11 Final Validation

| Field | Value |
|-------|-------|
| Starting SHA | `f48800839e1354e9f51846c17fcc455276f0b872` |
| Final SHA | *(see tip after push)* |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/11 |
| skin-tokens.cpp | `localai-org/skin-tokens.cpp@43e885af2eadee9c40aa85849b71528d1c958293` (Apache-2.0) |
| Models | `LocalAI-io/SkinTokens-GGUF` F16 (`mesh-encoder` / `skin-vae` / `tokenrig` GGUF; MIT-labelled) |
| License | Apache-2.0 CLI + MIT-labelled GGUF; `MODEL_LICENSES.json` id `skintokens-cpp` |

## What changed in this validation pass

1. **REAL skin-tokens.cpp CPU** — CI job `Auto-Rig CPU` provisions CLI (CMake CPU build, Vulkan OFF) + F16 GGUF into `%LocalAppData%/3DGod/...`, runs production `AutoRigProviderSelector` / `SkinTokensCppRuntime`, requires `autoroot-cpu-proof.json` (no soft skip when `THREEDGOD_CI_AUTOROOT=1`).
2. **Installed product E2E** — Velopack Setup.exe install → content root = `current/` workers → UvProvisioner Anny/Garment + SkinTokensCppProvisioner → human → garment fit → Auto-Rig CPU → save/reopen → composed export → uninstall. No developer checkout for worker runtime.
3. **FlaUI / GUI** — Hosted Windows remains `GATED_INTERACTIVE_WINDOWS` (no interactive desktop). Self-hosted `runtime-flaui.yml` unchanged.
4. **PRODUCT_VALIDATED** — Requires Build SUCCESS + installer CI_VERIFIED + Anny/Garment CI_VERIFIED|PASS_REAL + **Auto-Rig CPU PASS_REAL** + **Installed Product E2E PASS_REAL**. Missing Auto-Rig CLI is **not** treated as an acceptable external hardware gate.

## Provider path

```
skintokens-cli rig <MODEL_DIR=…/Models/skintokens-cpp/F16> <in.glb> <out.glb> --device cpu
```

Provenance string: `skintokens-cpp@43e885af…;device=cpu`

## Classification (target after green CI)

| Capability | Classification |
|------------|----------------|
| INSTALLER | PASS_REAL |
| CLEAN INSTALL | PASS_REAL |
| FIRST RUN | PASS_REAL (smoke) |
| AUTOMATIC COMPONENT INSTALL | PASS_REAL (uv + skintokens-cpp provisioner) |
| ANNY | PASS_REAL |
| GARMENTCODE | PASS_REAL |
| GARMENT FIT | PASS_REAL |
| BODY + JACKET | PASS_REAL |
| AUTO-RIG SELECTOR | PASS_LOGIC_ONLY (+ real CPU via selector in CI) |
| AUTO-RIG CPU | PASS_REAL (required) |
| AUTO-RIG VULKAN | GATED_EXTERNAL_HARDWARE |
| AUTO-RIG CUDA | GATED_EXTERNAL_HARDWARE |
| SAVE/REOPEN | PASS_REAL |
| AUTOSAVE/RECOVERY | PASS_REAL |
| COMPOSED EXPORT | PASS_REAL |
| INSTALLED APP GUI | GATED_INTERACTIVE_WINDOWS |
| FLAUI | GATED_INTERACTIVE_WINDOWS |
| FLUX | GATED_EXTERNAL_HARDWARE |
| UE5 | GATED_EXTERNAL_HARDWARE |
| UNINSTALL | PASS_REAL |
| PRODUCT_VALIDATED | True only with Auto-Rig CPU + installed E2E PASS_REAL |

## Previous overclaim corrected

Earlier tip claimed `PRODUCT_VALIDATED: True` while Auto-Rig CPU was only `GATED_EXTERNAL_RUNTIME` (selector/logic). That gate condition is removed: Auto-Rig CPU must be `PASS_REAL`.
