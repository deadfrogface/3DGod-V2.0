# FINAL PRODUCT CAPABILITY AUDIT

**Question answered:** What can a normal user actually do in 3D God today?

| Field | Value |
|-------|-------|
| Repo | https://github.com/deadfrogface/3DGod-V2.0 |
| Remote confirmed | `origin` → `deadfrogface/3DGod-V2.0` |
| Accepted main HEAD (baseline) | `acf43074719d98dadfd9dfa9a1dd76d568e0ae60` |
| Audit branch | `cursor/final-product-capability-audit-b322` |
| Audit date | 2026-09-14 |
| Agent host | Linux (no interactive Windows WPF) |
| Method | Source-code chain tracing (UI → service → backend → artifact). Docs/PASS labels are **not** treated as user proof. |

---

## 1. Executive Summary

**CURRENT PRODUCT VERDICT:** 3D God today is an incomplete WPF product with a strong gated backend foundation. A normal Windows user can open the app, view unskinned base GLBs, tweak materials/presets/settings, save a thin `.3dgod` (Anny parameters only), and copy-export a viewport GLB. Creating a real editable human, clothing, auto-rig, generative AI character, or UE5-ready pipeline from the UI is **not** a finished end-to-end product experience without installing optional workers **and** often without missing UI wiring even after install.

Previous phase PASS docs and CI greens prove engineering honesty and backend/test coverage. They do **not** prove a shippable creator product for end users.

---

## 2. Audit Scope, Method, and Constraints

### In scope
- `3DGodCreator.App` (XAML + code-behind; **no** MVVM ViewModels — handlers live in panels/`MainWindow`)
- Application feature gates, Infrastructure services, Workers, Persistence, Export, AI, Setup Assistant
- Project archive, autosave registration, remesh/rig/garment/creature pipelines
- Part 20 (Anny human) workflow as the primary “create a person” path

### Out of scope
- Implementing or “fixing” missing features
- Refactors, stubs, product code changes
- Claiming PASS from prior phase reports alone

### Method
1. Read App XAML/menus/panels and follow every click handler.
2. Trace DI (`ThreeDGodComposition`) vs what the App actually injects/calls.
3. Grep for NotImplemented / disabled / CanExecute / stub / fake / placeholder.
4. Classify every capability with **exactly one** label (legend §5).
5. Where WPF cannot run on this Linux agent: mark interactive proof as `UNKNOWN_REQUIRES_REAL_USER_TEST` while still classifying the **code chain**.

### Hard honesty rules
- Registered service ≠ user capability.
- Enabled-looking button that only shows a MessageBox/status string ≠ WORKS_END_TO_END.
- CI worker PASS on Actions ≠ default installed-desktop experience.
- Gated backends that never fake meshes are honest — and still incomplete for users.

---

## 3. Baseline Identity

| Check | Result |
|-------|--------|
| Fetch/checkout tip | `acf43074719d98dadfd9dfa9a1dd76d568e0ae60` (`Merge pull request #8 … triposr-ci-fix`) |
| Branch created | `cursor/final-product-capability-audit-b322` from that tip |
| Remote | `https://github.com/deadfrogface/3DGod-V2.0` |
| Product stack | .NET 10, WPF (`net10.0-windows`), HelixToolkit viewport, SharpGLTF, Velopack entry |

---

## 4. Architecture Map (as shipped)

```
WPF MainWindow / Panels (code-behind)
    → CharacterSystem (legacy in-memory sculpt/anatomy/materials/presets)
    → IFeatureAvailabilityService (DynamicFeatureAvailabilityService)
    → Selected services only:
         AnnyHumanService, IProjectService (GodProjectArchive),
         IFbxExportService, IAssetGenerationService (AiAssetPipeline),
         IComponentManager / SetupAssistant, IDiagnosticService, CommandStack
    → Optional workers (uv): anny, garmentcode, triposr, flux, skintokens
    → Viewport (Helix) / files (.3dgod, presets/*.json, exports/*.fbx, Generated/*.glb)
```

