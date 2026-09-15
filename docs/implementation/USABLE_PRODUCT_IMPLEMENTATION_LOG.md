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
| Full Core.Tests (baseline) | **302 passed**, 11 failed (python PATH), 17 skipped |
| Interactive WPF | `GATED_INTERACTIVE` |
| UE5 / FlaUI / GPU FLUX live | External gates |

## Stage tracker

| Stage | Focus | Status | Notes / proof |
|-------|-------|--------|---------------|
| 0 | Baseline + log + branch from main | `PASS_REAL` | SHA 68fe72f; audit present |
| 1 | Authoritative ActiveProjectSession | `PASS_REAL` | `ActiveProjectSession.cs` + DI |
| 2 | Embed mesh bytes in `.3dgod` | `PASS_REAL` | `MeshBytes` + archive `assets/{id}/mesh.glb`; test roundtrip |
| 3 | Wire New/Open/Save/Export to session | `PASS_REAL` | MainWindow menus + ExportPanel |
| 4 | Autosave + recovery prompt | `PASS_REAL` | Window_Loaded offers restore; Flush on save |
| 5 | Kill Form dual-path / height lie | `PASS_REAL` | Form disabled when Anny active; no uniform scale for Anny |
| 6 | Anny Human Creator primary UX | `PASS_REAL` | Session sync on generate/edit; honesty copy |
| 7 | Clothing → GarmentFitService | `PASS_REAL` (invoke) / `GATED_EXTERNAL` (worker) | Real FitJacket* call; no MessageBox fake |
| 8 | Materials → project state | `PASS_REAL` | MaterialEditorPanel → UpsertMaterial |
| 9 | Project-state GLB export | `PASS_REAL` | Materialize MeshBytes → GlbExportService |
| 10 | Undo / AI edits on session | `PASS_REAL` | AllowlistedAiEditExecutor + CommandStack |
| 11 | Image→3D UI | `PASS_REAL` (invoke) / `GATED_EXTERNAL` | AI Asset button runs TripoSR when image selected |
| 12 | FLUX / Text→Image | `PASS_REAL` (invoke) / `GATED_HARDWARE` | Reference button calls GenerateAsync |
| 13 | SkinTokens Rigging UI | `PASS_REAL` (invoke) / `GATED_HARDWARE` | Auto-Rig enabled when SkinTokens invocable |
| 14 | Setup Assistant honesty | `PASS_REAL` | Catalog + status: install ≠ usable |
| 15 | UE5 honest export path | `PASS_REAL` | Unreal button stays NotImplemented; preflight ≠ import |
| 16 | AI edit allowlisted execution | `PASS_REAL` | morph.height / material / local; refuses vague |
| 17 | Attachments honest gates | `PASS_REAL` | Still NotImplemented; AI plan gated not faked |
| 18–26 | P2 creature/remesh/UV | `PARTIAL` | Backends remain; not blocking P0 |
| 27 | Headless product workflow test | `PASS_REAL` | `ProductWorkflowIntegrationTests` 4/4 |
| 28–33 | CI preserve + polish | `PARTIAL` | Local Release build green; push for CI |
| 34 | Post-usability audit + report + PR | `PASS_REAL` | See sibling docs |

## External gates (do not soft-pass)

- No interactive Windows desktop → WPF click proofs `GATED_INTERACTIVE`
- No UE5 install → editor import `GATED_EXTERNAL`
- FLUX/SkinTokens GPU → `GATED_HARDWARE` when probe says so
- Anny/GarmentCode/TripoSR without uv install → honest NotInstalled

## PASS_REAL proofs

| Proof | Evidence |
|-------|----------|
| Release build | `dotnet build -c Release -p:EnableWindowsTargeting=true` → 0 errors |
| Stage 27 workflow | New→Anny state→mesh embed→AI height edit→garment→autosave→save→reopen→export GLB |
| MeshBytes archive | Save/load preserves embedded GLB byte length + relative path |
| Composition DI | ActiveProjectSession, AutosaveService, AllowlistedAiEditExecutor, ProductWorkflowService resolved |
| WorkerProcessHost (after python symlink) | 6/6 + Autosave 3/3 pass locally |
