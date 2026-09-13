# Safe Reuse / Replace / Verify — Final Report

## 1. Final commit

**SHA:** `b7c40eebc4b8acef564ccf1370045f163945e70b` (`cursor/safe-reuse-verify-b322`).

See tip of branch `cursor/safe-reuse-verify-b322` after this report is committed (base HEAD was `ee90b6050a9aea251f3554b84fafe308dd5dac85`).

## 2. Baseline (before)

- `dotnet test` Release on Linux agent: **257 passed / 10 failed / 17 skipped / 284 total**
- Failures: Python worker-host tests (no `python` on agent) — environment, not product regressions
- Product: honest foundation per `docs/audit/FINAL_RELEASE_AUDIT.md`

## 3. Original problems addressed

- Fragile remesh API surface (no replaceable backend boundary)
- Freeform skinning = 1 rigid joint/vertex
- Incomplete attachment socket map (tail/hair/beard/prosthetic fell through to chest)
- Broken preview/overlay PNG placeholders (ASCII `.gitinore`)
- `MinimumAppVersion` default **3.0.0** vs product **2.0.0**
- Empty `IImportService` / `IExportService` / `IRiggingService` markers
- Stale README feature claims
- No optional path to reuse already-referenced geometry3Sharp Reducer

## 4. Subsystem matrix (decisions)

See `docs/audit/SAFE_REUSE_SUBSYSTEM_MATRIX.md`. Highlights:

| Area | Decision |
|------|----------|
| Remesh vertex-cluster | **KEEP** default |
| geometry3Sharp Reducer | **WRAP** optional `geometry3sharp-qem` |
| Spherical UV | **KEEP**; xatlas **REJECT** this cycle |
| Freeform weights | **ADAPT** distance multi-bone |
| Auto-rig neural | **WRAP** NotInstalled `IAutoRigBackend` |
| Attachments | **ADAPT** shared `AttachmentSockets` |
| Generative AI garment/mesh edit | **KEEP NotImplemented** (prior research) |
| Preview PNGs | **REPLACE** with valid fictional PNGs |
| Manifest min version | **REPLACE** → 2.0.0 |
| Empty capability markers | **REPLACE** with gated contracts |

## 5. Repositories investigated

| URL | Purpose | Verdict |
|-----|---------|---------|
| https://github.com/gradientspace/geometry3Sharp | Remesh/QEM + already used for fit | **Reused** Reducer behind adapter |
| https://github.com/jpcy/xatlas + EvergineTeam/xatlas.NET | UV atlas | **Rejected** (native packaging cost) |
| https://github.com/Mesh2Motion/mesh2motion-app | Distance skin weights | **Adapted** heuristic only |
| UniRig / SkinTokens.cpp | Auto-rig | **Gated stub only** |
| meshoptimizer / UnityMeshSimplifier / Pinocchio | Remesh/autorig | **Rejected** (native/Unity/LGPL) |
| Prior generative garment / local AI edit candidates | See existing research docs | **Rejected** (unchanged) |

## 6. Reused code

- `g3.Reducer` via `Geometry3SharpQemMeshProcessor` (optional)
- Existing geometry3Sharp package reference in `ThreeDGod.Mesh`

## 7. Existing 3D God code kept

- Remesh vertex-cluster pipeline (default)
- Spherical UV, MeshValidator, SharpGLTF IO
- SemanticBoneMap, humanoid test rig, LBS pose validation
- CommandStack, GodProjectArchive, diagnostics/Cursor report
- Feature gates / hardware profiler
- Procedural jewelry + garment proximity fit
- Deterministic AI allow-list architecture

## 8. Existing code removed / superseded

- Invalid PNG placeholders replaced
- Empty marker interfaces replaced with Probe() contracts
- Freeform single-joint binding superseded by distance solver (same API `WriteSkinned`)

## 9. New custom code

- `IMeshProcessor` / `IUvUnwrapper` / selector (~small adapters)
- `ISkinWeightSolver` + `DistanceSkinWeightSolver`
- `AttachmentSockets`
- `IAutoRigBackend` + NotInstalled stubs
- AI `material.pbr` + longer-tail allow-list entries
- SafeReuse regression tests

Why custom: product-facing boundaries and allow-lists are 3D-God-specific; no suitable drop-in app shell exists.

## 10. Test results (after)

Linux agent Release:

| Metric | Count |
|--------|------:|
| Passed | **266** |
| Failed | **10** (same Python-worker env class as baseline) |
| Skipped | **17** |
| Total | **293** |

SafeReuse + Composition filters: **14/14 passed**.

## 11–18. Workflow results

- **Non-human:** remesh → freeform skinned GLB → horn+tail attachments → `.3dgod` save/load (**test covered**)
- **Rigging/skinning:** multi-bone normalized distance weights (**test covered**); neural auto-rig remains NotInstalled
- **Attachments:** shared sockets for tail/horn/hair/etc.
- **Materials:** deterministic “gold less shiny” → metallic/roughness/color (**test covered**)
- **Remesh:** default cluster unchanged; QEM optional backend validated
- **`.3dgod`:** roundtrip with attachments (**test covered**); min app version 2.0.0
- **GLB:** freeform WriteSkinned still emits skinned GLB
- **UI:** no WPF rewrite; Problems Copy details unchanged; dual-stack adapter kept

## 19. UI result

No Blender-like expansion. README now points at audits. Capability stubs honest.

## 20. Build result

`ThreeDGod.*` + test project compile Release on Linux. WPF app not executed (Windows-only).

## 21. Remaining limitations

- Python worker tests still need `python` on PATH / Windows CI
- QEM not default until skinned corpus bake-off
- xatlas not shipped
- Auto-rig / Anny / GarmentCode / CUDA remain gated
- Anatomical height morph still NotImplemented (no fake scale)
- Dual Legacy Core + V3 Domain still coexist (adapter kept; no big-bang UI migrate)