**Critical structural fact:** Many real backends are registered in DI (`IGarmentFitService`, `ISkinTokensRigService`, `ICreatureAssembly`, `IFreeformCharacterPipeline`, `IRemeshService`, `IReferenceImageGenerationService`, `AutosaveService`, …) but **are not called from any App panel**. Users cannot reach them through the normal UI.

There is **no** ViewModel/command layer for most tabs — classification is based on XAML click handlers and injected services.

---

## 5. Classification Legend

| Label | Meaning |
|-------|---------|
| `WORKS_END_TO_END` | Complete UI→…→user-visible result chain exists in code; no missing hop. Interactive Windows proof may still be UNKNOWN. |
| `WORKS_BACKEND_ONLY` | Real service/tests/workers exist; **no** (or no useful) App UI path. |
| `WORKS_UI_ONLY` | UI present; no real backend effect / only state or message. |
| `PARTIAL` | Some hops work; important hops missing, wrong, or incomplete for the marketed job. |
| `IMPLEMENTED_GATED_HARDWARE` | Code complete; needs CUDA/VRAM (etc.). |
| `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` | Code complete; needs Blender/uv/worker/ckpt/UE5/FlaUI/install root. |
| `NOT_IMPLEMENTED` | Explicitly absent or gated NotImplemented. |
| `BROKEN` | Intended path fails / contradicts itself in a way that breaks the job. |
| `MISLEADING_UI` | UI implies capability (enabled control / feature title) but does not invoke the real path. |
| `UNKNOWN_REQUIRES_REAL_USER_TEST` | Cannot prove interactive WPF behavior on this agent; use with a chain classification note. |

---

## 6. What a Normal User Can Do Today (plain language)

Assume a fresh Windows install of the foundation app **without** completing Setup Assistant installs:

| User goal | Reality today |
|-----------|---------------|
| Launch app | Yes (Windows/.NET 10). Setup Assistant may appear; Skip is allowed. |
| See a 3D body | Yes — `male_base.glb` / `female_base.glb` load in Helix. **No skin/armature** → deformation warning; Form sliders disabled. |
| Sculpt / change proportions meaningfully | **No** on base models. Height path in viewport is still uniform scale code (`ApplySculptTransform`). Anny height morph is **not** wired to Form. |
| Create Anny human | Only after Anny worker+uv install; then Menu/AI/Anny tab can generate GLB. Fresh install → disabled Anny inspector + honest NotInstalled messages. |
| Add clothes / piercings / tattoos | Piercings/tattoos: disabled NotImplemented. Clothes button: either disabled or **MessageBox only** (does not call `GarmentFitService`). |
| Auto-rig / MetaHuman | Buttons disabled; NotImplemented. SkinTokens exists in backend/Setup Assistant but **no Rigging UI call**. |
| AI text/image → character | Reference-image button can enable when FLUX probes Experimental, but click **only writes status text** (does not generate). Image→3D has **no** UI generate call. Asset button: procedural “frog necklace” catalog only. |
| Save project | File→Save writes `.3dgod` with **Anny parametric state only** — not full CharacterSystem/materials/viewport mesh bundle. |
| Autosave recovery after crash | Backend+tests exist; **App never wires AutosaveService**. |
| Export GLB | Yes from Menu/Export if a real viewport GLB path exists. |
| Export FBX / Unreal | FBX: Experimental path via Blender if configured. “Export to Unreal” **disabled** (NotImplemented — would be file copy only). |
| Physics / softbody / cloth | Checkboxes disabled; honest labels. Bepu accessory preview is backend/tests only. |

**Bottom line for a normal user:** browse/view foundation mesh + settings + preset JSON + GLB file export. The “character creator” fantasy is mostly gated or backend-only.

---

## 7. Full Capability Matrix

