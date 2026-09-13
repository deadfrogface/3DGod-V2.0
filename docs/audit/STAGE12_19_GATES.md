# Stages 12–19 — honest gates (updated after TripoSR + LLamaSharp real runs)

| Stage | Provider | Decision | State |
|------|----------|----------|-------|
| 12 | TripoSR | WRAP worker | **PASS_REAL** (CPU SUPPORTED_BUT_SLOW); MIT checkpoint acquire + uv worker |
| 13 | SF3D / SPAR3D | **REJECT** | `STAGE13_SF3D_SPAR3D_DECISION.md` — no product win vs TripoSR; Stability license |
| 14 | FLUX.1-schnell | OPTIONAL WRAP | **IMPLEMENTED_GATED_HARDWARE / GATED_MODEL** until pinned Apache-2.0 weights + GPU |
| 15 | LLamaSharp | KEEP deterministic parser + GGUF path | **PASS_REAL** on Qwen2.5-0.5B-Instruct Q4_K_M (Apache-2.0); malformed JSON rejected |
| 16 | SkinTokens | **DO NOT INTEGRATE** | **GATED_LICENSE** (+ hardware); `STAGE16_SKINTOKENS_DECISION.md` |
| 17 | UE5 editor import | KEEP preflight + automation script | **IMPLEMENTED_GATED_UE5_RUNTIME** (`scripts/ue5/Invoke-Ue5ImportSmoke.ps1`) |
| 18/19 | FlaUI | Test-only UIA3 project | **IMPLEMENTED_GATED_EXTERNAL_RUNNER** (`3DGodCreator.UiTests`); InstalledAppSmoke baseline |
| 11 | GarmentCode prune | Direct deps minimal | **DEPENDENCIES_REQUIRED** (transitive CGAL/libigl via pygarment); NiceGUI unused by headless |

Rejected for production pipe-install: `irm https://astral.sh/uv/install.ps1 | iex` — replaced by pinned `UvProvisioner`.
