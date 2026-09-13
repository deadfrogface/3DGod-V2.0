# Self-hosted runner prep (GPU / UE5 / interactive)

**Do not register the user's personal weak laptop.** Use dedicated lab/build machines only.

## Label contract (exact)

| Profile | Required labels | Workflow |
|---------|-----------------|----------|
| GPU / CUDA | `self-hosted`, `windows`, `gpu`, `cuda` | `.github/workflows/runtime-gpu-flux-skintokens.yml` |
| UE5 | `self-hosted`, `windows`, `ue5` | `.github/workflows/runtime-ue5.yml` |
| Interactive FlaUI | `self-hosted`, `windows`, `interactive` | `.github/workflows/runtime-flaui.yml` |

Print the same contract:

```powershell
.\scripts\ops\Prepare-SelfHostedRunnerLabels.ps1 -Profile all
```

## GPU / CUDA machine

- Windows x64 + NVIDIA driver + CUDA
- **FLUX.1-schnell:** ≥8GB VRAM; HF accept `black-forest-labs/FLUX.1-schnell` @ `741f7c3ce8b383c54771c7003378a50191e9efe9`
- **SkinTokens:** ≥14GB VRAM; HF `VAST-AI/SkinTokens` + upstream under `workers/skintokens/upstream`
- `uv` on PATH; .NET SDK matching `global.json`
- Optional: `HF_TOKEN` in runner secrets for gated downloads
- **Never** claim PASS_REAL without real PNG/skinned GLB

GitHub-hosted GPU larger runners exist (Tesla T4) but require **Team/Enterprise + billing + org larger-runner setup**. This repo does **not** silently enable paid GPU SKUs. Prefer self-hosted labels above.

## UE5 machine

- Unreal Engine 5.x installed
- `UE_ROOT` pointing at engine root
- `THREEDGOD_UE5_PROJECT` or workflow `project_path` → `.uproject`
- Success marker: log contains `3DGOD_UE5_IMPORT_OK` (optional skeleton/materials/morph/LOD markers)

```powershell
$env:UE_ROOT = 'C:\Program Files\Epic Games\UE_5.x'
.\scripts\ue5\Invoke-Ue5ImportSmoke.ps1 -AssetPath .\path\to\asset.fbx -ProjectPath .\MyProj.uproject
```

## Interactive FlaUI machine

- Interactive Windows desktop session (not headless Session 0 service-only)
- Installed 3D God tree; `THREEDGOD_INSTALL_ROOT` set
- Missing install root → **Skip** (`GATED_EXTERNAL_RUNNER` / `GATED_INTERACTIVE_DESKTOP`)
- Install root set but UI broken → **Fail**

```powershell
$env:THREEDGOD_INSTALL_ROOT = 'C:\Program Files\3DGod'
dotnet test .\3DGodCreator.UiTests\3DGodCreator.UiTests.csproj -c Release
```

## Security

- Never commit runner registration tokens
- Do not upload model weights (`.ckpt`, `.gguf`, `.safetensors`) as CI artifacts