| # | Capability | Chain (short) | Classification |
|---|------------|---------------|----------------|
| C01 | App launch / Velopack hook | `Program.Main` → `App.OnStartup` → `MainWindow` | `WORKS_END_TO_END` (interactive: `UNKNOWN_REQUIRES_REAL_USER_TEST`) |
| C02 | Setup Assistant open / Skip | Menu/Tools + first-run → `SetupAssistantWindow` | `WORKS_END_TO_END` (interactive UNKNOWN) |
| C03 | Setup Assistant Install/Repair workers | UI → `WorkerUvComponentInstaller` → uv sync | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| C04 | Component honesty states | Catalog READY/NOT INSTALLED/… | `WORKS_END_TO_END` (logic); interactive UNKNOWN |
| C05 | Helix viewport rotate/pan/zoom | HelixToolkit input | `WORKS_END_TO_END` (interactive UNKNOWN) |
| C06 | Load base GLB male/female | Form buttons → `LoadBaseModel` → `GlbLoader` | `PARTIAL` — mesh shows; **no skin**; Form sliders then disabled |
| C07 | Viewport left-click selection | `HelixViewportSession` / domain Guid | `PARTIAL` — selection text works; limited product value |
| C08 | Form body sliders (breast/hip/…) | `UpdateSculptValue` → viewport scale + Blender JSON | `PARTIAL` / effectively unusable on shipped bases (unrigged) |
| C09 | Height as anatomical morph | Feature text claims AnnyHeightMorph; Form uses uniform `ScaleTransform3D` | `MISLEADING_UI` (+ `NOT_IMPLEMENTED` for Form path) |
| C10 | Sculpt tab “start sculpting” | `CharacterSystem.Sculpt` → headless Blender script | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` — not in-app sculpt |
| C11 | NSFW anatomy checkboxes | State → PNG preview **only if not showing 3D** | `PARTIAL` — 64×64 PNGs exist; no real anatomy layers on GLB |
| C12 | Clothing load / jacket fit (UI) | ClothingPanel button | `MISLEADING_UI` when enabled (MessageBox only); else `NOT_IMPLEMENTED` / gated |
| C13 | Piercings / tattoos | Disabled buttons | `NOT_IMPLEMENTED` |
| C14 | GarmentCode + proximity fit | `GarmentFitService` (tests/CI) | `WORKS_BACKEND_ONLY` + `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| C15 | Garment templates / skin bind | `IGarmentService` / `IGarmentSkinService` | `WORKS_BACKEND_ONLY` |
| C16 | Physics checkboxes | Disabled softbody/cloth UI | `NOT_IMPLEMENTED` (UI honest) |
| C17 | Bepu accessory chain/earring | `IAccessoryPhysicsService` | `WORKS_BACKEND_ONLY` |
| C18 | Material color/PBR sliders | `SetMaterialPbr` → viewport + optional GLB write | `PARTIAL` — works on loaded mesh; not a full PBR studio |
| C19 | Preset JSON save/load/list/screenshot | `PresetService` + window JPEG | `WORKS_END_TO_END` for **legacy preset state** (interactive UNKNOWN) |
| C20 | Anny catalog + live sliders + generate | AnnyInspector → `AnnyHumanService.GenerateGlbAsync` → viewport | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` (code chain complete when probe Experimental) |
| C21 | Anny menu / AI Anny button | Same generate path | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| C22 | Anny user presets | `AnnyPresetStore` | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` (needs Anny UI enabled) |
| C23 | `.3dgod` save/load | File menu → `GodProjectArchive` | `PARTIAL` — archive real; **payload is Anny params only**; no autosave UI |
| C24 | Autosave / crash recovery | `AutosaveService` registered + tested | `WORKS_BACKEND_ONLY` |
| C25 | Undo/Redo | CommandStack; Anny slider commits | `PARTIAL` — only Anny property commands, not global editor |
| C26 | Auto-Rig UI | Disabled | `NOT_IMPLEMENTED` |
| C27 | MetaHuman export UI | Disabled | `NOT_IMPLEMENTED` |
| C28 | SkinTokens auto-rig | `SkinTokensRigService` + Setup feature | `IMPLEMENTED_GATED_HARDWARE` + **UI missing** → effective `WORKS_BACKEND_ONLY` |
| C29 | Freeform / creature assembly / text edit | Infrastructure services + tests | `WORKS_BACKEND_ONLY` |
| C30 | Remesh / UV | `RemeshService` / mesh processor | `WORKS_BACKEND_ONLY` |
| C31 | AI load image | File dialog only | `WORKS_UI_ONLY` |
| C32 | AI “Referenzbild erzeugen” | Button can enable; click = status string | `MISLEADING_UI` |
| C33 | FLUX reference PNG | `ReferenceImageService` / `FluxService` | `IMPLEMENTED_GATED_HARDWARE` (`WORKS_BACKEND_ONLY` from App) |
| C34 | AI “Erzeuge vollständige Person” | Disabled NotImplemented | `NOT_IMPLEMENTED` |
| C35 | Image→3D TripoSR | `ImageTo3DService` | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` / hardware as probed; **no App generate button** → `WORKS_BACKEND_ONLY` |
| C36 | AI asset generate | `AiAssetPipeline` — frog necklace catalog; else throws | `PARTIAL` |
| C37 | Deterministic AI command parser | `DeterministicAiParser` / executor | `WORKS_BACKEND_ONLY` |
| C38 | LLamaSharp GGUF inference | Probe forced to NotImplemented for product feature id | `NOT_IMPLEMENTED` (product); CI may still run provider tests |
| C39 | Export GLB | Menu/Export → `GlbExportService.Export` | `WORKS_END_TO_END` when viewport has file (interactive UNKNOWN) |
| C40 | Export FBX | ExportPanel → Blender headless | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| C41 | UE5 preflight on export | `UnrealEngine5ExportProfile` | `PARTIAL` — checks only; **not** editor import |
| C42 | Export to Unreal button | Disabled NotImplemented | `NOT_IMPLEMENTED` |
| C43 | Real UE5 editor import | `scripts/ue5/…` | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` (ops/script; not App) |
| C44 | Settings config/theme/language/folders | `ConfigService` + Loc | `WORKS_END_TO_END` (interactive UNKNOWN) |
| C45 | Controller checkbox | Disabled | `NOT_IMPLEMENTED` |
| C46 | Blender test / diagnostics / logs | Settings buttons | `WORKS_END_TO_END` / gated for Blender |
| C47 | Problems panel + copy Cursor report | `IDiagnosticService` | `WORKS_END_TO_END` when issues exist (interactive UNKNOWN) |
| C48 | Debug console F12 / Froggy | Keyword helper | `PARTIAL` — real UI; not smart diagnostics |
| C49 | SF3D/SPAR3D install | Setup rejected | `NOT_IMPLEMENTED` (product-rejected) |
| C50 | FlaUI installed-app tests | UiTests | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| C51 | Generative garment / local AI mesh edit | Feature ids NotImplemented | `NOT_IMPLEMENTED` |
| C52 | Assimp/FBX import | `NotInstalledImportService` | `NOT_IMPLEMENTED` / NotInstalled stub |

