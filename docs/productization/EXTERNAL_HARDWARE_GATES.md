# External Hardware / Runtime Gates

| Gate | Why | Approximate needs | How to clear later |
|------|-----|-------------------|--------------------|
| Auto-Rig Vulkan `PASS_REAL` | Hosted runners lack proven Vulkan GPU path | Vulkan 1.x GPU + skin-tokens.cpp CLI + GGUF F16 | Self-hosted Windows GPU runner; run `SkinTokensCppAutoRigTests.VulkanRig_*` |
| SkinTokens official CUDA | Needs NVIDIA + ≥14GB VRAM + MIT ckpts | CUDA GPU ≥14GB | `runtime-gpu-flux-skintokens.yml` self-hosted `gpu,cuda` |
| FLUX.1-schnell | CUDA ≥8GB + weights | same | same GPU workflow |
| FlaUI full end-user | Needs interactive desktop + install root | Windows interactive self-hosted | `runtime-flaui.yml` labels `self-hosted,windows,interactive` |
| UE5 editor import | Full Unreal install + project | UE5 + `UE_ROOT` | `runtime-ue5.yml` — do not install UE in hosted CI |
| Paid cloud GPU | Cost / credentials | user-provided | Document only — never auto-spend |

Do **not** convert these into soft PASS on CPU-only CI.
