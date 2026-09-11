# Stages 12–19 — honest gates

| Stage | Provider | Decision | State |
|------|----------|----------|-------|
| 12 | TripoSR | WRAP candidate (worker) | GATED_MODEL / NotInstalled until package+worker land |
| 13 | SF3D / SPAR3D | Optional; compare vs TripoSR | GATED_LICENSE + GATED_HARDWARE |
| 14 | FLUX.1-schnell | Optional text→image | GATED_MODEL (size/license/hardware checks via ComponentManager) |
| 15 | LLamaSharp | KEEP deterministic parser; LLM only for free-form | GATED_MODEL without GGUF |
| 16 | SkinTokens | Do not integrate until licenses+VRAM proven | GATED_LICENSE / GATED_HARDWARE |
| 17 | meshoptimizer / xatlas | Only if measurable deficit | KEEP current unless benchmarks win |
| 18 | UE5 | Preserve Blender preflight; no fake UE import | GATED_UE5 |
| 19 | FlaUI | Test-only; do not destabilize CI | GATED_EXTERNAL_RUNNER; InstalledAppSmoke remains baseline |

Rejected for production pipe-install: `irm https://astral.sh/uv/install.ps1 | iex` — replaced by pinned `UvProvisioner`.
