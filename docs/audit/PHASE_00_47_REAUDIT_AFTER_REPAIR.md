# PHASE 00–47 SECOND RE-AUDIT (AFTER REPAIR)

- Date: 2026-09-09
- Repo: `C:\Users\damia\Desktop\3DGod-V2.0`
- HEAD: `46c96c57c5799da1d842bad6aeaa60fefdac4c07` (`46c96c5` phase-20-32 honesty)
- Branch: `main` (tracks `origin/main`)
- Build: `dotnet build -c Release` → **0 errors, 0 warnings**
- Spot tests (repair/honesty filters): **72 passed, 0 failed**
- Full suite: `dotnet test 3DGodCreator.Core.Tests -c Release` → **223 passed, 0 failed, 0 skipped** (~1 m 28 s)
- Scope: independent second re-audit of PHASE 00–47 only. PHASE 48+ not started. `stash@{0}: wip-phase-48-pre-audit-repair` left untouched.

## Summary verdict

**May continue to PHASE 48? YES**

No remaining **repairable FAIL** or **repairable PARTIAL** in code for 00–47 after the five repair commits. Prior FAIL (11A, 17A) and prior PARTIAL honesty gaps (03, 15, 16, 17/17A, Clothing/Physics labels, Assimp/LOD/Anny soft-skips, PBR apply, Llama “complete”) are fixed or correctly **GATED_***.  
`GlbExportService` still uses `File.Copy` — noted as **out of 00–47 scope** (PHASE 48 export completeness / stashed WIP).

---

## Checkpoint verification (requested)

| # | Check | Result |
| --- | --- | --- |
| 1 | PipelineTrace still wired (11A) | **PASS** – production call sites in Remesh, Freeform, AI asset, ImageTo3D, ReferenceImage, Obj/Assimp import, GarmentCode/Anny, GarmentFit/Skin, RigValidation, GlbExport, GodProjectArchive |
| 2 | HelixViewportSession + ProblemsPanel Show object (17/17A) | **PASS** – `MainWindow` constructs `HelixViewportSession`, wires `ProblemsPanel(..., ShowDiagnosticIssueInViewport, ClearDiagnosticHighlight)`; highlight resolves real verts/tris + `UnavailableReason` |
| 3 | LaunchAutoRig no longer → LaunchSculpt (03) | **PASS** – `LegacyBlenderBackend.LaunchAutoRig` reports NotImplemented; never calls Sculpt; test asserts source + behavior |
| 4 | AssimpImportGate NotInstalled for FBX (15) | **GATED_NOT_INSTALLED** – `Status = "NotInstalled"`; non-OBJ throws; OBJ via built-in parser |
| 5 | LodService.BuildLodMesh exists (16) | **PASS** – remesh profiles; `EstimateTriangleBudget` labeled arithmetic-only |
| 6 | TestGate.cs in Anny/Garment soft paths | **PASS** – soft live paths use `TestGate.NotInstalled` / `ExternalDependency` (no silent `Assert.True(true)` orphans) |
| 7 | MaterialEditor PBR apply path | **PASS** – sliders/presets → `SetMaterialPbr` → `ApplyMaterialOverrides` / GLB metallic-roughness (`AssetPipelineTests`) |
| 8 | ClothingPanel / PhysicsPanel labels | **PASS** – Unavailable/NotImplemented/Experimental honesty; cloth/softbody not sold |
| 9 | GlbExportService still File.Copy? | **Yes** – verified copy with SharpGLTF load/vertex check; **out of 00–47** (PHASE 48) |
| 10 | PHASE_*_REPORT.md vs code | Stale notes below; repaired reports for 03/11A/15/16/17/17A/20–32 match code |

---

## Phase table 00–47

Status legend: **PASS** = acceptance met with honest claims; **GATED_*** = intentional external/license/hardware/NotImplemented gate (not a repairable failure); **PARTIAL** / **FAIL** = none remaining in this pass.

