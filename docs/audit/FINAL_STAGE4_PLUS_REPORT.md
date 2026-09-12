# FINAL AUDIT — Stages 4–19 (SAFE REUSE continuation)

**Accepted baseline:** `ee90b6050a9aea251f3554b84fafe308dd5dac85`  
**FINAL COMMIT SHA (full gate proof tip):** `f6063e9e40a583c94706c1f5152fad8e3f632b79`  
**PR:** https://github.com/deadfrogface/3DGod-V2.0/pull/4  
**Base:** `cursor/safe-reuse-verify-b322`  
**Branch:** `cursor/stage4-through-19-b322`

## Verdict

Stage 4–11 product wiring is **PASS** on Windows CI with honest gates retained for Stages 12–19. Release Gate on the proof tip:

| Gate | Status |
|------|--------|
| BUILD_RESULT | SUCCESS |
| CLEAN_WINDOWS_INSTALLER | CI_VERIFIED |
| ANNY_RUNTIME | CI_VERIFIED |
| GARMENT_RUNTIME | CI_VERIFIED |
| CUDA_MODELS | GATED_GPU |
| UE5_IMPORT | GATED_UE5 |
| MANUAL_VISUAL | GATED_MANUAL_VISUAL |

## GitHub Actions proof

| Run | SHA | Result | Notes |
|-----|-----|--------|-------|
| [34544734590](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34544734590) | `1af380c` Stage 4 | SUCCESS | Build+Test; heavy jobs skipped by prior path policy |
| [34546839170](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34546839170) | `13f8a87` usings fix | SUCCESS | Build+Test + Release Gate; Installer/Anny/Garment still skipped |
| **[34547315306](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34547315306)** | **`63de6cd`** | **SUCCESS** | **All four jobs success:** Build+Test, Clean Installer Smoke, Anny/Garment CPU, Release Gate |
| **[34698366244](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34698366244)** | **`f6063e9`** Stage 12–19 honesty | **SUCCESS** | Build+Test, Clean Installer Smoke, Anny/Garment CPU, Release Gate all success; INSTALLER/ANNY/GARMENT=CI_VERIFIED |
| [34547318896](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34547318896) | `63de6cd` (PR) | SUCCESS | Same four jobs success on pull_request event |

Proof tip live tests (runtime-integration job):

- `CiRuntimeIntegrationTests.Anny_CpuGenerate_WritesRealGlb` — Passed (~1m25s) → `ANNY_RUNTIME=CI_VERIFIED`
- `CiRuntimeIntegrationTests.GarmentCode_CpuJacket_WritesRealGlb` — Passed → `GARMENT_RUNTIME=CI_VERIFIED`
- `INSTALLER_SMOKE_STATUS=SUCCESS` → `CLEAN_WINDOWS_INSTALLER=CI_VERIFIED`

## Build / tests (local Linux agent)

- Release Infrastructure + Core.Tests filter for Component/Setup/Height: **21 passed**
- Full Core.Tests: **~280 passed / 10 failed / 17 skipped** — the 10 failures are pre-existing Python worker host tests without `python` on this Linux agent (same class as baseline), not Stage 4+ regressions
- Authoritative Windows proof: GHA run above (307 unit tests on build-and-test job)

## Subsystem matrix