---

## 8. App Shell, Menus, Lifecycle

| Item | Evidence | Classification |
|------|----------|----------------|
| File New | Resets Anny inspector state in memory; `generate:false` | `PARTIAL` — does not clear CharacterSystem/viewport fully |
| File Open `.3dgod` | `GodProjectArchive.LoadAsync` → Anny `ApplyStateAsync` | `PARTIAL` |
| File Save `.3dgod` | Bundle with one `CharacterDocument` + `ParametricHumanState` | `PARTIAL` |
| File Generate Anny | Probe gate → `GenerateGlbAsync` → `LoadPreview` | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| File Export GLB | Requires `_currentPreviewPath` file | `WORKS_END_TO_END` (when path set) |
| Edit Undo/Redo | `ApplicationCommands` + `CommandStack` | `PARTIAL` |
| Tools Setup Assistant | Dialog | `WORKS_END_TO_END` |
| Unhandled exceptions | Logged; dispatcher `Handled=true` | `PARTIAL` — stability yes; silent swallow risk |
| Autosave on dirty project | **Not referenced by App** | `WORKS_BACKEND_ONLY` |

---

## 9. Viewport & Preview

| Item | Evidence | Classification |
|------|----------|----------------|
| GLB load via SharpGLTF/`GlbLoader` | Real mesh display | `WORKS_END_TO_END` |
| Base assets on disk | `male_base.glb` ~1.7MB, `female_base.glb` ~1.8MB; **no SKIN/JOINTS/WEIGHTS** chunks | `PARTIAL` |
| Rig warning MessageBox | Shown when unskinned | Honest `PARTIAL` |
| OBJ/3DS/STL via Helix importer | Code path exists | `PARTIAL` / rarely used |
| Anatomy PNG fallback | Real 64×64 PNGs (not the old 9-byte placeholders) | `PARTIAL` — tiny placeholders still not production anatomy |
| Uniform height scale | `ApplySculptTransform` scales X=Y=Z from `height` | `MISLEADING_UI` vs FeatureIds.HeightMorph messaging |

