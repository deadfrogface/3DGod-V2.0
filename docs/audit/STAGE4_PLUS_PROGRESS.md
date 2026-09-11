# SAFE REUSE Stages 4+ progress

Accepted baseline: `ee90b6050a9aea251f3554b84fafe308dd5dac85`

## Stage 4 — ComponentManager (PASS)
- KEEP ModelManager security (SHA-256, SafeZipExtractor, path escape, atomic activate)
- ADAPT into `IComponentManager` + typed allowlisted health checks
- Demo `echo_worker.py` checks moved to `workers/echo/component.manifest.json`

## Stage 5 — Download (PASS / CUSTOM_BUILD)
- `IComponentDownloadService` over HttpClient
- REJECT wrapping bezzad/Downloader for now (insufficient advantage vs owned pinning/resume)
- Tests: success, cancel, hash mismatch, HTTP failure, invalid URL, resume

## Stage 6 — Hardware (PASS / KEEP+small ADAPT)
- KEEP HardwareProfiler + nvidia-smi
- REJECT LibreHardwareMonitor; DEFER Hardware.Info
- Added vendor/arch/driver fields; CUDA never inferred from GPU name alone

## Stage 7 — Setup Assistant (PASS / CUSTOM_BUILD thin WPF)
- Product feature catalog (Human Creator, Image→3D, …)
- First-launch + Settings/Components + Tools menu
- Skip never blocks core startup

## Stage 8 — uv provisioning (PASS / WRAP astral-sh/uv)
- Pinned uv 0.6.16 with SHA-256; managed under `%LocalAppData%/3DGod/runtime/uv`
- NO `irm | iex` for product path
- `IWorkerUvComponentInstaller` uses same `workers/*/uv.lock` as CI

## Stage 9 — Anny product path (PARTIAL)
- CI_VERIFIED runtime preserved
- End-user install path wired via ComponentManager + uv
- Full GUI E2E still needs Windows runner proof

## Stage 10 — Height morph (PARTIAL / ADAPT)
- `AnnyHeightMorph` maps to Anny phenotype keys (not uniform scale)
- FeatureAvailability → Experimental
- Live mesh proof remains Anny-live / CI gated on Linux agents without worker venv

## Stage 11 — GarmentCode (PARTIAL)
- CI_VERIFIED preserved
- Production pyproject remains `pygarment`-centered; NiceGUI not used by headless worker
- Further transitive prune gated on re-lock + live test

## Stages 12–19
See `docs/audit/STAGE12_19_GATES.md` — honest GATED_* where models/licenses/UE5/FlaUI unavailable.

## build.ps1
Root orchestrator added; CI build-and-test migrates to call it for restore/build/test.
