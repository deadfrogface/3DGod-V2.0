#Requires -Version 5.1
<#
.SYNOPSIS
  Acquire TripoSR MIT checkpoint (config.yaml + model.ckpt) with pinned SHA-256 for CI.
  ~1.6GB – intended for scheduled / workflow_dispatch / heavy job. Do not upload the ckpt as artifact.
#>
param(
    [string]$ModelDir = "",
    [switch]$SkipIfPresent
)

$ErrorActionPreference = "Stop"

$Sha256 = "429e2c6b22a0923967459de24d67f05962b235f79cde6b032aa7ed2ffcd970ee"
$SizeBytes = 1677246742
$CkptUrl = "https://huggingface.co/stabilityai/TripoSR/resolve/main/model.ckpt"
$ConfigUrl = "https://huggingface.co/stabilityai/TripoSR/resolve/main/config.yaml"

if (-not $ModelDir) {
    if ($IsWindows -or $env:OS -match "Windows") {
        $ModelDir = Join-Path $env:LOCALAPPDATA "3DGod\Models\triposr"
    } else {
        $xdg = if ($env:XDG_DATA_HOME) { $env:XDG_DATA_HOME } else { Join-Path $HOME ".local/share" }
        $ModelDir = Join-Path $xdg "3DGod/Models/triposr"
    }
}

New-Item -ItemType Directory -Force -Path $ModelDir | Out-Null
$ckpt = Join-Path $ModelDir "model.ckpt"
$config = Join-Path $ModelDir "config.yaml"

Write-Host "TRIPOSR_MODEL_DIR=$ModelDir"
Write-Host "TRIPOSR_CKPT_SHA256=$Sha256"

if (-not (Test-Path $config)) {
    Write-Host "Downloading config.yaml…"
    & curl.exe -L --fail --retry 5 --retry-delay 5 -o $config $ConfigUrl
    if ($LASTEXITCODE -ne 0) { throw "config.yaml download failed" }
}

$have = (Test-Path $ckpt) -and ((Get-Item $ckpt).Length -eq $SizeBytes)
if ($SkipIfPresent -and $have) {
    Write-Host "Checkpoint present with expected size – verifying hash…"
} elseif ($have) {
    Write-Host "Checkpoint present – verifying hash…"
} else {
    if (Test-Path $ckpt) { Remove-Item -Force $ckpt }
    Write-Host "Downloading model.ckpt ($SizeBytes bytes) – this is slow…"
    & curl.exe -L --fail --retry 5 --retry-delay 5 -o $ckpt $CkptUrl
    if ($LASTEXITCODE -ne 0) { throw "model.ckpt download failed (exit $LASTEXITCODE)" }
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $ckpt).Hash.ToLowerInvariant()
if ($hash -ne $Sha256) {
    throw "TripoSR checkpoint SHA-256 mismatch: got $hash expected $Sha256"
}
Write-Host "TRIPOSR_CKPT_HASH_OK=$hash"
Write-Host "TRIPOSR_ACQUIRE=SUCCESS"