Interactive FPS/feel: `UNKNOWN_REQUIRES_REAL_USER_TEST`.

---

## 10. Project Save / Load / Undo / Recovery

### `.3dgod` archive (backend)
`GodProjectArchive` writes ZIP with manifest, SHA checks, path rules, migrations — unit tests pass on this agent (**9/9** `GodProjectArchiveTests`).

### What File→Save actually persists
Only Anny `ParametricHumanState` inside one character document. It does **not** persist:
- CharacterSystem sculpt/anatomy/physics/materials
- Viewport GLB bytes / Generated mesh path
- Garments, creatures, reference images, rigs (even though archive format supports many of these)

### Open
Restores Anny state; regenerates mesh only if Anny is invocable.

### Autosave
`AutosaveService` in DI + `AutosaveServiceTests` — **zero App call sites**. Crash recovery is invisible to users.

| Sub-capability | Classification |
|----------------|----------------|
| Archive format correctness | `WORKS_BACKEND_ONLY` / strong |
| User Save/Open for Anny params | `PARTIAL` |
| Full project fidelity | `NOT_IMPLEMENTED` |
| Autosave recovery UX | `WORKS_BACKEND_ONLY` |
| Undo beyond Anny sliders | `NOT_IMPLEMENTED` |

---

## 11. Anny / Parametric Human (Part 20 core)

| Hop | Status |
|-----|--------|
| Worker scripts + `uv.lock` in repo | Present (`workers/anny`) |
| Probe Experimental when uv + lock/venv | Code + `AnnyRuntimeTests` (5 passed; NotInstalled generate test skipped because runtime present on agent) |
| UI gate disables inspector if not invocable | `AnnyInspectorPanel` ctor |
| Catalog → sliders → debounced `GenerateGlbAsync` → `LoadPreview` | Wired |
| Menu + AI Anny button | Wired |
| Undo for parameter commits | Wired to `CommandStack` |

**Classification:** `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` for the generate chain; interactive WPF confirmation `UNKNOWN_REQUIRES_REAL_USER_TEST`.

Without Setup Assistant install on a clean machine: user sees disabled Anny tab + NotInstalled messages — correct honesty, **no human creation**.

---

## 12. Form / Base Models / Sculpt / Height

| Path | Reality | Classification |
|------|---------|----------------|
| Gender load | Loads unskinned base GLB | `PARTIAL` |
| Form sliders | Disabled when `IsCurrentModelRigged==false` | Effectively dead on shipped assets |
| HeightMorph feature message | Speaks Anny phenotype regeneration | **Not connected to FormPanel** |
| Viewport height | Uniform scale | `MISLEADING_UI` |
| Sculpt button | Headless Blender if configured | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| Symmetry checkbox | Dict flag only | `WORKS_UI_ONLY` |

---

## 13. NSFW / Anatomy

Checkboxes update `AnatomyState` and refresh preview. While a 3D model is shown, anatomy images are ignored (`UpdatePreviewFromAnatomy` early-return). No mesh layer surgery.

**Classification:** `PARTIAL` (state + tiny PNG fallback only).

---

## 14. Clothing / Garments / Physics

