#Requires -Version 5.1
<#
.SYNOPSIS
  Honest online-runtime classification sidecar (PASS_REAL / SKIPPED_ENVIRONMENT / GATED_* / FAIL).
  No soft-pass: skipped ≠ passed.
#>
param(
    [string]$Build = "unknown",
    [string]$Installer = "unknown",
    [string]$Anny = "unknown",
    [string]$AnnyMorph = "unknown",
    [string]$Garment = "unknown",
    [string]$TripoSr = "unknown",
    [string]$LlamaSharp = "unknown",
    [string]$Flux = "GATED_HARDWARE",
    [string]$SkinTokens = "GATED_HARDWARE",
    [string]$Ue5 = "GATED_UE5",
    [string]$FlaUi = "GATED_INTERACTIVE_DESKTOP",
    [string]$SaveLoad = "unknown",
    [string]$Security = "unknown",
    [string]$Diagnostics = "unknown",
    [string]$SetupAssistant = "unknown",
    [string]$ComponentManager = "unknown",
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if (-not $OutDir) { $OutDir = Join-Path $root "artifacts/gate" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Normalize([string]$v) {
    if ([string]::IsNullOrWhiteSpace($v)) { return "unknown" }
    return $v.Trim()
}

$payload = [ordered]@{
    schema = "3dgod-online-runtime/1"
    utc = (Get-Date).ToUniversalTime().ToString("o")
    classifications = [ordered]@{
        appBuild = Normalize $Build
        installer = Normalize $Installer
        annyCpuGenerate = Normalize $Anny
        annyHeightMorph = Normalize $AnnyMorph
        garmentCodeCpu = Normalize $Garment
        tripoSrCpu = Normalize $TripoSr
        llamaSharpCpuGguf = Normalize $LlamaSharp
        flux = Normalize $Flux
        fluxThenTripoSr = Normalize $Flux
        skinTokens = Normalize $SkinTokens
        ue5EditorImport = Normalize $Ue5
        flaUiInteractive = Normalize $FlaUi
        projectSaveLoad = Normalize $SaveLoad
        securityHardening = Normalize $Security
        smartDiagnostics = Normalize $Diagnostics
        setupAssistant = Normalize $SetupAssistant
        componentManager = Normalize $ComponentManager
    }
    notes = @(
        "PASS_REAL = real runtime executed on this runner and assertions passed.",
        "SKIPPED_ENVIRONMENT = env contract unmet; SkippableFact (not a pass).",
        "GATED_HARDWARE = needs CUDA/VRAM not available on standard GH-hosted runners (no paid GPU auto-intro).",
        "GATED_UE5 = needs Unreal Engine install + UE_ROOT + .uproject on self-hosted Windows.",
        "GATED_INTERACTIVE_DESKTOP = needs interactive Windows session + THREEDGOD_INSTALL_ROOT.",
        "FAIL = required real execution failed."
    )
}

$jsonPath = Join-Path $OutDir "online-runtime-classification.json"
$payload | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
Write-Host "WROTE $jsonPath"
$payload.classifications.GetEnumerator() | ForEach-Object {
    Write-Host ("{0}={1}" -f $_.Key, $_.Value)
}
