#Requires -Version 5.1
<#
.SYNOPSIS
  Print the exact self-hosted runner label contract for GPU / UE5 / FlaUI jobs.
  Does NOT register a runner on the user's PC. Prep-only documentation helper.
#>
param(
    [ValidateSet("gpu", "ue5", "interactive", "all")]
    [string]$Profile = "all"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== 3D God self-hosted runner prep (DO NOT auto-register) ==="
Write-Host "Register only on machines you intentionally dedicate. Labels must match workflows exactly."
Write-Host ""

$profiles = @{
    gpu = @{
        Labels = @("self-hosted", "windows", "gpu", "cuda")
        Needs  = @(
            "Windows x64",
            "NVIDIA GPU with driver + CUDA toolkit",
            ">=8GB VRAM for FLUX.1-schnell; >=14GB VRAM for SkinTokens",
            "HF token with accepted ToS for black-forest-labs/FLUX.1-schnell when downloading",
            "uv on PATH; .NET SDK matching global.json"
        )
        Workflows = @(".github/workflows/runtime-gpu-flux-skintokens.yml")
    }
    ue5 = @{
        Labels = @("self-hosted", "windows", "ue5")
        Needs  = @(
            "Windows x64 with Unreal Engine 5.x installed",
            "UE_ROOT env pointing at engine root",
            "A .uproject path via THREEDGOD_UE5_PROJECT (or workflow input)",
            "UnrealEditor-Cmd available under UE_ROOT"
        )
        Workflows = @(".github/workflows/runtime-ue5.yml")
    }
    interactive = @{
        Labels = @("self-hosted", "windows", "interactive")
        Needs  = @(
            "Interactive Windows desktop session (not Session 0 / headless service-only)",
            "Installed 3D God tree; set THREEDGOD_INSTALL_ROOT",
            "FlaUI UIA3 can attach to 3DGodCreator.App.exe"
        )
        Workflows = @(".github/workflows/runtime-flaui.yml")
    }
}

$keys = if ($Profile -eq "all") { @("gpu", "ue5", "interactive") } else { @($Profile) }
foreach ($k in $keys) {
    $p = $profiles[$k]
    Write-Host "---- $k ----"
    Write-Host ("labels: " + ($p.Labels -join ", "))
    Write-Host "runs-on example:"
    Write-Host ("  [ " + (($p.Labels | ForEach-Object { "'$_'" }) -join ", ") + " ]")
    Write-Host "requirements:"
    $p.Needs | ForEach-Object { Write-Host "  - $_" }
    Write-Host "workflows:"
    $p.Workflows | ForEach-Object { Write-Host "  - $_" }
    Write-Host ""
}

Write-Host "GitHub UI: Repo → Settings → Actions → Runners → New self-hosted runner"
Write-Host "Use the labels above when configuring. Never commit registration tokens."
