# FINAL AUDIT — Stages 4–19 (SAFE REUSE continuation)

**Accepted baseline:** `ee90b6050a9aea251f3554b84fafe308dd5dac85`  
**FINAL COMMIT SHA (full gate proof tip):** `63de6cd630095f52d72fe082c9d3e0aca0a1cbbb`  
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
| TripoSR | WRAP candidate | VAST-AI TripoSR | provider stubs | no fake inference | GATED_MODEL / NotInstalled |
| SF3D | REJECT for now | Stability SF3D | routing/license gates | — | GATED_LICENSE + GATED_HARDWARE |
| SPAR3D | REJECT for now | Stability SPAR3D | routing/license gates | — | GATED_LICENSE + GATED_HARDWARE |
| FLUX.1-schnell | OPTIONAL WRAP | BFL FLUX | ComponentManager gates | non-commercial variants forbidden | GATED_MODEL / GATED_HARDWARE |
| LLamaSharp | KEEP parser + WRAP LLM | SciSharp LLamaSharp | deterministic parser | free-form only with schema | GATED_MODEL without GGUF |
| SkinTokens | DO NOT INTEGRATE yet | VAST SkinTokens | stubs | — | GATED_LICENSE / GATED_HARDWARE |
| meshoptimizer | KEEP current | zeux/meshoptimizer | optional QEM already evaluated | no mandatory replace | KEEP (prior safe-reuse) |
| xatlas | REJECT | jpcy/xatlas | — | packaging cost | REJECTED (prior doc) |
| Blender export | KEEP | — | headless FBX path | — | PASS (existing unit); live Blender **GATED_NOT_INSTALLED** on stock runners |
| UE5 | KEEP preflight | Blender-For-UnrealEngine (GPL ref only) | no GPL copy-in | — | GATED_UE5 |
| FlaUI | WRAP tests only | FlaUI | — | not in production | GATED_EXTERNAL_RUNNER; InstalledAppSmoke via installer job |
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

## Honest gaps (remaining)

1. Height morph is phenotype-parameter based with unit coverage; a dedicated taller-delta live mesh golden is still Experimental / not a separate CI filter beyond Anny generate.
2. Stages 12–16/18–19 remain gated on models, licenses, UE5, GPU, or desktop automation (FlaUI).
3. Product end-user ComponentManager uv install path is implemented and worker locks match CI; UI-driven install was not separately FlaUI-proven.

## Migration commits (Stage 4+)

1. `1af380c` Stage 4 ComponentManager  
2. `d74827e` Stages 5–8+ download/uv/setup/height  
3. `149d1dd` MainWindow restore  
4. `5e60258` SettingsPanel brace fix  
5. `13f8a87` MainWindow usings fix  
6. `63de6cd` enable `cursor/*` heavy CI gates + draft final report  
7. *(this commit)* finalize FINAL REPORT with GHA proof URLs and CI_VERIFIED gates  
