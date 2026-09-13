# Stages 12–19 — honest gates

| Stage | Provider | Decision | State |
|------|----------|----------|-------|
| 12 | TripoSR | WRAP worker (`workers/triposr`) | **PASS_REAL** – local CPU inference wrote real GLB (chair.png → ~97KB, 2454 verts). SUPPORTED_BUT_SLOW on CPU; CUDA recommended. |
| 13 | SF3D / SPAR3D | **REJECT** this cycle | `STAGE13_SF3D_SPAR3D_DECISION.md`; Setup Assistant CanInstall=false |
| 14 | FLUX.1-schnell | OPTIONAL WRAP (Apache-2.0 only) | **GATED_MODEL** / NotInstalled; non-commercial FLUX variants forbidden |
| 15 | LLamaSharp | KEEP deterministic parser; LLM only for free-form | **GATED_MODEL** without verified GGUF mapping |
| 16 | SkinTokens | **DO NOT INTEGRATE** | `STAGE16_SKINTOKENS_DECISION.md`; GATED_LICENSE + GATED_HARDWARE |
| 17 | meshoptimizer / xatlas | **KEEP** current remesh + spherical UVs | `STAGE17_MESHOPT_XATLAS_DECISION.md`; natives not referenced |
| 18 | UE5 | KEEP Blender preflight | **GATED_UE5** — no fake UE import claims |
| 19 | FlaUI | Test-only; not on CI | `STAGE19_FLAUI_DECISION.md`; **GATED_EXTERNAL_RUNNER**; InstalledAppSmoke remains baseline |

Rejected for production pipe-install: `irm https://astral.sh/uv/install.ps1 | iex` — replaced by pinned `UvProvisioner`.
