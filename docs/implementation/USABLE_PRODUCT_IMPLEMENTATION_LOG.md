# USABLE PRODUCT IMPLEMENTATION LOG

| Field | Value |
|-------|-------|
| Repo | https://github.com/deadfrogface/3DGod-V2.0 |
| Baseline SHA (CURRENT main tip) | `68fe72f1152a417c9f27d0acb00538eb09cddedd` |
| Baseline note | Includes merged PR #9 `docs/audit/FINAL_PRODUCT_CAPABILITY_AUDIT.md` |
| Implementation branch | `cursor/make-product-usable-b322` |
| Agent host | Linux (no interactive Windows WPF / no UE5 / GPU optional) |
| Strategy | REUSE > ADAPT > WRAP > PORT > NEW — wire existing backends into one product path |

## Stage status legend

| Label | Meaning |
|-------|---------|
| `PASS_REAL` | Implemented + build/test proof on this agent (or CI) with real artifacts |
| `PARTIAL` | Real progress; important hop still missing or gated |
| `GATED_EXTERNAL` | Code complete; needs worker/uv/Blender/UE5/install |
| `GATED_HARDWARE` | Code complete; needs CUDA/VRAM |
| `GATED_INTERACTIVE` | Needs Windows desktop / FlaUI |
| `FAIL` | Intended path broken |

## Baseline build / test (Stage 0)

| Check | Result |
|-------|--------|
| `dotnet build -c Release -p:EnableWindowsTargeting=true` | **PASS** (0 warn / 0 err) |
| GodProjectArchive + Autosave + Composition filter | **19/19 PASS** |
| Full Core.Tests | **302 passed**, 11 failed, 17 skipped |
| Failures (env, not product regressions) | WorkerProcessHost / SecurityHardening / WorkerSmartDiagnostics need `python` on PATH (only `python3` present); ImageTo3D NotInstalled message mismatch when partial runtime present |
| Interactive WPF | `GATED_INTERACTIVE` |
| UE5 / FlaUI / GPU FLUX live | External gates |

## Stage tracker

| Stage | Focus | Status | Notes / proof |
|-------|-------|--------|---------------|
| 0 | Baseline + log + branch from main | `PASS_REAL` | SHA above; audit present |
| 1 | Authoritative ActiveProjectSession | pending | |
| 2 | Embed mesh bytes in `.3dgod` archive | pending | |
| 3 | Wire New/Open/Save/Export to session | pending | |
| 4 | Autosave + recovery prompt | pending | |
| 5 | Kill Form dual-path / height lie for Human | pending | |
| 6 | Anny Human Creator primary UX | pending | |
| 7 | Clothing → GarmentFitService E2E | pending | |
| 8 | Materials → project state | pending | |
| 9 | Project-state GLB export | pending | |
| 10 | Undo beyond Anny (session-aware) | pending | |
| 11 | Image→3D UI | pending | |
| 12 | FLUX / Text→Image honest generate | pending | |
| 13 | SkinTokens Rigging UI + gates | pending | |
| 14 | Setup Assistant honesty | pending | |
| 15 | UE5 honest export path | pending | |
| 16 | AI edit allowlisted execution | pending | |
| 17 | Attachments honest gates | pending | |
| 18–26 | P2 creature/remesh/UV (non-blocking) | pending | |
| 27 | Headless product workflow integration test | pending | |
| 28–33 | CI preserve + polish | pending | |
| 34 | Post-usability audit + report + PR | pending | |

## External gates (do not soft-pass)

- No interactive Windows desktop → WPF click proofs `GATED_INTERACTIVE`
- No UE5 install → editor import `GATED_EXTERNAL`
- FLUX/SkinTokens GPU → `GATED_HARDWARE` when probe says so
- Anny/GarmentCode/TripoSR without uv install → honest NotInstalled

## PASS_REAL proofs (append as stages complete)

_(none yet beyond Stage 0 baseline)_