| UI | Backend | Classification |
|----|---------|----------------|
| Jacket Fit button | Does **not** call `IGarmentFitService`; MessageBox when Experimental | `MISLEADING_UI` |
| Piercings/Tattoos | Disabled | `NOT_IMPLEMENTED` |
| GarmentFitService / GarmentCode | Real when workers installed; CI runtime-integration | `WORKS_BACKEND_ONLY` + gated runtime |
| Physics panel | All softbody/cloth checkboxes disabled; honest copy | `NOT_IMPLEMENTED` |
| Bepu accessories | Service/tests only | `WORKS_BACKEND_ONLY` |

---

## 15. Materials / Presets

| Item | Classification |
|------|----------------|
| Color picker + metallic/roughness → viewport | `PARTIAL` / near E2E for display |
| Write PBR into current Preview GLB | Attempted via `PbrMaterials.ApplyToGlb` | `PARTIAL` |
| Preset JSON list/load/save/screenshot | `WORKS_END_TO_END` for legacy JSON state |
| Presets ≠ `.3dgod` domain project | Dual persistence model — easy user confusion → honesty gap |

---

## 16. Rigging / Skinning / Creatures

| Item | Classification |
|------|----------------|
| RiggingPanel Auto-Rig / MetaHuman | `NOT_IMPLEMENTED` (disabled) |
| `LegacyBlenderBackend.LaunchAutoRig` | Honest NotImplemented (does not launch Sculpt) |
| `NotInstalledAutoRigBackend` | NotInstalled stub |
| SkinTokens service | `IMPLEMENTED_GATED_HARDWARE` + `WORKS_BACKEND_ONLY` (no panel invoke) |
| Setup “Automatic Rigging” | Can install component; still no in-app Rig button to run it → product gap / borderline `MISLEADING_UI` at Setup level |
| CreatureAssembly / FreeformPipeline / text edit | `WORKS_BACKEND_ONLY` |
| Rig validation | `WORKS_BACKEND_ONLY` (Available in feature service; no dedicated UI) |

---

## 17. AI Panel / Image-to-3D / FLUX / LLM

| Control | Code behavior | Classification |
|---------|---------------|----------------|
| Bild laden | Stores path; status says generation not implemented | `WORKS_UI_ONLY` |
| Anny Human | Real generate when gated OK | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| Referenzbild erzeugen | **Enabled when probe invocable; click only sets `StatusLabel` to probe message** — never calls `IReferenceImageGenerationService.GenerateAsync` | `MISLEADING_UI` |
| Erzeuge vollständige Person | Disabled; `AiGeneratePerson` NotImplemented | `NOT_IMPLEMENTED` |
| Erzeuge Asset | Calls `AiAssetPipeline`; frog necklace procedural; otherwise NotInstalled throw | `PARTIAL` |
| FLUX/TripoSR services | Real gated workers | `IMPLEMENTED_GATED_HARDWARE` / `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` + backend-only from UI |
| LLamaSharp product feature | Dynamic service maps ready probe → **NotImplemented** | `NOT_IMPLEMENTED` |
| Deterministic parser | Tests/services | `WORKS_BACKEND_ONLY` |

---

## 18. Export (GLB / FBX / UE5)

