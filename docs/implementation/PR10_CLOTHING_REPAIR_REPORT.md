# PR #10 Clothing Repair Report

**BASELINE_PR10_SHA:** `c774c0fd60d91e8db95c48730ea4c370524e4e58`  
**REPAIRED_SHA:** `e8928e3323bc7f8c60daba9dbbe5b08d23c704f6`  
**CURRENT_BRANCH:** `cursor/make-product-usable-b322`  
**CURRENT_CI:** **PASS** — push run [35151901022](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/35151901022), PR run [35151905375](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/35151905375) (10/10 including Anny/Garment CPU Integration)

## REPRODUCED_DEFECTS

### 1. Fake garment Stage 27 proof
`ProductWorkflowIntegrationTests` previously used `WriteCompleteSceneStatic` + `AddFittedGarment` as clothing E2E evidence.

This proved **garment persistence only**, not GarmentCode → GarmentFit.

### 2. Jacket replaces body in viewport
`ClothingPanel` called `_loadPreview(result.FittedGlb)` after fit → single-GLB Helix load replaced the body.

### 3. Save/reopen viewport
Bundle retained garment MeshBytes, but reopen used body-only preview path — garments not shown together.

### 4. Clothed GLB export
`ExportActiveGlb` exported only the active body mesh via `GetActiveMeshGlbPathOrMaterialize`.

---

## REPAIR APPLIED

| Area | Change |
|------|--------|
| Test honesty | Renamed fake path → `GarmentPersistence_Roundtrip_PreservesEmbeddedGarmentMesh` (persistence-only) |
| Real clothing E2E | `RealClothingPipelineIntegrationTests.RealGarmentCodeFit_SaveReopen_ComposedExport_ContainsBodyAndJacket` invokes production `GarmentCodeService` + `GarmentFitService` |
| Viewport | `ActiveProjectSession.ListSceneParts` / `MaterializeSceneGlbs` + `MainWindow.RefreshViewportFromProject` (Model3DGroup) |
| Clothing button | Adds garment to session → `RefreshViewportFromProject` (BODY + JACKET) |
| Export | `GlbExportService.ComposeScenes` + `ProductWorkflowService.ExportActiveGlb` multi-node compose |
| Body regen honesty | `SetActiveMeshBytes` marks fitted garments `stale-needs-refit` |
| Anny / TripoSR / Rig | GLB preview callbacks use project scene refresh when session already holds mesh |

## CAPABILITY CLASSIFICATION (post-repair)

| Capability | Status | Evidence |
|------------|--------|----------|
| AUTHORITATIVE PROJECT STATE | **PASS_REAL** | ActiveProjectSession preserved |
| BODY PERSISTENCE | **PASS_REAL** | MeshBytes roundtrip |
| GARMENT PERSISTENCE | **PASS_REAL** | persistence-only test + real pipeline reopen |
| REAL GARMENTCODE GENERATION | **PASS_REAL** | `GarmentCodeTests` + real clothing test on agent; CI gates when worker missing |
| REAL GARMENT FIT | **PASS_REAL** | `GarmentFitTests` + real clothing test (`insideAfter=0`, geometry3Sharp report) |
| BODY + GARMENT PROJECT ROUNDTRIP | **PASS_REAL** | save → destroy session → reopen restores body+garment bytes (no GarmentCode on reopen) |
| BODY + GARMENT COMPOSED VIEWPORT | **IMPLEMENTED_GATED_INTERACTIVE_VISUAL_PROOF** | Headless: `MaterializeSceneGlbs` yields 2 readable meshes; WPF Helix Model3DGroup wired; interactive pixels not proven here |
| BODY + GARMENT COMPOSED EXPORT | **PASS_REAL** | `ComposedExport_*` + real pipeline assert MeshCount≥2, named nodes, triangle sum |
| AUTOSAVE WITH GARMENT | **PASS_REAL** | FlushAutosave + ListRecoveries in persistence + real pipeline tests |
| ANNY REGRESSION | **PASS_REAL** | AnnyInspector → OnAnnyPreviewReady → RefreshViewportFromProject; stale-needs-refit on body change |
| TRIPOSR REGRESSION | **PASS_REAL** (invoke path) | Image→3D still SetActiveMesh + scene refresh; runtime remains gated |

## MATERIALS NOTE

`UpsertMaterial` still stores one Character-level material slot (skin). Garment materials remain inside the fitted garment GLB. Body material is not overwritten by garment fit. No giant material rewrite in this repair.

## FAKE GARMENT TEST

**Renamed to persistence-only** — not counted as clothing E2E.

## OVERCLAIMED REPORTS

Corrected in this file + `MAKE_3D_GOD_USABLE_REPORT.md` + `USABLE_PRODUCT_IMPLEMENTATION_LOG.md` + `POST_USABILITY_PRODUCT_CAPABILITY_AUDIT.md`.