| Phase | Status | One-line evidence |
| --- | --- | --- |
| 00 | PASS | Baseline audit + smoke tests; `V2_FEATURE_AUDIT.md` present |
| 01 | PASS | net10.0 / net10.0-windows Release build green |
| 02 | PASS | Layered projects + `ThreeDGodComposition` DI |
| 03 | PASS | AutoRig ≠ Sculpt; honest NotImplemented on legacy host |
| 04 | PASS | Feature gate + UI disables; dynamic overrides stay non-fake |
| 05 | PASS | Domain model in `ThreeDGod.Core` |
| 06 | PASS | `.3dgod` ZIP save/load + PipelineTrace Project.* |
| 07 | PASS | Autosave/recovery paths present |
| 08 | PASS | Undo/redo command stack |
| 09 | PASS | Logging / crash paths |
| 09A | PASS | Smart Diagnostics core (`DiagnosticService`, scene refs) |
| 09B | PASS | Problems panel + copy error/details |
| 10 | PASS | Worker protocol surfaces |
| 10A | PASS | Worker diagnostics integration |
| 11 | PASS | Backend registry / router |
| 11A | PASS | Real `PipelineTrace` breadcrumbs from production paths |
| 12 | PASS | Hardware profiler / GPU queue scaffolding |
| 13 | PASS | Model manager |
| 14 | PASS | SharpGLTF canonical pipeline |
| 15 | GATED_NOT_INSTALLED | Assimp native FBX/DAE NotInstalled; OBJ PASS via `ObjImporter` |
| 16 | PASS | Validator + `BuildLodMesh`; budget helper arithmetic-only |
| 17 | PASS | Helix hit-test → `ViewportSelectionService` → DomainObjectId |
| 17A | PASS | Show object: real highlight/focus, not Active-only stub |
| 18 | PASS | Simple UI shell |
| 19 | PASS | AvalonDock optional / layout reset |
| 20 | GATED_NOT_INSTALLED | Worker/export/gates real; live Anny generate soft-gated via TestGate when uv missing |
| 21 | GATED_NOT_INSTALLED | Param state/undo real; live catalog/vertex needs Anny |
| 22 | GATED_NOT_INSTALLED | Preset model real; mesh apply needs Anny |
| 23 | GATED_NOT_IMPLEMENTED | Deterministic parser Available; LLamaSharp not invocable / no verified inference |
| 24 | PASS | Deterministic AI edit executor + honest HeightMorph gate |
| 25 | GATED_NOT_INSTALLED | Import/probe/persist; FLUX/Qwen generate gated |
| 26 | GATED_NOT_INSTALLED | TripoSR generate gated; attach/persist real |
| 27 | GATED_NOT_INSTALLED | SF3D output gated; license/HW router real |
| 28 | GATED_NOT_INSTALLED | SPAR3D mesh gated; profile/router real |
| 29 | GATED_NOT_INSTALLED | TRELLIS runtime gated; registry/router real |
| 30 | GATED_NOT_INSTALLED | Procedural catalog PASS; FLUX/TripoSR gated |
| 31 | PASS | Kinematic/attachment math; not sold as live cloth/rig viewport |
| 32 | PASS | Catalog PBR editor → viewport + GLB; no AI text-to-PBR claim |
| 33 | PASS | In-process remesh/UVs; native instant-meshes/xatlas not claimed |
| 34 | GATED_UNSUPPORTED_HARDWARE | SkinTokens honest gate; no fake rigged GLB |
| 35 | PASS | Rig validator + LBS test poses |
| 36 | PASS | Creature body plan / modular parts |
| 37 | PASS | Ork catalog path (non-fake mesh) |
| 38 | PASS | Humanoid rat catalog path |
| 39 | PASS | Creature text edits via catalog ops |
| 40 | GATED_NOT_INSTALLED | Freeform catalog+remesh+authored rig Experimental; FLUX/TripoSR/SkinTokens gated |
| 41 | PASS | Parametric garment templates; sleeve length changes mesh |
| 42 | GATED_NOT_INSTALLED | PyGarment jacket worker when uv/lock present; else NotInstalled; no Warp bundle |
| 43 | GATED_NOT_INSTALLED | geometry3Sharp fit Experimental when GarmentCode present; no cloth sim |
| 44 | GATED_NOT_INSTALLED | Nearest-vertex garment skin Experimental; live pose needs runtimes |
| 45 | GATED_NOT_IMPLEMENTED | Generative garment decision: NotImplemented, no fake button |
| 46 | PASS | Bepu rigid chain/earring preview Experimental; UI cloth/softbody disabled |
| 47 | GATED_NOT_IMPLEMENTED | Local AI mesh edit NotImplemented until Anny GLB PoC |

