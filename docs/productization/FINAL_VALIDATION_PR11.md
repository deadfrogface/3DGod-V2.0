# PR #11 Final Validation

| Field | Value |
|-------|-------|
| Starting SHA | `f48800839e1354e9f51846c17fcc455276f0b872` |
| Final SHA | `b2bef15ca9ff490a430bc60cea655481933b05ef` |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/11 |
| Evidence CI (PR) | run [`35169173164`](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/35169173164) — **7/7 jobs success** (14 checks) |
| skin-tokens.cpp | `localai-org/skin-tokens.cpp@43e885af2eadee9c40aa85849b71528d1c958293` (Apache-2.0) |
| Models | `LocalAI-io/SkinTokens-GGUF` F16 (`mesh-encoder` / `skin-vae` / `tokenrig` GGUF; MIT-labelled) |
| License | Apache-2.0 CLI + MIT-labelled GGUF; `MODEL_LICENSES.json` id `skintokens-cpp` |
| PRODUCT_VALIDATED | **True** (`release-gate-classification.json`) |

## What was validated

1. **REAL skin-tokens.cpp CPU** — Job `Auto-Rig CPU (skin-tokens.cpp)` provisioned CLI (clang-cl + Ninja, Vulkan OFF, `/EHsc` + `_CRT_SECURE_NO_WARNINGS`) + F16 GGUF into `%LocalAppData%/3DGod/...`, ran production `AutoRigProviderSelector` / `SkinTokensCppRuntime`, wrote `autoroot-cpu-proof.json`. Soft-skip forbidden when `THREEDGOD_CI_AUTOROOT=1`.
2. **Installed product E2E** — Velopack `ThreeDGodCreator-win-Setup.exe` → install root → `current/` workers as `THREEDGOD_CONTENT_ROOT` → UvProvisioner Anny/Garment + SkinTokensCppProvisioner → human → garment fit → Auto-Rig CPU → save/reopen → composed export (`meshCount=2`, `garmentCount=1`) → `Update.exe uninstall --silent`. No developer checkout required for worker runtime.
3. **FlaUI / GUI** — Hosted Windows = `GATED_INTERACTIVE_WINDOWS` (artifact `flaui-classification.txt`). No fake visual proof from backend state.
4. **PRODUCT_VALIDATED** — Requires Build SUCCESS + installer CI_VERIFIED + Anny/Garment CI_VERIFIED|PASS_REAL + **Auto-Rig CPU PASS_REAL** + **Installed Product E2E PASS_REAL**. Missing Auto-Rig CLI is **not** an acceptable external hardware gate.

## skin-tokens.cpp CPU evidence (run 35169173164)

| Item | Value |
|------|-------|
| Upstream commit | `43e885af2eadee9c40aa85849b71528d1c958293` |
| Provider | `skintokens-cpp-cpu` |
| Provenance | `skintokens-cpp@43e885af2eadee9c40aa85849b71528d1c958293;device=cpu` |
| Device | `cpu` |
| Duration | **134474 ms** (~2 m 14 s inference in test) |
| Model repo | https://huggingface.co/LocalAI-io/SkinTokens-GGUF (F16) |
| Input | unrigged GLB **1076** bytes |
| Output | rigged GLB **4180** bytes (differs from input) |
| Structure | `skinCount=1`, `jointCount=4`, `vertexCount=8` (RigValidator + SharpGLTF) |
| CLI path | `%LocalAppData%\3DGod\Components\skintokens-cpp\bin\skintokens-cli.exe` |
| Command shape | `skintokens-cli rig <MODEL_DIR=…/Models/skintokens-cpp/F16> <in.glb> <out.glb> --device cpu` |
| Artifacts | `autoroot-cpu-proof.json`, `autoroot-input-unrigged.glb`, `autoroot-output-rigged.glb`, `skintokens-cli.exe` |

## Installed-product evidence

| Item | Value |
|------|-------|
| Installer | `artifacts/releases/stable/ThreeDGodCreator-win-Setup.exe` (Velopack) |
| Content root | `D:\a\_temp\3dgod-installed-e2e\current` |
| Test | `InstalledProductE2ETests.InstalledTree_AnnyGarment_SaveReopen_Compose_AndAutoRigCpu` **Passed** (23 m 53 s) |
| Proof JSON | `{"ok":true,"autoRig":true,"meshCount":2,"garmentCount":1}` |
| Composed export | `installed-composed.glb` (artifact) |
| FlaUI | `GATED_INTERACTIVE_WINDOWS` |
| Uninstall | post-E2E `Update.exe uninstall --silent` + separate Clean Install Smoke uninstall |

## Classification (actual, tip `9155e37` / CI 35169173164)

| Capability | Classification |
|------------|----------------|
| INSTALLER | PASS_REAL |
| CLEAN INSTALL | PASS_REAL |
| FIRST RUN | PASS_REAL (smoke `--smoke-test`; interactive UI gated) |
| AUTOMATIC COMPONENT INSTALL | PASS_REAL (uv Anny/Garment + skintokens-cpp provisioner) |
| ANNY | PASS_REAL |
| GARMENTCODE | PASS_REAL |
| GARMENT FIT | PASS_REAL |
| BODY + JACKET | PASS_REAL (meshCount≥2, garment instance persisted) |
| AUTO-RIG SELECTOR | PASS_LOGIC_ONLY (+ real CPU via selector in CI) |
| AUTO-RIG CPU | **PASS_REAL** |
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
| PRODUCT_VALIDATED | **True** |

## Previous overclaim corrected

Earlier tip `f488008` / `0c3573a` claimed `PRODUCT_VALIDATED: True` while Auto-Rig CPU was only `GATED_EXTERNAL_RUNTIME` (selector/logic). Gate now requires Auto-Rig CPU **PASS_REAL** + Installed Product E2E **PASS_REAL**. Both proven on CI run 35169173164.

## Remaining genuine external gates

- Auto-Rig Vulkan (real AMD/Vulkan GPU)
- Official SkinTokens CUDA (≥14GB VRAM)
- FLUX GPU
- UE5 Editor import
- FlaUI / interactive Windows visual proof (hosted = no desktop)

## Merge

**Do not merge** until human external review. Recommendation: `READY_FOR_EXTERNAL_REVIEW`.
