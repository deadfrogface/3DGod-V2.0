# POST USABILITY PRODUCT CAPABILITY AUDIT

| Field | Value |
|-------|-------|
| Repo | https://github.com/deadfrogface/3DGod-V2.0 |
| Prior audit | PR #9 / `docs/audit/FINAL_PRODUCT_CAPABILITY_AUDIT.md` |
| Baseline SHA | `68fe72f1152a417c9f27d0acb00538eb09cddedd` |
| Implementation tip | Branch `cursor/make-product-usable-b322` (PR #10) |
| Clothing repair baseline | `c774c0fd60d91e8db95c48730ea4c370524e4e58` |
| Audit date | 2026-09-16 (updated after clothing repair) |
| Method | Source re-trace + real GarmentCode/Fit + composed export proofs |

---

## Executive verdict

**Improved, not finished.** 3D God has **one authoritative ActiveProjectSession** over `ProjectBundle` / `.3dgod` (embedded mesh bytes, materials, garments, Anny state, autosave recovery). Clothing is no longer MessageBox-only: production GarmentFit runs, BODY+GARMENT compose for viewport/export, and reopen restores embedded fitted meshes without re-running GarmentCode.

Interactive Helix visual pixels remain gated. Fresh-machine worker install remains gated.

**CURRENT PRODUCT VERDICT:** Headless Human + clothing project fidelity is **PASS_REAL** where GarmentCode is installed. Interactive Windows BODY+JACKET visual proof remains **IMPLEMENTED_GATED_INTERACTIVE_VISUAL_PROOF**.

---

## What changed vs prior post-usability audit

| Capability | Prior overclaim | After clothing repair | Label now |
|------------|-----------------|----------------------|-----------|
| Fake Stage-27 jacket | Counted as clothing E2E | Renamed persistence-only | `PASS_REAL` persistence ≠ generation |
| Real GarmentCode/Fit | Not proven in Stage 27 | Production services in `RealClothingPipelineIntegrationTests` | `PASS_REAL` / `GATED_EXTERNAL_RUNTIME` |
| Viewport after fit | Jacket replaced body | `RefreshViewportFromProject` Model3DGroup | Headless data path `PASS_REAL`; visual `GATED_INTERACTIVE` |
| Clothed export | Body-only materialize | `ComposeScenes` multi-node GLB | `PASS_REAL` |
| Reopen clothed project | Bytes OK; viewport incomplete | Reopen → MaterializeSceneGlbs both parts | `PASS_REAL` |
| Authoritative project state | ActiveProjectSession | Unchanged / preserved | `WORKS_END_TO_END` (headless) |
| Autosave with garment | Wired | Proven with garment in recovery list | `PASS_REAL` |
| Materials | Character UpsertMaterial | Unchanged; garment mats in garment GLB | `PASS_REAL` with documented limitation |

---

## Capability matrix (clothing-focused)

| Capability | Classification |
|------------|----------------|
| AUTHORITATIVE PROJECT STATE | `PASS_REAL` |
| BODY PERSISTENCE | `PASS_REAL` |
| GARMENT PERSISTENCE | `PASS_REAL` |
| REAL GARMENTCODE GENERATION | `PASS_REAL` (worker present) / `GATED_EXTERNAL_RUNTIME` |
| REAL GARMENT FIT | `PASS_REAL` (worker present) / `GATED_EXTERNAL_RUNTIME` |
| BODY + GARMENT PROJECT ROUNDTRIP | `PASS_REAL` |
| BODY + GARMENT COMPOSED VIEWPORT | `IMPLEMENTED_GATED_INTERACTIVE_VISUAL_PROOF` |
| BODY + GARMENT COMPOSED EXPORT | `PASS_REAL` |
| AUTOSAVE WITH GARMENT | `PASS_REAL` |
| ANNY REGRESSION | `PASS_REAL` (stale-needs-refit on body change) |
| TRIPOSR REGRESSION | `PASS_REAL` invoke path / runtime gated |

---

## Minimum HUMAN + clothing workflow (code chain)

Launch → New → body (Anny or fixture) → Clothing Jacket Fit → session AddFittedGarment → RefreshViewportFromProject (BODY+JACKET) → save `.3dgod` → close → reopen (embedded bytes; **no** GarmentCode) → Export composed GLB (MeshCount≥2).

| Step | Status |
|------|--------|
| Session New/Open/Save | `PASS_REAL` |
| Real GarmentCode + Fit | `PASS_REAL` when worker installed |
| Composed viewport data | `PASS_REAL` headless |
| Composed Helix pixels | `GATED_INTERACTIVE` |
| Composed export | `PASS_REAL` |

---

## Remaining blockers

1. Interactive Windows visual proof of simultaneous BODY+JACKET Helix render.
2. Fresh Windows install still needs Setup Assistant for Anny/GarmentCode (honest).
3. FLUX / SkinTokens need GPU (`GATED_HARDWARE`).
4. UE5 editor import still external scripts only.
5. P2 creature/freeform/remesh UI still unwired.
6. Attachments (piercings/tattoos) still NotImplemented.
7. Character MaterialSet is still a single skin slot (garment materials live in garment GLB).

---

## Classification reminder

Do **not** conflate: garment persistence ≠ GarmentCode generation ≠ GarmentFit ≠ composed viewport ≠ composed export.
