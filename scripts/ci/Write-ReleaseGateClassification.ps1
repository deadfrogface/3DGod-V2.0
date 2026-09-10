#Requires -Version 5.1
<#
.SYNOPSIS
  Classify remaining external release gates for CI summary output.
#>
param(
    [ValidateSet("success", "failure", "skipped", "unknown")]
    [string]$InstallerSmoke = "unknown",
    [ValidateSet("CI_VERIFIED", "CI_PARTIAL", "GATED_EXTERNAL_RUNTIME", "FAILED", "unknown")]
    [string]$Anny = "unknown",
    [ValidateSet("CI_VERIFIED", "CI_PARTIAL", "GATED_EXTERNAL_RUNTIME", "FAILED", "unknown")]
    [string]$Garment = "unknown",
    [string]$BuildResult = "unknown",
    [string]$TestSummaryPath = ""
)

$ErrorActionPreference = "Stop"

function Write-Section([string]$t) {
    Write-Host ""
    Write-Host ("=" * 72)
    Write-Host $t
    Write-Host ("=" * 72)
}

$hasGpu = $false
try {
    $nvsmi = Get-Command nvidia-smi -ErrorAction SilentlyContinue
    if ($nvsmi) {
        & nvidia-smi -L 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $hasGpu = $true }
    }
} catch { $hasGpu = $false }

$cudaStatus = if ($hasGpu) { "GATED_MODEL_DOWNLOAD (GPU present; checkpoints/licenses not verified in hosted CI)" } else { "GATED_GPU" }
$ue5Status = "GATED_UE5"
$visualStatus = "GATED_MANUAL_VISUAL"
$installerStatus = switch ($InstallerSmoke) {
    "success" { "CI_VERIFIED" }
    "failure" { "FAILED" }
    "skipped" { "GATED_OTHER (installer smoke not scheduled on this ref)" }
    default { "unknown" }
}

Write-Section "RELEASE GATE CLASSIFICATION"
Write-Host "BUILD_RESULT: $BuildResult"
if ($TestSummaryPath -and (Test-Path $TestSummaryPath)) {
    Write-Host "TEST_SUMMARY_FILE: $TestSummaryPath"
}
Write-Host "CLEAN_WINDOWS_INSTALLER: $installerStatus"
Write-Host "ANNY_RUNTIME: $Anny"
Write-Host "GARMENT_RUNTIME: $Garment"
Write-Host "CUDA_MODELS: $cudaStatus"
Write-Host "UE5_IMPORT: $ue5Status"
Write-Host "MANUAL_VISUAL: $visualStatus"
Write-Host ""
Write-Host "NOTES:"
Write-Host " - CI_VERIFIED means the real capability executed successfully on GitHub-hosted infrastructure."
Write-Host " - GATED_* means not PASS; capability was not executed or cannot be claimed."
Write-Host " - GLB/FBX structural checks are NOT equivalent to real UE5 editor import."
Write-Host " - Self-hosted GPU/UE5 runners could later clear GATED_GPU / GATED_UE5 / model download gates."

# Machine-readable sidecar for artifacts
$outDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..\..")) "artifacts/gate"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$payload = [ordered]@{
    buildResult = $BuildResult
    cleanWindowsInstaller = $installerStatus
    annyRuntime = $Anny
    garmentRuntime = $Garment
    cudaModels = $cudaStatus
    ue5Import = $ue5Status
    manualVisual = $visualStatus
    gpuDetected = $hasGpu
    utc = (Get-Date).ToUniversalTime().ToString("o")
}
$jsonPath = Join-Path $outDir "release-gate-classification.json"
$payload | ConvertTo-Json | Set-Content -LiteralPath $jsonPath -Encoding UTF8
Write-Host "WROTE $jsonPath"