| Path | Classification |
|------|----------------|
| GLB rewrite export with count checks | `WORKS_END_TO_END` (needs source file) |
| FBX via Blender + sanity | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| UE5 preflight soft/hard messages | `PARTIAL` (honest non-import) |
| Export to Unreal (UI) | `NOT_IMPLEMENTED` (disabled); dead `File.Copy` handler remains |
| Real UE5 editor import smoke | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` (script/CI self-hosted) |

---

## 19. Setup Assistant / Components / Workers / Hardware

Setup Assistant surfaces: Human Creator (anny), Image→3D (triposr), Text→Character (flux), Parametric Clothing (garmentcode), Automatic Rigging (skintokens), Advanced 3D Quality (**SF3D rejected**), Unreal Export (blender).

| Observation | Impact |
|-------------|--------|
| Install can provision uv workers | Good for power users |
| Several installs have **no matching App action** that runs the worker (FLUX button, SkinTokens, TripoSR, jacket fit) | User installs → still cannot complete the job in UI |
| HardwareProfiler in Settings | Informative |
| Gated workers refuse fake meshes | Honest engineering; incomplete product |

---

## 20. Part 20 Workflow Trace (Anny human) — exact break points

**Intended user story (PHASE 20 / Human Creator):** Install Anny → generate human → edit parameters → see viewport → save project → export GLB.

| Step | Expected | Code path | Break / gate |
|------|----------|-----------|--------------|
| 0 | Fresh Windows app | WPF | Agent cannot run UI → interactive proof UNKNOWN |
| 1 | Setup Assistant install Human Creator | `WorkerUvComponentInstaller` | Needs network + uv; skip leaves Anny NotInstalled |
| 2 | Open Anny tab | `AnnyInspectorPanel` | **BP1:** If probe NotInstalled → `IsEnabled=false` — workflow stops |
| 3 | Load catalog | `GetCatalogAsync` | **BP2:** Worker crash/timeout → status error, no sliders |
| 4 | Move slider / Apply preset | Debounce → `GenerateGlbAsync` | **BP3:** Runtime/venv broken → exception Message/status; no fake mesh |
| 5 | Viewport shows GLB | `LoadPreview` | Works when file written |
| 6 | Undo parameter | `CommandStack` | Works for Anny commits only |
| 7 | File→Save `.3dgod` | Anny state only | **BP4:** Does not embed generated GLB; reopen needs Anny again to regenerate |
| 8 | File→Export GLB | Copy/rewrite current preview | Works if `_currentPreviewPath` set |
| 9 | Edit in Form tab for same character | Separate `CharacterSystem` | **BP5:** Dual models — Form/base GLB ≠ Anny document |
| 10 | Add clothing on that human | ClothingPanel | **BP6:** No call to `GarmentFitService` |
| 11 | Auto-rig for UE5 | RiggingPanel / SkinTokens | **BP7:** UI NotImplemented; SkinTokens not wired |
| 12 | FBX/UE5 | ExportPanel | **BP8:** Needs Blender; Unreal button disabled; preflight ≠ import |

**Verdict for Part 20:** Backend+UI generate chain is **real when Anny is installed**. As a complete “make a game-ready character” workflow it **breaks** at clothing, rig, full project fidelity, and UE5 — and may never start on a clean install (BP1).

---

## 21. Misleading UI & Backend-Only Inventory

### Misleading / bait controls
1. **Referenzbild erzeugen** — can be enabled; does not generate (`AiPanel.BtnReferenceImage_Click`).
2. **Jacket Fit (Experimental)** — MessageBox only; no fit invocation.
3. **Height / Größe** messaging vs uniform scale + disabled Form sliders on bases.
4. **Setup Assistant feature titles** (Image→3D, Text→Character, Automatic Rigging) imply in-app workflows that lack invoke buttons after install.
5. **Export log historically** — current FBX path is more honest; Unreal remains correctly disabled.

### Strong backends users cannot operate from App UI
`GarmentFitService`, `SkinTokensRigService`, `ReferenceImageService`/`FluxService`, `ImageTo3DService`/`TripoSrService`, `CreatureAssembly`, `FreeformPipeline`, `CreatureTextEditService`, `RemeshService`, `AutosaveService`, deterministic AI interpreter, accessory physics.

### Dual state systems
Legacy `CharacterSystem` + presets JSON **versus** domain `ProjectBundle` / Anny state — users can think they “saved the character” when they saved only one slice.

---

## 22. Recommended Implementation Order

Order is **product capability for normal users**, not phase vanity:

1. **Wire already-built backends to UI** (highest ROI): FLUX generate button → PNG → optional TripoSR; Clothing → `GarmentFitService` → viewport; SkinTokens/Automatic Rig from Rigging panel with honest gates.
2. **Unify project model:** `.3dgod` must include active mesh asset(s), materials, and Anny state; wire `AutosaveService` + recovery prompt on startup.
3. **Kill dual morph lies:** Form height either drives AnnyHeightMorph regeneration or is removed/disabled with explicit copy; remove uniform-scale-as-anatomy.
4. **Ship one clean “Human Creator” path:** Setup install Anny → always-on inspector → save/load/export documented as the supported path; demote or hide dead legacy tabs.
5. **Clothing E2E:** GarmentCode install → fit → skin bind → show in viewport → persist in `.3dgod`.
6. **Export honesty package:** GLB default; FBX only with Blender health; UE5 = preflight + optional external smoke — never “Export to Unreal” copy.
7. **Only then** invest in freeform creatures, LLM planning, generative garments, FlaUI polish.

---

## Final Summary — Questions 1–14

1. **Can a normal user launch 3D God and see a body?**  
   Yes on Windows: unskinned base GLBs in Helix (interactive proof still needs a real desktop).

2. **Can they create a new realistic human from the UI today?**  
   Only after Anny worker install; otherwise no. Even then it is Experimental, not a polished product path.

3. **Can they anatomically edit proportions (not uniform scale)?**  
   Not via Form. Anny parameter sliders can, when Anny works. Form height remains uniform scale code.

4. **Can they add clothing that fits the body in-app?**  
   No. Backend fit exists; Clothing UI does not run it.

5. **Can they auto-rig for games?**  
   No in UI. SkinTokens is hardware-gated backend-only.

6. **Can they generate from text/image (FLUX/TripoSR) in-app?**  
   No useful UI path. Reference button is misleading; Image→3D not invoked from App.

7. **Can they save/load their work reliably?**  
   Partially: `.3dgod` saves Anny parameters; not full scene. Autosave not in App. Legacy presets are a second, narrower format.

8. **Can they undo mistakes?**  
   Only for Anny parameter commands hooked to `CommandStack`.

9. **Can they export a GLB of what they see?**  
   Yes, if a real preview file path exists.

10. **Can they export FBX / into Unreal as a pipeline?**  
    FBX only with Blender (gated). Unreal editor pipeline: not in App (button disabled). Preflight ≠ import.

11. **Do physics / NSFW / piercings / tattoos work?**  
    No meaningful product physics or mesh NSFW layers. Checkboxes are disabled or state-only.

12. **Is Setup Assistant enough to unlock the product?**  
    It can install workers, but several installs still lack UI invoke paths — install ≠ usable feature.

13. **Are backends fake?**  
    Generally no — gates refuse fake meshes/images. Honesty is stronger than completeness.

14. **Is 3D God a complete end-user product today?**  
    **No.** It is an incomplete creator UI over a partially excellent gated backend. Strong tests/CI do not equal a finished user product.

---

## Top 5 Blockers (user-facing)

1. **Anny (and other workers) not installed by default** — human creation blocked at probe (Part 20 BP1).
2. **UI not wired to real backends** (FLUX, TripoSR, Garment fit, SkinTokens) — including enabled buttons that only show status/MessageBox.
3. **`.3dgod` / autosave incomplete** — project fidelity and crash recovery missing from App.
4. **Unskinned base models + dual Form/Anny systems** — foundation “character editing” path is hollow.
5. **No in-app path to game-ready rig + UE5 import** — export stops at GLB/optional FBX/preflight.

---

## Evidence index (primary files)

- UI: `3DGodCreator.App/MainWindow.xaml(.cs)`, `Panels/*.xaml.cs`, `Windows/SetupAssistantWindow.xaml.cs`
- Gates: `DynamicFeatureAvailabilityService.cs`, `FeatureAvailabilityService.cs`
- DI: `ThreeDGodComposition.cs`
- Anny/Workers: `AnnyHumanService.cs`, `FluxService.cs`, `TripoSrService.cs`, `GarmentCodeService.cs`
- Project: `GodProjectArchive.cs`, `AutosaveService.cs` (unwired)
- Export: `GlbExportService.cs`, `FbxExportService.cs`
- AI UI gap: `AiPanel.xaml.cs` (`BtnReferenceImage_Click`)
- Clothing UI gap: `ClothingPanel.xaml.cs`
- Height lie: `MainWindow.ApplySculptTransform`, `AnnyHeightMorph.cs` (unused by Form)

---

*End of audit. Product code was not modified. Prior PASS/phase documents were not treated as user proof.*
