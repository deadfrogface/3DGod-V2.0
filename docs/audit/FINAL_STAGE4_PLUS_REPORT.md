# FINAL AUDIT — Stages 4–19 (SAFE REUSE continuation)

**Accepted baseline:** `ee90b6050a9aea251f3554b84fafe308dd5dac85`  
**Branch tip (at report write):** post-policy commit on `cursor/stage4-through-19-b322` (see migration list)  
**PR:** https://github.com/deadfrogface/3DGod-V2.0/pull/4  
**Base:** `cursor/safe-reuse-verify-b322`

## GitHub Actions proof

| Run | SHA | Result | Notes |
|-----|-----|--------|-------|
| [34544734590](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34544734590) | `1af380c` Stage 4 | SUCCESS | Build+Test green; Installer/Anny/Garment skipped by path policy |
| [34546839170](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34546839170) | `13f8a87` Stages 5–8+ fix | SUCCESS | Build+Test + Release Gate green; heavy jobs still skipped |
| Post-policy push | this tip | pending / in flight | `ci.yml` `if` extended: `cursor/*` + PRs into `cursor/*` run Clean Installer Smoke + Anny/Garment |

## Build / tests (local Linux agent)

- Release Infrastructure + Core.Tests filter for Component/Setup/Height: **21 passed**
- Full Core.Tests: **~280 passed / 10 failed / 17 skipped** — the 10 failures are the pre-existing Python worker host tests without `python` on this Linux agent (same class as baseline), not Stage 4+ regressions

## Subsystem matrix

| Subsystem | Decision | External eval | Reused | Custom kept/deleted | State |
|-----------|----------|---------------|--------|---------------------|-------|
| ComponentManager | ADAPT | — | ModelManager ZIP/SHA/SafeZip/atomic | echo hardcode → manifest health | PASS (unit) |
| Health checks | CUSTOM_BUILD | — | allowlisted kinds only | no shell-from-manifest | PASS |
| Download | CUSTOM_BUILD | bezzad/Downloader | HttpClient behind `IComponentDownloadService` | Downloader **REJECTED** (no owned pinning gain) | PASS (unit) |
| HardwareProfiler | KEEP+ADAPT | Hardware.Info, LibreHardwareMonitor | nvidia-smi + env overrides | vendor/arch/driver fields; CUDA never from name | PASS; Hardware.Info **DEFERRED**; LHM **REJECTED** |
| Setup Assistant | CUSTOM_BUILD | wizard frameworks | ComponentManager | thin WPF only | PASS (build+UI wired); full GUI FlaUI **GATED_EXTERNAL_RUNNER** |
| uv provisioning | WRAP | astral-sh/uv | pinned 0.6.16 + SHA | no `irm\|iex` product path | PASS (code); end-user install **PARTIAL** until Windows runtime job on this branch |
| Anny product path | WRAP/ADAPT | naver/anny | existing worker + uv.lock | manifests + installer | CI_VERIFIED historically; this-branch re-proof pending |
| Anny height morph | ADAPT | — | Anny phenotype keys | no uniform scale; FeatureAvailability Experimental | PARTIAL (unit + adapter); live mesh **GATED** without Anny venv on Linux |
| GarmentCode | WRAP/KEEP | maria-korosteleva/GarmentCode | pygarment worker | NiceGUI unused by headless; further prune gated on re-lock | CI_VERIFIED historically; prune **PARTIAL** |
| TripoSR | WRAP candidate | VAST-AI TripoSR | provider stubs | no fake inference | GATED_MODEL / NotInstalled |
| SF3D | REJECT for now | Stability SF3D | routing/license gates | — | GATED_LICENSE + GATED_HARDWARE |
| SPAR3D | REJECT for now | Stability SPAR3D | routing/license gates | — | GATED_LICENSE + GATED_HARDWARE |
| FLUX.1-schnell | OPTIONAL WRAP | BFL FLUX | ComponentManager gates | non-commercial variants forbidden | GATED_MODEL / GATED_HARDWARE |
| LLamaSharp | KEEP parser + WRAP LLM | SciSharp LLamaSharp | deterministic parser | free-form only with schema | GATED_MODEL without GGUF |
| SkinTokens | DO NOT INTEGRATE yet | VAST SkinTokens | stubs | — | GATED_LICENSE / GATED_HARDWARE |
| meshoptimizer | KEEP current | zeux/meshoptimizer | optional QEM already evaluated | no mandatory replace | KEEP (prior safe-reuse) |
| xatlas | REJECT | jpcy/xatlas | — | packaging cost | REJECTED (prior doc) |
| Blender export | KEEP | — | headless FBX path | — | PASS (existing) |
| UE5 | KEEP preflight | Blender-For-UnrealEngine (GPL ref only) | no GPL copy-in | — | GATED_UE5 |
| FlaUI | WRAP tests only | FlaUI | — | not in production | GATED_EXTERNAL_RUNNER; InstalledAppSmoke remains baseline |
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

## Honest gaps

1. Installer smoke + Anny/Garment jobs were skipped on feature-branch pushes until CI `if` extended for `cursor/*`.
2. End-user uv install is implemented but not yet re-proven by a green `runtime-integration` job on this tip.
3. Height morph is phenotype-parameter based; live vertex proof still needs Anny runtime.
4. Stages 12–16/18–19 remain gated on models, licenses, UE5, or runner desktop automation.

## Migration commits (Stage 4+)

1. `1af380c` Stage 4 ComponentManager  
2. `d74827e` Stages 5–8+ download/uv/setup/height  
3. `149d1dd` MainWindow restore  
4. `5e60258` SettingsPanel brace fix  
5. `13f8a87` MainWindow usings fix  
6. *(this commit)* enable cursor/* heavy CI gates + this report  