---

## Remaining PARTIAL (repairable)

**None** in code for PHASE 00–47.

Doc-only staleness (not counted as product PARTIAL):

- `PHASE_41_REPORT.md` / `PHASE_42_REPORT.md` still say ``clothing.fit` bleibt NotImplemented`` — historically true at those phase cuts; **current code** (`DynamicFeatureAvailabilityService`) sets `clothing.fit` to **Experimental** when GarmentCode is Available/Experimental (PHASE 43+).
- `PHASE_04_REPORT.md` static catalog text still lists Clothing/Physics as blanket NotImplemented; dynamic service supersedes.

---

## Remaining GATED (exact reasons)

| Gate | Reason |
| --- | --- |
| Assimp FBX/DAE (15) | Native Assimp not shipped/verified; `AssimpImportGate.Status = NotInstalled` |
| Anny live generate/catalog/presets (20–22) | Requires `uv` + Anny worker/runtime; TestGate soft-skip when missing |
| LLamaSharp inference (23) | Package present; no verified prompt→AiEditPlan mapping; feature **NotImplemented** / not invocable |
| FLUX/Qwen reference generate (25) | Runtime NotInstalled |
| TripoSR / SF3D / SPAR3D / TRELLIS (26–29) | Workers/runtimes NotInstalled or HW/license blocked |
| AI asset true generative path (30, 40) | Catalog fallback only; FLUX/TripoSR NotInstalled |
| SkinTokens (34) | CUDA / VRAM / checkpoint; UnsupportedHardware or NotInstalled; writes no GLB |
| GarmentCode / fit / skin live (42–44) | Python uv worker + assets; TestGate when missing |
| Generative garment (45) | Product decision NotImplemented (license/CUDA) |
| Local AI mesh edit (47) | No Anny GLB PoC; NotImplemented |
| Legacy Blender Auto-Rig (03) | NotImplemented on host (honest; not a soft PASS) |
| MetaHuman / Unreal pipeline / Height morph / piercings-tattoos / cloth softbody | Feature gates NotImplemented; UI disabled or labeled |

---

## Remaining FAIL

**None.**

Prior FAIL 11A (breadcrumbs not from production) and FAIL 17A (Active-only / unwired Show object) are repaired on HEAD.

---

## UI honesty notes

- **ClothingPanel**: Fit button reflects FeatureAvailability; piercings/tattoos Unavailable + NotImplemented; no fake load success.
- **PhysicsPanel**: Softbody/cloth checkboxes disabled and labeled NotImplemented; status text clarifies Bepu accessory ≠ cloth.
- **RiggingPanel**: Auto-Rig Unavailable / NotImplemented; does not launch Sculpt.
- **MaterialEditor**: Real PBR apply to character materials + viewport override path.
- **Export GLB**: Still byte-copy of verified source (`File.Copy`); status message admits copy — completeness is PHASE 48.
- **LLama**: UI/`FeatureIds.AiLlamaSharp` not invocable; deterministic parser is the only valid plan producer. Residual: `LlamaSharpProvider.Probe()` Experimental string still says “inference is wired” when a GGUF exists, but `Interpret` does not run model inference and feature status is forced to NotImplemented — not elevated to PARTIAL because product surface is honest.
- **Assimp**: Not sold as full PASS; FBX path fails with NotInstalled.

---

## Smart Diagnostics coverage notes

- `PipelineTrace` emits Started/Completed/Failed/Fallback with provider on Import, Human, Garment, Mesh, Rig, AI, Project, Export.
- Issues carry `LastSuccessfulStage` / `FailingStage` / breadcrumbs for Cursor reports.
- Viewport diagnostics: `DiagnosticSceneRef` → `ViewportDiagnosticHighlight.Show` resolves vertex/triangle IDs against loaded mesh; ProblemsPanel **Betroffenes Objekt anzeigen** calls into `HelixViewportSession.ShowDiagnostic`.
- Unresolvable targets set `UnavailableReason` (no fake Active success).

---

## Tests requiring external runtime / hardware

| Area | Gate used | Dependency |
| --- | --- | --- |
| AnnyRuntime / AnnyLive / AnnyPreset | `TestGate.NotInstalled` / `ExternalDependency` | Anny uv + worker |
| GarmentCode / GarmentFit / GarmentSkin | `TestGate.NotInstalled` | GarmentCode uv + pygarment |
| LegacyBlender headless smoke | `TestGate.NotInstalled` | Blender binary |
| ImageTo3D attach path | `TestGate.ExternalDependency` | `male_base.glb` / TripoSR |
| SkinTokens live rig | Probe UnsupportedHardware / NotInstalled | CUDA ≥14 GB + checkpoint |
| Heavy gated workers (FLUX, SF3D, SPAR3D, TRELLIS, …) | Catalog probe never Available | Model/runtime install |

These soft-gates keep `dotnet test` green without claiming live PASS.

---

## Repair commits (`git log --oneline d4b2664..HEAD`)

```
46c96c5 phase-20-32: honest Anny/AI gates and real catalog PBR editing
2eaf6a6 phase-15/16+ui: honest Assimp gate, real LOD mesh, soft-skip TestGate
e9c15e1 phase-03: stop AutoRig from launching Sculpt
d3b9bfb phase-17a+17: wire Helix selection and real diagnostic viewport focus
cc56c97 phase-11a: emit real pipeline breadcrumbs from production paths
```

Preceding HEAD baseline for the range start: `d4b2664 phase-47: keep local AI mesh edit NotImplemented until an Anny GLB PoC exists`.

Recent context (`git log -8 --oneline`):

```
46c96c5 phase-20-32: honest Anny/AI gates and real catalog PBR editing
2eaf6a6 phase-15/16+ui: honest Assimp gate, real LOD mesh, soft-skip TestGate
e9c15e1 phase-03: stop AutoRig from launching Sculpt
d3b9bfb phase-17a+17: wire Helix selection and real diagnostic viewport focus
cc56c97 phase-11a: emit real pipeline breadcrumbs from production paths
d4b2664 phase-47: keep local AI mesh edit NotImplemented until an Anny GLB PoC exists
b9cc3b0 phase-46: add Bepu rigid accessory chain and earring preview
0c93fc9 phase-45: reject generative garment providers until licenses and CUDA are product-clear
```

---

## Out of scope / next

- **PHASE 48**: not started. Stash `wip-phase-48-pre-audit-repair` present.
- **GLB export completeness**: replace/`File.Copy`-only exporter remains for PHASE 48 work.
- Do not treat existing `docs/audit/PHASE_48_REPORT.md` (and later) as validated by this 00–47 re-audit.

---

## Bottom line

| Question | Answer |
| --- | --- |
| Continue to PHASE 48? | **YES** |
| Report path | `docs/audit/PHASE_00_47_REAUDIT_AFTER_REPAIR.md` |