| Subsystem | Decision | External eval | Reused | Custom kept/deleted | State |
|-----------|----------|---------------|--------|---------------------|-------|
| ComponentManager | ADAPT | — | ModelManager ZIP/SHA/SafeZip/atomic | echo hardcode → manifest health | PASS (unit) |
| Health checks | CUSTOM_BUILD | — | allowlisted kinds only | no shell-from-manifest | PASS |
| Download | CUSTOM_BUILD | bezzad/Downloader | HttpClient behind `IComponentDownloadService` | Downloader **REJECTED** | PASS (unit) |
| HardwareProfiler | KEEP+ADAPT | Hardware.Info, LibreHardwareMonitor | nvidia-smi + env overrides | vendor/arch/driver; CUDA never from name | PASS; Hardware.Info **DEFERRED**; LHM **REJECTED** |
| Setup Assistant | CUSTOM_BUILD | wizard frameworks | ComponentManager | thin WPF only | PASS (build+UI); FlaUI **GATED_EXTERNAL_RUNNER** |
| uv provisioning | WRAP | astral-sh/uv | pinned 0.6.16 + SHA | no product `irm\|iex` | PASS (CI worker sync on Windows); product installer path coded |
| Anny product path | WRAP/ADAPT | naver/anny | existing worker + uv.lock | manifests + installer | **CI_VERIFIED** on `63de6cd` |
| Anny height morph | ADAPT | — | Anny phenotype keys | no uniform scale; FeatureAvailability Experimental | PASS (unit); live mesh via Anny generate **CI_VERIFIED** path; dedicated taller-delta mesh assert still Experimental |
| GarmentCode | WRAP/KEEP | maria-korosteleva/GarmentCode | pygarment worker | NiceGUI unused by headless | **CI_VERIFIED** on `63de6cd`; further prune **PARTIAL** |
| TripoSR | WRAP candidate | VAST-AI TripoSR | provider stubs + packaging NotInstalled | no fake inference; Setup Assistant install only with URL/local source | **GATED_MODEL** / NotInstalled |
| SF3D | **REJECT** this cycle | Stability SF3D | routing/license gates | Setup Assistant CanInstall=false | **REJECTED** (`STAGE13_SF3D_SPAR3D_DECISION.md`) |
| SPAR3D | **REJECT** this cycle | Stability SPAR3D | routing/license gates | Setup Assistant CanInstall=false | **REJECTED** (`STAGE13_SF3D_SPAR3D_DECISION.md`) |
| FLUX.1-schnell | OPTIONAL WRAP | BFL FLUX.1-schnell (Apache-2.0) | ComponentManager gates | non-commercial FLUX variants forbidden | **GATED_MODEL** / NotInstalled |
| LLamaSharp | KEEP parser + WRAP LLM | SciSharp LLamaSharp | deterministic parser | free-form only with schema | **GATED_MODEL** without GGUF |
| SkinTokens | **DO NOT INTEGRATE** | VAST SkinTokens | stubs | Setup Assistant CanInstall=false | **GATED_LICENSE** + **GATED_HARDWARE** (`STAGE16_SKINTOKENS_DECISION.md`) |
| meshoptimizer | KEEP current | zeux/meshoptimizer | optional QEM already evaluated | no mandatory replace; no native PackageReference | **KEEP** (`STAGE17_MESHOPT_XATLAS_DECISION.md`) |
| xatlas | REJECT | jpcy/xatlas | — | packaging cost; no PackageReference | **REJECTED** (`STAGE17_MESHOPT_XATLAS_DECISION.md`) |
| Blender export | KEEP | — | headless FBX path | — | PASS (existing unit); live Blender **GATED_NOT_INSTALLED** on stock runners |
| UE5 | KEEP preflight | Blender-For-UnrealEngine (GPL ref only) | no GPL copy-in | — | **GATED_UE5** |
| FlaUI | DO NOT add to CI | FlaUI | — | not in production | **GATED_EXTERNAL_RUNNER** (`STAGE19_FLAUI_DECISION.md`); InstalledAppSmoke baseline |
| build.ps1 | CUSTOM_BUILD | — | packaging scripts | CI build-and-test calls build.ps1 | PASS |

## Repositories REJECTED / deferred

| Repo | Reason |
|------|--------|
| bezzad/Downloader | HttpClient + Range resume sufficient; avoid foreign types in app |
| LibreHardwareMonitor | Not required after nvidia-smi + Windows APIs |
| Hardware.Info | Deferred; current profiler meets product gates |
| SF3D / SPAR3D | No clear product win vs TripoSR yet; Stability license + VRAM |
| Non-schnell FLUX variants | Non-commercial / unsuitable |
| SkinTokens | License/provenance/VRAM not cleared |
| xatlas | Native packaging burden outweighs UV gain |
| Blender-For-UnrealEngine add-on | GPL-3.0 — reference only, not copied into Core |

## Honest gaps (remaining — external only)

1. **GATED_MODEL:** TripoSR / FLUX.1-schnell / LLamaSharp GGUF — no pinned verified checkpoints on CI runners.
2. **GATED_LICENSE + GATED_HARDWARE:** SkinTokens (and rejected SF3D/SPAR3D) pending commercial/VRAM clearance.
3. **GATED_UE5 / GATED_GPU / GATED_NOT_INSTALLED:** real Unreal import, CUDA models, live Blender on stock runners.
4. **GATED_EXTERNAL_RUNNER:** FlaUI desktop automation; InstalledAppSmoke remains the installer baseline.
5. Height morph dedicated taller-delta live mesh golden remains Experimental (Anny generate path is CI_VERIFIED).

## Migration commits (Stage 4+)

1. `1af380c` Stage 4 ComponentManager  
2. `d74827e` Stages 5–8+ download/uv/setup/height  
3. `149d1dd` MainWindow restore  
4. `5e60258` SettingsPanel brace fix  
5. `13f8a87` MainWindow usings fix  
6. `63de6cd` enable `cursor/*` heavy CI gates + draft final report  
7. `33eec12` finalize FINAL REPORT with GHA proof URLs and CI_VERIFIED gates  
8. `9f14e69`…`f6063e9` Stages 12–19 honesty: Setup Assistant install gates, REJECT/KEEP docs, packaging retarget, honesty tests, final audit tip  

